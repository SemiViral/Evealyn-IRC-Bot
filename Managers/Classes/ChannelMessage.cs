using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Eve.Data.Protocols;

namespace Eve.Managers.Classes {
	public class ChannelMessage {
		public string RawMessage { get; }

		public DateTime Time { get; private set; }

		// is it a server?
		public bool SenderIdentifiable { get; private set; }
		private readonly string[] _shortList = {
			"nickserv",
			"chanserv",
		};

		// Recievable variables
		public string Nickname { get; private set; }
		public string Realname { get; private set; }
		public string Hostname { get; private set; }
		public string Recipient { get; private set; }
		public string Type { get; set; }
		public string Args { get; private set; }
		public List<string> _Args { get; private set; }

		// Response variables
		public string Target { get; set; }
		public string Message { get; set; }
		public List<string> MultiMessage { get; set; }
		public ExitType ExitType { get; set; }

		// Regex for parsing raw message
		private readonly Regex _messageRegex = new Regex(@"^:(?<Sender>[^\s]+)\s(?<Type>[^\s]+)\s(?<Recipient>[^\s]+)\s?:?(?<Args>.*)", RegexOptions.Compiled);
		private readonly Regex _pingRegex = new Regex(@"^PING :(?<Message>.+)", RegexOptions.None);
		// private readonly Regex _argMessageRegex = new Regex(@"^:(?<Arg1>[^\s]+)\s(?<Arg2>[^\s]+)\s(?<Arg3>[^\s]+)\s?:?(?<Arg4>.*)", RegexOptions.Compiled);
		private readonly Regex _senderRegex = new Regex(@"^(?<Nickname>[^\s]+)!(?<Realname>[^\s]+)@(?<Hostname>[^\s]+)", RegexOptions.Compiled);

		public ChannelMessage(string rawData) {
			RawMessage = rawData;
			
			ParseMessage(RawMessage);
		}

		public void ParseMessage(string rawData) {
			if (_pingRegex.IsMatch(RawMessage)) {
				Type = IrcProtocol.Pong;
				Message = _pingRegex.Match(RawMessage).Groups["Message"].Value;
				Console.Write("Ping ... ");
				return;
			}

			if (!_messageRegex.IsMatch(rawData)) return;

			Match mVal = _messageRegex.Match(rawData);
			string mSender = mVal.Groups["Sender"].Value;
			Match sMatch = _senderRegex.Match(mSender);

			// class property setting
			Nickname = mSender;
			Realname = mSender.ToLower();
			Hostname = mSender;
			Type = mVal.Groups["Type"].Value;
			Recipient = mVal.Groups["Recipient"].Value.StartsWith(":")
				? mVal.Groups["Recipient"].Value.Substring(1)
				: mVal.Groups["Recipient"].Value;
			Args = mVal.Groups["Args"].Value;
			_Args = Args?.Trim().Split(new[] { ' ' }, 4).ToList();

			Time = DateTime.UtcNow;
			MultiMessage = new List<string>();

			if (!sMatch.Success) {
				SenderIdentifiable = false;	
				return;
			}

			string realname = sMatch.Groups["Realname"].Value;
			Nickname = sMatch.Groups["Nickname"].Value;
			Realname = realname.StartsWith("~") ? realname.Substring(1) : realname;
			Hostname = sMatch.Groups["Hostname"].Value;

			SenderIdentifiable = (!_shortList.Contains(Realname.ToLower()));
		}

		public void Reset(string target = null) {
			MultiMessage = new List<string>();
			ExitType = ExitType.DoNotExit;
			Target = Message = string.Empty;

			Target = target ?? Recipient;
		}
	}

	public enum ExitType {
		DoNotExit,
		MessageAndExit,
		Exit
	}
}
