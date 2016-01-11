using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text.RegularExpressions;
using Eve.Utilities;

namespace Eve.Core {
	public class Core : Utils, IModule {
		private readonly Variables _v = IrcBot.V;

		public ChannelMessage O { get; set; } = new ChannelMessage {
			Type = "PRIVMSG",
			Args = String.Empty
		};

		public Dictionary<string, string> Def => null;

		public ChannelMessage OnChannelMessage(ChannelMessage c) {
			O.Nickname = c.Recipient;

			switch (c.Type) {
				case "JOIN":
					O.ExitType = 0;
					if (c.Realname == "Eve") return O;

					if (_v.QueryName(c.Realname) != null
						&& _v.CurrentUser.Messages != null) {
						foreach (Eve.Message m in _v.CurrentUser.Messages)
							O._Args.Add($"({m.Date}) {m.Sender}: {Regex.Unescape(m.Contents)}");

						_v.Users.First(e => e.Realname == c.Realname).Messages = null;

						SQLiteCommand x = new SQLiteCommand($"DELETE FROM messages WHERE id={_v.CurrentUser.Id}", _v.Db);
						x.ExecuteNonQuery();
					}

					if (!_v.UserChannelList.ContainsKey(c.Recipient))
						_v.UserChannelList.Add(c.Recipient, new List<string>());

					_v.UserChannelList[c.Recipient].Add(c.Realname);
					break;
				case "PART":
					O.ExitType = 0;
					_v.UserChannelList[c.Recipient].Remove(c.Realname);
					break;
				case "353":
					O.ExitType = 0;
					if (!_v.UserChannelList.ContainsKey(c.Recipient))
						_v.UserChannelList.Add(c.Recipient, new List<string>());

					// splits the channel user list in half by the :, then splits each user into an array object to iterated
					foreach (string s in c.Args.Split(':')[1].Split(' '))
						_v.UserChannelList[c.Recipient].Add(s);
					break;
				default:
					if (!c._Args[0].Replace(",", String.Empty).CaseEquals("eve")
						|| _v.IgnoreList.Contains(c.Realname)
						|| GetUserTimeout(c.Realname)) {
						O.ExitType = 0;
						return O;
					}

					if (c._Args.Count < 2) {
						O.ExitType = 1;
						O.Args = "Please provide a command. Type 'eve help' to view my command list.";
					}
					else if (!_v.Commands.ContainsKey(c._Args[1].ToLower())) {
						O.ExitType = 1;
						O.Args = "Invalid command. Type 'eve help' to view my command list.";
					}

					break;
			}

			IrcBot.V = _v;
			return O;
		}
	}

	public class Join : IModule {
		private readonly ChannelMessage _o = new ChannelMessage {Type = "JOIN"};
		private readonly Variables _v = IrcBot.V;

		public Dictionary<string, string> Def => new Dictionary<string, string> {
			{"join", "(<channel>) — joins specified channel."}
		};

		public ChannelMessage OnChannelMessage(ChannelMessage c) {
			if (!c._Args[1].CaseEquals("join"))
				return _o;

			if (_v.CurrentUser.Access > 1)
				_o.Args = "Insufficient permissions.";
			else if (String.IsNullOrEmpty(c._Args[2]))
				_o.Args = "Insufficient parameters. Type 'eve help join' to view ccommand's help index.";
			else if (!c._Args[2].StartsWith("#"))
				_o.Args = "Channel c._Argsument must be a proper channel name (i.e. starts with '#').";
			else if (_v.Channels.Contains(c._Args[2].ToLower()))
				_o.Args = "I'm already in that channel.";

			if (!String.IsNullOrEmpty(_o.Args)) {
				_o.Type = "PRIVMSG";
				return _o;
			}

			IrcBot.V.Channels.Add(c._Args[2].ToLower());
			_o.Args = c._Args[2];
			return _o;
		}
	}

	public class Part : IModule {
		private readonly ChannelMessage _o = new ChannelMessage {Type = "PART"};
		private readonly Variables _v = IrcBot.V;

