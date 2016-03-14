using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Eve.Ref;

namespace Eve.Plugin {
	[Serializable]
	public class ChannelMessageEventArgs : EventArgs {
		// Unused regexs
		// private readonly Regex _argMessageRegex = new Regex(@"^:(?<Arg1>[^\s]+)\s(?<Arg2>[^\s]+)\s(?<Arg3>[^\s]+)\s?:?(?<Arg4>.*)", RegexOptions.Compiled);
		// private static readonly Regex PingRegex = new Regex(@"^PING :(?<Message>.+)", RegexOptions.None);

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

			if (PreprocessAndAbort()) Type = Protocols.ABORT;
		}

		public string RawMessage { get; }

		/// <summary>
		///     Represents whether the realname processed was contained in the specified identifier list (ChanServ, NickServ)
		/// </summary>
		public bool IsRealUser { get; private set; }

		public DateTime Timestamp { get; private set; }

		public string Nickname { get; private set; }
		public string Realname { get; private set; }
		public string Hostname { get; private set; }
		public string Recipient { get; private set; }
		public string Type { get; set; }
		public string Args { get; private set; }
		public List<string> SplitArgs { get; private set; } = new List<string>();

		public void Parse(string rawData) {
			if (!MessageRegex.IsMatch(rawData)) return;

			Timestamp = DateTime.Now;

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
		private bool PreprocessAndAbort() {
			switch (Type) {
				case Protocols.MOTD_REPLY_END:
					if (Program.Bot.Config.Identified) break;

					Writer.SendData(Protocols.PRIVMSG, $"NICKSERV IDENTIFY {Program.Bot.Config.Password}");
					Writer.SendData(Protocols.MODE, $"{Program.Bot.Config.Nickname} +B");

					foreach (string channel in Program.Bot.Config.Channels) {
						Writer.SendData(Protocols.JOIN, channel);
						Program.Bot.Channels.Add(channel);
					}

					Program.Bot.Config.Identified = true;
					break;
				case Protocols.NICK:
					Database.QueryDefaultDatabase(
						$"UPDATE users SET nickname='{Recipient}' WHERE realname='{Realname}'");
					break;
				case Protocols.JOIN:
					// todo write code to send messages inside user object to channel

					Program.Bot.Channels.Get(Recipient).AddUser(Realname);
					break;
				case Protocols.PART:
					Program.Bot.Channels.Get(Recipient).RemoveUser(Realname);
					break;
				case Protocols.NAME_REPLY:
					// splits the channel user list in half by the :, then splits each user into an array object to be iterated
					foreach (string s in Args.Split(':')[1].Split(' ')) {
						Program.Bot.Channels.Get(Recipient)?.AddUser(s);
					}
					break;
				default:
					if (!SplitArgs[0].Replace(",", string.Empty).Equals(Program.Bot.Config.Nickname.ToLower()) ||
						Program.Bot.IgnoreList.Contains(Realname)) {
						break;
					}

					if (SplitArgs.Count < 2) {
						Writer.Privmsg(Recipient, "Please provide a command. Type 'eve help' to view my command list.");
						break;
					}

					if (!Program.Bot.HasCommand(SplitArgs[1].ToLower())) break;

					// built-in `help' command
					if (SplitArgs[1].ToLower().Equals("help")) {
						if (SplitArgs.Count == 2) {
							Writer.SendData(Protocols.PRIVMSG,
								$"{Recipient} Active commands: {string.Join(", ", Program.Bot.GetCommands())}");
						} else {
							if (SplitArgs.Count < 3) {
								Writer.Privmsg(Recipient, "Insufficient parameters.");
								break;
							}

							if (!PluginWrapper.Commands.ContainsKey(SplitArgs[2])) {
								Writer.Privmsg(Recipient, "Command not found.");
								break;
							}

							Writer.Privmsg(Recipient, $"{SplitArgs[2]} {Program.Bot.GetCommands(SplitArgs[1])}");
						}
					}

					Writer.Privmsg(Recipient, "Invalid command. Type 'eve help' to view my command list.");
					break;
			}

			return true;
		}
	}
}