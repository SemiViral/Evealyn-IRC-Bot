using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Eve.Utilities;

namespace Eve.Core {
	public class Core : IModule {
		private ChannelMessage o = new ChannelMessage {
			Type = "PRIVMSG",
			Args = null
		};

		private Variables v = Eve.IRCBot.v;

		public Dictionary<String, String> def {
			get {
				return null;
			}
		}

		public ChannelMessage OnChannelMessage(ChannelMessage c) {
			o.Nickname = c.Recipient;

			switch (c.Type) {
				case "JOIN":
					if (c.Realname == "Eve") return null;

					if (v.QueryName(c.Realname) != null
						&& v.currentUser.Messages != null) {
						foreach (Eve.Message m in v.currentUser.Messages)
							o._Args.Add($"({m.Date}) {m.Sender}: {Regex.Unescape(m.Contents)}");

						v.users.First(e => e.Realname == c.Realname).Messages = null;

						var x = new SQLiteCommand($"DELETE FROM messages WHERE id={v.currentUser.ID}", v.db);
						x.ExecuteNonQuery();
					}

					if (!v.userChannelList.ContainsKey(c.Recipient))
						v.userChannelList.Add(c.Recipient, new List<string>());
					
					v.userChannelList[c.Recipient].Add(c.Realname);
					break;
				case "PART":
					v.userChannelList[c.Recipient].Remove(c.Realname);
					break;
				case "353":
					if (!v.userChannelList.ContainsKey(c.Recipient))
						v.userChannelList.Add(c.Recipient, new List<string>());

					// 353 is the IRC command response for a channel's user list
					MatchCollection key = Regex.Matches(c.Args, @"(#\w+)");
					List<string> list = key.Cast<Match>().Select(match => match.Value).ToList();

					foreach (string s in c.Args.Split(':')[1].Split(' '))
						v.userChannelList[c.Recipient].Add(s);
					break;
			}

			Eve.IRCBot.v = v;
			return o;
		}
	}

	public class Join : IModule {
		ChannelMessage o = new ChannelMessage { Type = "JOIN" };
		Variables v = Eve.IRCBot.v;

		public Dictionary<String, String> def {
			get {
				return new Dictionary<string, string> {
					{ "join", "(<channel>) — joins specified channel." }
				};
			}
		}

		public ChannelMessage OnChannelMessage(ChannelMessage c) {
			if (c._Args[0].Replace(",", string.Empty) != "eve"
				|| c._Args.Count < 2
				|| c._Args[1] != "join")
				return null;

			if (v.currentUser.Access > 1)
				o.Args = "Insufficient permissions.";
			else if (String.IsNullOrEmpty(c._Args[2]))
				o.Args = "Insufficient parameters. Type 'eve help join' to view ccommand's help index.";
			else if (!c._Args[2].StartsWith("#"))
				o.Args = "Channel c._Argsument must be a proper channel name (i.e. starts with '#').";
			else if (v.channels.Contains(c._Args[2]))
				o.Args = "I'm already in that channel.";

			if (!String.IsNullOrEmpty(o.Args))
				return o;

			Eve.IRCBot.v.channels.Add(c._Args[2]);
			o.Args = c._Args[2];
			return o;
		}
	}

	public class Part : IModule {
		ChannelMessage o = new ChannelMessage { Type = "PART" };
		Variables v = Eve.IRCBot.v;

		public Dictionary<String, String> def {
			get {
				return new Dictionary<string, string> {
					{ "part", "(<channel> *<message>) — parts from specified channel." }
				};
			}
		}

		public ChannelMessage OnChannelMessage(ChannelMessage c) {
			if (c._Args[0].Replace(",", string.Empty) != "eve"
				|| c._Args.Count < 2
				|| c._Args[1] != "part")
				return null;

			if (v.currentUser.Access > 1)
				o.Args = "Insufficient permissions.";
			else if (String.IsNullOrEmpty(c._Args[2]))
				o.Args = "Insufficient parameters. Type 'eve help part' to view ccommand's help index.";
			else if (!c._Args[2].StartsWith("#"))
				o.Args = "Channel c._Argsument must be a proper channel name (i.e. starts with '#').";
			else if (!v.channels.Contains(c._Args[2]))
				o.Args = "I'm not in that channel.";

			if (!String.IsNullOrEmpty(o.Args))
				return o;

			o.Args = (c._Args.Count > 3) ? $"{c._Args[2]} {c._Args[3]}" : c._Args[2];

			v.channels.Remove(c._Args[2]);
			v.userChannelList.Remove(c._Args[2]);
			Eve.IRCBot.v = v;

			return o;
		}
	}