		public Dictionary<string, string> Def => new Dictionary<string, string> {
			["part"] = "(<channel> *<message>) — parts from specified channel."
		};

		public ChannelMessage OnChannelMessage(ChannelMessage c) {
			if (!c._Args[1].CaseEquals("part"))
				return _o;

			if (_v.CurrentUser.Access > 1)
				_o.Args = "Insufficient permissions.";
			else if (String.IsNullOrEmpty(c._Args[2]))
				_o.Args = "Insufficient parameters. Type 'eve help part' to view ccommand's help index.";
			else if (!c._Args[2].StartsWith("#"))
				_o.Args = "Channel c._Argsument must be a proper channel name (i.e. starts with '#').";
			else if (!_v.Channels.Contains(c._Args[2].ToLower()))
				_o.Args = "I'm not in that channel.";

			if (!String.IsNullOrEmpty(_o.Args)) {
				_o.Type = "PRIVMSG";
				return _o;
			}

			_o.Args = c._Args.Count > 3 ? $"{c._Args[2]} {c._Args[3]}" : c._Args[2];

			_v.Channels.Remove(c._Args[2]);
			_v.UserChannelList.Remove(c._Args[2]);
			IrcBot.V = _v;

			return _o;
		}
	}

	public class Say : IModule {
		private readonly ChannelMessage _o = new ChannelMessage {Type = "PRIVMSG"};

		public Dictionary<string, string> Def => new Dictionary<string, string> {
			{"say", "(*<channel> <message>) — returns specified message to (optionally) specified channel."}
		};

		public ChannelMessage OnChannelMessage(ChannelMessage c) {
			if (!c._Args[1].CaseEquals("say"))
				return _o;

			_o.Nickname = c.Recipient;
			Console.WriteLine(c._Args.Count);
			if (c._Args.Count < 3
				|| (c._Args[2].StartsWith("#")
					&& c._Args.Count < 4)) {
				_o.Args = "Insufficient parameters. Type 'eve help say' to view ccommand's help index.";
				return _o;
			}

			string msg = c._Args[2].StartsWith("#")
				? $"{c._Args[2]} {c._Args[3]}"
				: c._Args[2];
			string chan = c._Args[2].StartsWith("#") ? c._Args[2] : String.Empty;

			_o.Args = String.IsNullOrEmpty(chan) ? msg : $"{chan} {msg}";
			return _o;
		}
	}

	public class Channels : IModule {
		public Dictionary<string, string> Def => new Dictionary<string, string> {
			{"channels", "ouputs a list of channels currently connected to."}
		};

		public ChannelMessage OnChannelMessage(ChannelMessage c) {
			if (!c._Args[1].CaseEquals("channels"))
				return null;

			return new ChannelMessage {
				Type = "PRIVMSG",
				Nickname = c.Recipient,
				Args = string.Join(", ", IrcBot.V.Channels)
			};
		}
	}

	public class Message : IModule {
		private readonly ChannelMessage _o = new ChannelMessage {Type = "PRIVMSG"};
		private readonly Variables _v = IrcBot.V;

		public Dictionary<string, string> Def => new Dictionary<string, string> {
			{
				"message",
				"(<recipient> <message>) — saves message to be sent to specified recipient upon their rejoining a channel."
			}
		};

		public ChannelMessage OnChannelMessage(ChannelMessage c) {
			if (!c._Args[1].CaseEquals("message"))
				return _o;

			_o.Nickname = c.Recipient;
			c._Args[2] = c._Args[2] ?? c._Args[2].ToLower();

			if (c._Args.Count < 4)
				_o.Args = "Insufficient parameters. Type 'eve help message' to view correct usage.";
			else if (_v.QueryName(c._Args[2]) == null)
				_o.Args = "User does not exist in database.";
			else if (_v.UserChannelList.ContainsKey(c._Args[2]))
				_o.Args = "User is already online.";

			if (!String.IsNullOrEmpty(_o.Args))
				return _o;

			string who = Regex.Escape(c._Args[3]);
			Eve.Message m = new Eve.Message {
				Sender = c.Nickname,
				Contents = c._Args[2],
				Date = DateTime.UtcNow
			};

			if (_v.QueryName(who).Messages == null)
				_v.Users.First(e => e.Realname == who).Messages = new List<Eve.Message> {m};
			else
				_v.Users.First(e => e.Realname == who).Messages.Add(m);

			using (
				SQLiteCommand x =
					new SQLiteCommand(
						$"INSERT INTO messages (id, sender, message, datetime) VALUES ({_v.CurrentUser.Id}, '{m.Sender}', '{m.Contents}', '{m.Date}')",
						_v.Db))
				x.ExecuteNonQuery();

			_o.Args = $"Message recorded and will be sent to {who}";
			IrcBot.V = _v;
			return _o;
		}
	}

