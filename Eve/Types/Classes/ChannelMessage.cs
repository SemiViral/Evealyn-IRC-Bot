using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Eve.Types.Irc;

namespace Eve.Types.Classes {
	internal class ReadonlyVars {
		// Useless regex for now
		// private readonly Regex _argMessageRegex = new Regex(@"^:(?<Arg1>[^\s]+)\s(?<Arg2>[^\s]+)\s(?<Arg3>[^\s]+)\s?:?(?<Arg4>.*)", RegexOptions.Compiled);

		// Regex for parsing raw message
		public static readonly Regex MessageRegex =
			new Regex(@"^:(?<Sender>[^\s]+)\s(?<Type>[^\s]+)\s(?<Recipient>[^\s]+)\s?:?(?<Args>.*)", RegexOptions.Compiled);

		public static readonly Regex PingRegex = new Regex(@"^PING :(?<Message>.+)", RegexOptions.None);

		public static readonly Regex SenderRegex = new Regex(@"^(?<Nickname>[^\s]+)!(?<Realname>[^\s]+)@(?<Hostname>[^\s]+)",
			RegexOptions.Compiled);

		public static readonly string[] ShortIgnoreList = {
			"nickserv",
			"chanserv"
		};
	}

	public class ChannelMessage {
		public ChannelMessage(string rawData) {
			RawMessage = rawData;
			ParseMessage(RawMessage);
		}

		public string RawMessage { get; }

		public DateTime Time { get; private set; }

		// is it a server?
		public bool SenderIdentifiable { get; private set; }

		// Recievable variables
		public string Nickname { get; private set; }
		public string Realname { get; private set; }
		public string Hostname { get; private set; }
		public string Recipient { get; private set; }
		public string Args { get; private set; }
		public List<string> _Args { get; private set; } = new List<string>();
		public string Type { get; set; }

		// Response variables
		public string Target { get; set; }
		public string Message { get; set; }
		public List<string> MultiMessage { get; set; } = new List<string>();
		public ExitType ExitType { get; set; } = ExitType.DoNotExit;

		public void ParseMessage(string rawData) {
			if (ReadonlyVars.PingRegex.IsMatch(rawData)) {
				Type = IrcProtocol.Pong;
				Message = rawData.Replace("PING", "PONG");
				return;
			}

			if (!ReadonlyVars.MessageRegex.IsMatch(rawData)) return;

			// begin parsing message into sections
			Match mVal = ReadonlyVars.MessageRegex.Match(rawData);
			string mSender = mVal.Groups["Sender"].Value;
			Match sMatch = ReadonlyVars.SenderRegex.Match(mSender);

			// class property setting
			Nickname = mSender;
			Realname = mSender.ToLower();
			Hostname = mSender;
			Type = mVal.Groups["Type"].Value;
			Recipient = mVal.Groups["Recipient"].Value.StartsWith(":")
				? mVal.Groups["Recipient"].Value.Substring(1)
				: mVal.Groups["Recipient"].Value;
			Args = mVal.Groups["Args"].Value;
			_Args = Args?.Trim().Split(new[] {' '}, 4).ToList();

			Time = DateTime.UtcNow;

			if (!sMatch.Success) {
				SenderIdentifiable = false;
				return;
			}

			string realname = sMatch.Groups["Realname"].Value;
			Nickname = sMatch.Groups["Nickname"].Value;
			Realname = realname.StartsWith("~") ? realname.Substring(1) : realname;
			Hostname = sMatch.Groups["Hostname"].Value;

			SenderIdentifiable = !ReadonlyVars.ShortIgnoreList.Contains(Realname.ToLower());
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