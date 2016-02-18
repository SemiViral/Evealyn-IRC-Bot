using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Eve.Ref;
using Eve.Types;

namespace Eve.Plugin {
	[Serializable]
	public class ChannelMessageEventArgs {
		// Unused regexs
		// private readonly Regex _argMessageRegex = new Regex(@"^:(?<Arg1>[^\s]+)\s(?<Arg2>[^\s]+)\s(?<Arg3>[^\s]+)\s?:?(?<Arg4>.*)", RegexOptions.Compiled);
		//private static readonly Regex PingRegex = new Regex(@"^PING :(?<Message>.+)", RegexOptions.None);

		// Regex for parsing raw messages
		private static readonly Regex MessageRegex =
			new Regex(@"^:(?<Sender>[^\s]+)\s(?<Type>[^\s]+)\s(?<Recipient>[^\s]+)\s?:?(?<Args>.*)", RegexOptions.Compiled);

		private static readonly Regex SenderRegex = new Regex(@"^(?<Nickname>[^\s]+)!(?<Realname>[^\s]+)@(?<Hostname>[^\s]+)",
			RegexOptions.Compiled);

		private static readonly string[] ShortIgnoreList = {
			"nickserv",
			"chanserv"
		};

		public ChannelMessageEventArgs(string rawData) {
			RawMessage = rawData;
			Parse(RawMessage);
			Preprocess();
		}

		public string RawMessage { get; }

		/// <summary>
		///     Represents whether the realname processed was contained in the specified identifier list (ChanServ, NickServ)
		/// </summary>
		public bool IsRealUser { get; private set; }

		public string Nickname { get; private set; }
		public string Realname { get; private set; }
		public string Hostname { get; private set; }
		public string Recipient { get; private set; }
		public string Type { get; set; }
		public string Args { get; private set; }
		public List<string> SplitArgs { get; private set; } = new List<string>();

		public void Parse(string rawData) {
			if (!MessageRegex.IsMatch(rawData)) return;

			// begin parsing message into sections
			Match mVal = MessageRegex.Match(rawData);
			Match sMatch = SenderRegex.Match(mVal.Groups["Sender"].Value);

			// class property setting
			Nickname = mVal.Groups["Sender"].Value;
			Realname = mVal.Groups["Sender"].Value.ToLower();
			Hostname = mVal.Groups["Sender"].Value;
			Type = mVal.Groups["Type"].Value;
			Recipient = mVal.Groups["Recipient"].Value.StartsWith(":")
				? mVal.Groups["Recipient"].Value.Substring(1)
				: mVal.Groups["Recipient"].Value;
			Args = mVal.Groups["Args"].Value;
			SplitArgs = Args?.Trim().Split(new[] {' '}, 4).ToList();
			IsRealUser = false;

			if (!sMatch.Success) return;

			string realname = sMatch.Groups["Realname"].Value;
			Nickname = sMatch.Groups["Nickname"].Value;
			Realname = realname.StartsWith("~") ? realname.Substring(1) : realname;
			Hostname = sMatch.Groups["Hostname"].Value;
			IsRealUser = !ShortIgnoreList.Contains(Realname.ToLower());
		}

		/// <summary>
		///     Preprocess ChannelMessage and determine whether to fire OnChannelMessage event
		/// </summary>
		private void Preprocess() {
			User.Current = User.Get(Realname);

			switch (Type) {
				case Protocols.MOTD_REPLY_END:
					if (IrcBot.Config.Identified) return;

					Writer.SendData(Protocols.PRIVMSG, $"NICKSERV IDENTIFY {IrcBot.Config.Password}");
					Writer.SendData(Protocols.MODE, $"{IrcBot.Config.Nickname} +B");

					foreach (string channel in IrcBot.Config.Channels) {
						Writer.SendData(Protocols.JOIN, channel);
						Channel.Add(channel);
					}

					IrcBot.Config.Identified = true;
					SetAbort();
					break;
				case Protocols.NICK:
					Database.QueryDefaultDatabase(
						$"UPDATE users SET nickname='{Recipient}' WHERE realname='{Realname}'");

					SetAbort();
					break;
				case Protocols.JOIN:
					// todo write code to send messages inside user object to channel

					Channel.Get(Recipient).AddUser(Realname);

					SetAbort();
					break;
				case Protocols.PART:
					Channel.Get(Recipient).RemoveUser(Realname);

					SetAbort();
					break;
				case Protocols.NAME_REPLY:
					// splits the channel user list in half by the :, then splits each user into an array object to be iterated
					foreach (string s in Args.Split(':')[1].Split(' ')) {
						Channel.Get(Recipient)?.AddUser(s);
					}

					SetAbort();
					break;
				default:
					if (!SplitArgs[0].Replace(",", string.Empty).Equals(IrcBot.Config.Nickname.ToLower()) ||
						IrcBot.IgnoreList.Contains(Realname) ||
						User.Current.GetTimeout()) {
						SetAbort();
						break;
					}

					if (SplitArgs.Count < 2) {
						Writer.Privmsg(Recipient, "Please provide a command. Type 'eve help' to view my command list.");
						SetAbort();
					}

					if (PluginWrapper.Commands.Keys.Contains(SplitArgs[1].ToLower())) return;

					// built-in `help' command
					if (SplitArgs[1].ToLower().Equals("help")) {
						if (SplitArgs.Count == 2)
							Writer.SendData(Protocols.PRIVMSG,
								$"{Recipient} Active commands: {string.Join(", ", PluginWrapper.Commands.Keys)}");
						else {
							if (SplitArgs.Count < 3) {
								Writer.Privmsg(Recipient, "Insufficient parameters.");
								break;
							}

							if (!PluginWrapper.Commands.ContainsKey(SplitArgs[2])) {
								Writer.Privmsg(Recipient, "Command not found.");
								break;
							}

							Writer.Privmsg(Recipient, $"{SplitArgs[2]} {PluginWrapper.Commands[SplitArgs[2]]}");
						}
						break;
					}

					Writer.Privmsg(Recipient, "Invalid command. Type 'eve help' to view my command list.");
					break;
			}
		}

		private void SetAbort() {
			Type = Protocols.ABORT;
		}
	}
}