	public class Help : IModule {
		private readonly ChannelMessage _o = new ChannelMessage {Type = "PRIVMSG"};
		private readonly Variables _v = IrcBot.V;

		public Dictionary<string, string> Def => new Dictionary<string, string> {
			{"help", "(*<command>) — prints the definition index for specified command."}
		};

		public ChannelMessage OnChannelMessage(ChannelMessage c) {
			if (!c._Args[1].CaseEquals("help"))
				return _o;

			_o.Nickname = c.Recipient;

			if (c._Args.Count > 2
				&& !_v.Commands.ContainsKey(c._Args[2])) {
				_o.Args = "Command does not exist.";
				return _o;
			}

			_o.Args = String.IsNullOrEmpty(c._Args[2])
				? $"My commands: {String.Join(", ", _v.Commands.Keys)}"
				: $"{c._Args[2]}: {_v.Commands[c._Args[2]]}";

			return _o;
		}
	}

	public class User : IModule {
		private readonly ChannelMessage _o = new ChannelMessage {Type = "PRIVMSG"};
		private readonly Variables _v = IrcBot.V;

		public Dictionary<string, string> Def => new Dictionary<string, string> {
			{"user", "(<user>) — returns stored nickname and access level of specified user."}
		};

		public ChannelMessage OnChannelMessage(ChannelMessage c) {
			if (c._Args[1] != "user")
				return _o;

			_o.Nickname = c.Recipient;
			c._Args[2] = c._Args[2].ToLower();

			if (c._Args.Count < 3)
				_o.Args = "Insufficient parameters. Type 'eve help user' to view command's help index.";
			else if (_v.QueryName(c._Args[2]) == null)
				_o.Args = $"User {c._Args[2]} does not exist in database.";

			if (!String.IsNullOrEmpty(_o.Args))
				return _o;

			_o.Args = $"{_v.QueryName(c._Args[2]).Realname}({_v.QueryName(c._Args[2]).Access})";

			return _o;
		}
	}

	public class Seen : IModule {
		private readonly ChannelMessage _o = new ChannelMessage {Type = "PRIVMSG"};
		private readonly Variables _v = IrcBot.V;

		public Dictionary<string, string> Def => new Dictionary<string, string> {
			{"seen", "(<user>) — returns a DateTime of the last message by user in any channel."}
		};

		public ChannelMessage OnChannelMessage(ChannelMessage c) {
			if (!c._Args[1].CaseEquals("seen"))
				return _o;

			_o.Nickname = c.Recipient;
			c._Args[2] = c._Args[2].ToLower();

			if (c._Args.Count < 3)
				_o.Args = "Insufficient parameters. Type 'eve help message' to view correct usage.";
			else if (_v.QueryName(c._Args[2]) == null)
				_o.Args = "User does not exist in database.";

			if (!String.IsNullOrEmpty(_o.Args))
				return _o;

			Eve.User u = _v.QueryName(c._Args[2]);
			_o.Args = $"{u.Realname} was last seen on: {u.Seen} (UTC)";

			return _o;
		}
	}

	public class SetAccess : IModule {
		private readonly ChannelMessage _o = new ChannelMessage {Type = "PRIVMSG"};
		private readonly Variables _v = IrcBot.V;