	public class Say : IModule {
		ChannelMessage o = new ChannelMessage { Type = "PRIVMSG" };

		public Dictionary<String, String> def {
			get {
				return new Dictionary<string, string> {
					{ "say", "(*<channel> <message>) — returns specified message to (optionally) specified channel." }
				};
			}
		}

		public ChannelMessage OnChannelMessage(ChannelMessage c) {
			if (c._Args[0].Replace(",", string.Empty) != "eve"
				|| c._Args.Count < 2
				|| c._Args[1] != "say")
				return null;

			o.Nickname = c.Recipient;

			if (c._Args.Count < 3
				|| (c._Args[2].StartsWith("#")
				&& c._Args.Count < 4)) {
				o.Args = "Insufficient parameters. Type 'eve help say' to view ccommand's help index.";
				return o;
			}

			string msg = !c._Args[2].StartsWith("#") && c._Args.Count > 3
				? $"{c._Args[2]} {c._Args[3]}" : c._Args[2];

			string chan = (c._Args[2].StartsWith("#")) ? c._Args[2] : null;
			o.Args = (String.IsNullOrEmpty(chan)) ? msg : $"{chan} {msg}";

			return o;
		}
	}

	public class Channels : IModule {
		public Dictionary<String, String> def {
			get {
				return new Dictionary<string, string> {
					{ "channels", "ouputs a list of channels currently connected to." }
				};
			}
		}

		public ChannelMessage OnChannelMessage(ChannelMessage c) {
			if (c._Args[0].Replace(",", string.Empty) != "eve"
				|| c._Args.Count < 2
				|| c._Args[1] != "chanlist")
				return null;

			return new ChannelMessage {
				Type = "PRIVMSG",
				Recipient = c.Recipient,
				Args = string.Join(", ", Eve.IRCBot.v.channels)
			};
		}
	}

	public class Message : IModule {
		ChannelMessage o = new ChannelMessage { Type = "PRIVMSG" };
		Variables v = Eve.IRCBot.v;

		public Dictionary<String, String> def {
			get {
				return new Dictionary<string, string> {
					{ "message", "(<recipient> <message>) — saves message to be sent to specified recipient upon their rejoining a channel." }
				};
			}
		}

		public ChannelMessage OnChannelMessage(ChannelMessage c) {
			if (c._Args[0].Replace(",", string.Empty) != "eve"
				|| c._Args.Count < 2
				|| c._Args[1] != "message")
				return null;

			o.Nickname = c.Recipient;

			if (c._Args.Count < 4)
				o.Args = "Insufficient parameters. Type 'eve help message' to view correct usage.";
			else if (v.QueryName(c._Args[2]) == null)
				o.Args = "User does not exist in database.";
			else if (v.userChannelList.ContainsKey(c._Args[2]))
				o.Args = "User is already online.";

			if (!String.IsNullOrEmpty(o.Args))
				return o;

			string who = Regex.Escape(c._Args[3]);
			Eve.Message m = new Eve.Message {
				Sender = c.Nickname,
				Contents = c._Args[2],
				Date = DateTime.UtcNow
			};

			if (v.QueryName(who).Messages == null)
				v.users.First(e => e.Realname == who).Messages = new List<Eve.Message> { m };
			else
				v.users.First(e => e.Realname == who).Messages.Add(m);

			using (var x = new SQLiteCommand($"INSERT INTO messages (id, sender, message, datetime) VALUES ({v.currentUser.ID}, '{m.Sender}', '{m.Contents}', '{m.Date}')", v.db))
				x.ExecuteNonQuery();

			o.Args = $"Message recorded and will be sent to {who}";
			Eve.IRCBot.v = v;
			return o;
		}
	}

	public class Help : IModule {
		ChannelMessage o = new ChannelMessage { Type = "PRIVMSG" };
		Variables v = Eve.IRCBot.v;

		public Dictionary<String, String> def {
			get {
				return new Dictionary<string, string> {
					{ "help", "(*<command>) — prints the definition index for specified command." }
				};
			}
		}