		public Dictionary<string, string> Def => new Dictionary<string, string> {
			{"setaccess", "(<user> <new access>) — updates specified user's access level."}
		};

		public ChannelMessage OnChannelMessage(ChannelMessage c) {
			if (!c._Args[1].CaseEquals("setaccess"))
				return _o;

			var i = 2;
			_o.Nickname = c.Recipient;
			c._Args[2] = c._Args[2].ToLower();

			if (c._Args.Count < 4)
				_o.Args = "Insuffient parameters. Type 'eve help setaccess' to view command's help index.";
			else if (_v.QueryName(c._Args[2]) == null)
				_o.Args = "User does not exist in database.";
			else if (!Int32.TryParse(c._Args[3], out i))
				_o.Args = "Invalid access parameter.";
			else if (i > 9 || i < 1)
				_o.Args = "Invalid access setting. Please use a number between 1 and 9.";
			else if (_v.CurrentUser.Access > 1)
				_o.Args = "Insufficient permissions. Only users with an access level of 1 or 0 can promote.";

			if (!String.IsNullOrEmpty(_o.Args))
				return _o;

			SQLiteCommand com = new SQLiteCommand($"UPDATE users SET access={i} WHERE id={_v.QueryName(c._Args[2]).Id}", _v.Db);
			com.ExecuteNonQuery();

			_v.Users.First(e => e.Realname == c._Args[2]).Access = i;
			_o.Args = $"User {c._Args[2]}'s access changed to ({i}).";

			IrcBot.V = _v;
			return _o;
		}
	}

	public class About : IModule {
		public Dictionary<string, string> Def => new Dictionary<string, string> {
			{"about", "returns general information about Evealyn IRCBot."}
		};

		public ChannelMessage OnChannelMessage(ChannelMessage c) {
			if (!c._Args[1].CaseEquals("about"))
				return null;

			return new ChannelMessage {
				Type = "PRIVMSG",
				Nickname = c.Recipient,
				Args = IrcBot.V.Info
			};
		}
	}

	public class Modules : IModule {
		public Dictionary<string, string> Def => new Dictionary<string, string> {
			{"modules", "returns a list of all currently active modules."}
		};

		public ChannelMessage OnChannelMessage(ChannelMessage c) {
			if (c._Args[1] != "modules")
				return null;

			return new ChannelMessage {
				Type = "PRIVMSG",
				Nickname = c.Recipient,
				Args = $"Active modules: {String.Join(", ", IrcBot.Modules.Keys)}"
			};
		}
	}

	public class Reload : IModule {
		private readonly ChannelMessage _o = new ChannelMessage {Type = "PRIVMSG"};
		private readonly Variables _v = IrcBot.V;
		private Dictionary<string, Type> _modules = IrcBot.Modules;

		public Dictionary<string, string> Def => new Dictionary<string, string> {
			{"reload", "(<module>) — reloads specified module."}
		};

		public ChannelMessage OnChannelMessage(ChannelMessage c) {
			if (!c._Args[1].CaseEquals("reload"))
				return _o;

			_o.Nickname = c.Recipient;
			if (_v.CurrentUser.Access > 1) {
				_o.Args = "Insufficient permissions.";
				return _o;
			}

			_modules.Clear();
			_v.Commands.Clear();
			IrcBot.V = _v;

			_modules = IrcBot.LoadModules();
			IrcBot.Modules = _modules;

			_o.Args = "Modules reloaded.";
			return _o;
		}
	}

	public class Quit : IModule {
		private readonly ChannelMessage _o = new ChannelMessage {Type = "PRIVMSG"};

		public Dictionary<string, string> Def => new Dictionary<string, string> {
			{"quit", "ends program's execution."}
		};

		public ChannelMessage OnChannelMessage(ChannelMessage c) {
			if (!c._Args[1].CaseEquals("quit"))
				return _o;

			if (IrcBot.V.CurrentUser.Access < 1) {
				_o.Args = "Goodybe.";
				Environment.Exit(0);
			}
			else _o.Args = "Insufficient permissions.";

			_o.Nickname = c.Recipient;
			return _o;
		}
	}
}