		public ChannelMessage OnChannelMessage(ChannelMessage c) {
			if (c._Args[0].Replace(",", string.Empty) != "eve"
				|| c._Args.Count < 2
				|| c._Args[1] != "help")
				return null;

			o.Nickname = c.Recipient;

			if (c._Args.Count > 3
				&& !v.commands.ContainsKey(c._Args[2])) {
				o.Args = "Command does not exist.";
				return o;
			}
			
			string cmd = c._Args.Count < 3 ? null : c._Args[2];
			o.Args = (String.IsNullOrEmpty(cmd)) ?
				$"My commands: {String.Join(", ", v.commands.Keys)}"
				: $"{cmd}: {v.commands[cmd]}";

			Eve.IRCBot.v = v;
			return o;
		}
	}

	public class Users: IModule {
		public Dictionary<String, String> def {
			get {
				return new Dictionary<string, string> {
					{ "users", "returns a list of users in database." }
				};
			}
		}

		public ChannelMessage OnChannelMessage(ChannelMessage c) {
			if (c._Args[0].Replace(",", string.Empty) != "eve"
				|| c._Args.Count < 2
				|| c._Args[1] != "users")
				return null;

			return new ChannelMessage {
				Type = "PRIVMSG",
				Nickname = c.Recipient,
				Args = String.Join(", ", Eve.IRCBot.v.users.
					Select(e => $"{e.Nickname}({e.Access})"))
			};
		}
	}

	public class User : IModule {
		ChannelMessage o = new ChannelMessage { Type = "PRIVMSG" };
		Variables v = Eve.IRCBot.v;

		public Dictionary<String, String> def {
			get {
				return new Dictionary<string, string> {
					{ "user", "(<user>) — returns stored nickname and access level of specified user." }
				};
			}
		}

		public ChannelMessage OnChannelMessage(ChannelMessage c) {
			if (c._Args[0].Replace(",", string.Empty) != "eve"
				|| c._Args.Count < 2
				|| c._Args[1] != "user")
				return null;

			o.Nickname = c.Recipient;

			if (c._Args.Count < 3)
				o.Args = "Insufficient parameters. Type 'eve help user' to view command's help index.";
			else if (v.QueryName(c._Args[2]) == null)
				o.Args = $"User {c._Args[2]} does not exist in database.";

			if (!String.IsNullOrEmpty(o.Args))
				return o;

			o.Args = ($"{v.QueryName(c._Args[2]).Realname}({v.QueryName(c._Args[2]).Access})");

			Eve.IRCBot.v = v;
			return o;
		}
	}

	public class Seen : IModule {
		ChannelMessage o = new ChannelMessage { Type = "PRIVMSG" };
		Variables v = Eve.IRCBot.v;

		public Dictionary<String, String> def {
			get {
				return new Dictionary<string, string> {
					{ "seen", "(<user>) — returns a DateTime of the last message by user in any channel." }
				};
			}
		}

		public ChannelMessage OnChannelMessage(ChannelMessage c) {
			if (c._Args[0].Replace(",", string.Empty) != "eve"
				|| c._Args.Count < 2
				|| c._Args[1] != "seen")
				return null;

			if (c._Args.Count < 3)
				o.Args = "Insufficient parameters. Type 'eve help message' to view correct usage.";
			else if (v.QueryName(c._Args[2]) == null)
				o.Args = "User does not exist in database.";

			if (!String.IsNullOrEmpty(o.Args))
				return o;

			Eve.User u = v.QueryName(c._Args[2]);
			o.Args = $"{u.Realname} was last seen on: {u.Seen} (UTC)";

			return o;
		}
	}

	public class SetAccess : IModule {
		ChannelMessage o = new ChannelMessage { Type = "PRIVMSG" };
		Variables v = Eve.IRCBot.v;

		public Dictionary<String, String> def {
			get {
				return new Dictionary<string, string> {
					{ "setaccess", "(<user> <new access>) — updates specified user's access level." }
				};
			}
		}

		public ChannelMessage OnChannelMessage(ChannelMessage c) {
			if (c._Args[0].Replace(",", string.Empty) != "eve"
				|| c._Args.Count < 2
				|| c._Args[1] != "setaccess")
				return null;

			int i = 2;
			o.Nickname = c.Recipient;

			if (c._Args.Count < 4)
				o.Args = "Insuffient parameters. Type 'eve help setaccess' to view command's help index.";
			else if (v.QueryName(c._Args[2]) == null)
				o.Args = "User does not exist in database.";
			else if (!Int32.TryParse(c._Args[3], out i))
				o.Args = "Invalid access parameter.";
			else if (i > 9 || i < 1)
				o.Args = "Invalid access setting. Please use a number between 1 and 9.";
			else if (v.currentUser.Access > 1)
				o.Args = "Insufficient permissions. Only users with an access level of 1 or 0 can promote.";

			if (!String.IsNullOrEmpty(o.Args))
				return o;
			
			var com = new SQLiteCommand($"UPDATE users SET access={i} WHERE id={v.QueryName(c._Args[2]).ID}", v.db);
			com.ExecuteNonQuery();

			v.users.First(e => e.Realname == c._Args[2]).Access = i;
			o.Args = $"User {c._Args[2]}'s access changed to ({i}).";

			Eve.IRCBot.v = v;
			return o;
		}
	}

	public class About : IModule {
		public Dictionary<String, String> def {
			get {
				return new Dictionary<string, string> {
					{ "about", "returns general information about Evealyn IRCBot." }
				};
			}
		}

		public ChannelMessage OnChannelMessage(ChannelMessage c) {
			if (c._Args[0].Replace(",", string.Empty) != "eve"
				|| c._Args.Count < 2
				|| c._Args[1] != "about")
				return null;

			return new ChannelMessage {
				Type = "PRIVMSG",
				Nickname = c.Recipient,
				Args = Eve.IRCBot.v.info
			};
		}
	}

	public class Modules : IModule {
		public Dictionary<String, String> def {
			get {
				return new Dictionary<string, string> {
					{ "modules", "returns a list of all currently active modules." }
				};
			}
		}

		public ChannelMessage OnChannelMessage(ChannelMessage c) {
			if (c._Args[0].Replace(",", string.Empty) != "eve"
				|| c._Args.Count < 2
				|| c._Args[1] != "modules")
				return null;

			return new ChannelMessage {
				Type = "PRIVMSG",
				Nickname = c.Recipient,
				Args = $"Active modules: {String.Join(", ", Eve.IRCBot.modules.Keys)}"
			};
		}
	}

	//public class Reload : IModule {
	//	ChannelMessage o = new ChannelMessage { Type = "PRIVMSG" };
	//	Dictionary<string, Type> modules = Eve.IRCBot.modules;
	//	Variables v = Eve.IRCBot.v;

	//	public Dictionary<String, String> def {
	//		get {
	//			return new Dictionary<string, string> {
	//				{ "reload", "(<module>) — reloads specified module." }
	//			};
	//		}
	//	}

	//	public ChannelMessage OnChannelMessage(ChannelMessage c) {
	//		if (c._Args[0].Replace(",", string.Empty) != "eve"
	//			|| c._Args.Count < 2
	//			|| c._Args[1] != "reload")
	//			return null;

	//		o.Nickname = c.Recipient;
	//		if (v.currentUser.Access > 1) {
	//			o.Args = "Insufficient permissions.";
	//			return o;
	//		}

	//		modules.Clear();
	//		v.commands.Clear();
	//		Eve.IRCBot.v = v;

	//		modules = Eve.IRCBot.LoadModules();
	//		Eve.IRCBot.modules = modules;

	//		o.Args = "Module reloaded.";
	//		return o;
	//	}
	//}

	public class Quit : IModule {
		ChannelMessage o = new ChannelMessage { Type = "PRIVMSG" };

		public Dictionary<String, String> def {
			get {
				return new Dictionary<string, string> {
					{ "quit", "ends program's execution." }
				};
			}
		}

		public ChannelMessage OnChannelMessage(ChannelMessage c) {
			if (c._Args[0].Replace(",", string.Empty) != "eve"
				|| c._Args.Count < 2
				|| c._Args[1] != "quit")
				return null;
			
			if (Eve.IRCBot.v.currentUser.Access < 1) {
				o.Args = "Goodybe.";
				Environment.Exit(0);
			} else o.Args = "Insufficient permissions.";

			o.Nickname = c.Recipient;
			return o;
		}
	}
}
