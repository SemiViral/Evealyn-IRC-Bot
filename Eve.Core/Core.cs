using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text.RegularExpressions;
using Eve.Utilities;

namespace Eve.Core {
	public class Core : Utils, IModule {
		public Dictionary<string, string> Def => null;

		public ChannelMessage OnChannelMessage(ChannelMessage c) {
			switch (c.Type) {
				case "NICK":
					c.ExitType = ExitType.Exit;

					using (
						SQLiteCommand com =
							new SQLiteCommand($"UPDATE users SET nickname='{c.Recipient}' WHERE realname='{c.Realname}'", IrcBot.V.Db))
						com.ExecuteNonQuery();
					break;
				case "JOIN":
					c.ExitType = ExitType.Exit;

					if (IrcBot.V.QueryName(c.Realname) != null
						&& IrcBot.V.CurrentUser.Messages != null) {
						c.Target = c.Nickname;

						foreach (Eve.Message m in IrcBot.V.CurrentUser.Messages)
							c.MultiMessage.Add($"({m.Date}) {m.Sender}: {Regex.Unescape(m.Contents)}");

						IrcBot.V.Users.First(e => e.Realname == c.Realname).Messages = null;

						using (SQLiteCommand x = new SQLiteCommand($"DELETE FROM messages WHERE id={IrcBot.V.CurrentUser.Id}", IrcBot.V.Db))
							x.ExecuteNonQuery();
					}

					AddUserToChannel(c.Recipient, c.Realname);
					break;
				case "PART":
					c.ExitType = ExitType.Exit;
					RemoveUserFromChannel(c.Recipient, c.Realname);
					break;
				case "353":
					c.ExitType = ExitType.Exit;

					// splits the channel user list in half by the :, then splits each user into an array object to iterated
					foreach (string s in c.Args.Split(':')[1].Split(' '))
						AddUserToChannel(c.Recipient, s);
					break;
				default:
					if (!c._Args[0].Replace(",", string.Empty).CaseEquals("eve")
						|| IrcBot.V.IgnoreList.Contains(c.Realname)
						|| GetUserTimeout(c.Realname)) {
						c.ExitType = ExitType.Exit;
						return c;
					}

					if (c._Args.Count < 2) {
						c.ExitType = ExitType.MessageAndExit;
						c.Message = "Please provide a command. Type 'eve help' to view my command list.";
					}
					else if (!IrcBot.V.Commands.ContainsKey(c._Args[1].ToLower())) {
						c.ExitType = ExitType.MessageAndExit;
						c.Message = "Invalid command. Type 'eve help' to view my command list.";
					}

					break;
			}

			return c;
		}
	}

	public class Join : IModule {
		public Dictionary<string, string> Def => new Dictionary<string, string> {
			["join"] = "(<channel>) — joins specified channel."
		};

		public ChannelMessage OnChannelMessage(ChannelMessage c) {
			if (!c._Args[1].CaseEquals("join"))
				return null;

			if (IrcBot.V.CurrentUser.Access > 1)
				c.Message = "Insufficient permissions.";
			else if (string.IsNullOrEmpty(c._Args[2]))
				c.Message = "Insufficient parameters. Type 'eve help join' to view ccommand's help index.";
			else if (!c._Args[2].StartsWith("#"))
				c.Message = "Channel c._Argsument must be a proper channel name (i.e. starts with '#').";
			else if (Utils.CheckChannelExists(c._Args[2].ToLower()))
				c.Message = "I'm already in that channel.";

			if (!string.IsNullOrEmpty(c.Message)) {
				c.Type = "PRIVMSG";
				return c;
			}

			c.Target = string.Empty;
			c.Message = c._Args[2];
			c.Type = "JOIN";
			return c;
		}
	}

	public class Part : IModule {
		public Dictionary<string, string> Def => new Dictionary<string, string> {
			["part"] = "(<channel> *<message>) — parts from specified channel."
		};

		public ChannelMessage OnChannelMessage(ChannelMessage c) {
			if (!c._Args[1].CaseEquals("part"))
				return null;

			if (IrcBot.V.CurrentUser.Access > 1)
				c.Message = "Insufficient permissions.";
			else if (string.IsNullOrEmpty(c._Args[2]))
				c.Message = "Insufficient parameters. Type 'eve help part' to view ccommand's help index.";
			else if (!c._Args[2].StartsWith("#"))
				c.Message = "Channel c._Argsument must be a proper channel name (i.e. starts with '#').";
			else if (!Utils.CheckChannelExists(c._Args[2].ToLower()))
				c.Message = "I'm not in that channel.";

			if (!string.IsNullOrEmpty(c.Message)) {
				c.Type = "PRIVMSG";
				return c;
			}

			IrcBot.V.Channels.RemoveAll(e => e.Name == c._Args[2].ToLower());

			c.Target = string.Empty;
			c.Message = c._Args.Count > 3 ? $"{c._Args[2]} {c._Args[3]}" : c._Args[2];
			c.Type = "PART";
			return c;
		}
	}

	public class Say : IModule {
		public Dictionary<string, string> Def => new Dictionary<string, string> {
			["say"] = "(*<channel> <message>) — returns specified message to (optionally) specified channel."
		};

		public ChannelMessage OnChannelMessage(ChannelMessage c) {
			if (!c._Args[1].CaseEquals("say"))
				return null;

			if (c._Args.Count < 3
				|| (c._Args[2].StartsWith("#")
					&& c._Args.Count < 4)) {
				c.Message = "Insufficient parameters. Type 'eve help say' to view ccommand's help index.";
				return c;
			}

			string msg = c._Args[2].StartsWith("#")
				? $"{c._Args[2]} {c._Args[3]}"
				: c._Args[2];
			string chan = c._Args[2].StartsWith("#") ? c._Args[2] : string.Empty;

			c.Message = string.IsNullOrEmpty(chan) ? msg : $"{chan} {msg}";
			return c;
		}
	}

	public class Channels : IModule {
		public Dictionary<string, string> Def => new Dictionary<string, string> {
			["channels"] = "ouputs a list of channels currently connected to."
		};

		public ChannelMessage OnChannelMessage(ChannelMessage c) {
			if (!c._Args[1].CaseEquals("channels"))
				return null;

			c.Message = string.Join(", ", IrcBot.V.Channels.Select(e => e.Name).ToArray());
			return c;
		}
	}

	public class Message : IModule {
		public Dictionary<string, string> Def => new Dictionary<string, string> {
			["message"] =
				"(<recipient> <message>) — saves message to be sent to specified recipient upon their rejoining a channel."
		};

		public ChannelMessage OnChannelMessage(ChannelMessage c) {
			if (!c._Args[1].CaseEquals("message"))
				return null;


			if (c._Args.Count > 3)
				c._Args[2] = c._Args[2].ToLower();

			if (c._Args.Count < 4)
				c.Message = "Insufficient parameters. Type 'eve help message' to view correct usage.";
			else if (IrcBot.V.QueryName(c._Args[2]) == null)
				c.Message = "User does not exist in database.";

			if (!string.IsNullOrEmpty(c.Message))
				return c;

			string who = Regex.Escape(c._Args[2]);
			Eve.Message m = new Eve.Message {
				Sender = c.Nickname,
				Contents = Regex.Escape(c._Args[3]),
				Date = DateTime.UtcNow
			};

			if (IrcBot.V.QueryName(who).Messages == null)
				IrcBot.V.Users.First(e => e.Realname == who).Messages = new List<Eve.Message> { m };
			else
				IrcBot.V.Users.First(e => e.Realname == who).Messages.Add(m);

			using (
				SQLiteCommand x =
					new SQLiteCommand(
						$"INSERT INTO messages VALUES ({IrcBot.V.QueryName(who).Id}, '{m.Sender}', '{m.Contents}', '{m.Date}')",
						IrcBot.V.Db))
				x.ExecuteNonQuery();

			c.Message = $"Message recorded and will be sent to {who}";
			return c;
		}
	}

	public class Help : IModule {
		public Dictionary<string, string> Def => new Dictionary<string, string> {
			["help"] = "(*<command>) — prints the definition index for specified command."
		};

		public ChannelMessage OnChannelMessage(ChannelMessage c) {
			if (!c._Args[1].CaseEquals("help"))
				return null;

			if (c._Args.Count > 2
				&& !IrcBot.V.Commands.ContainsKey(c._Args[2])) {
				c.Message = "Command does not exist.";
				return c;
			}
			
			c.Message = c._Args.Count < 3
				? $"My commands: {string.Join(", ", IrcBot.V.Commands.Keys)}"
				: $"{c._Args[2]}: {IrcBot.V.Commands[c._Args[2]]}";

			return c;
		}
	}

	public class User : IModule {
		public Dictionary<string, string> Def => new Dictionary<string, string> {
			["user"] = "(<user>) — returns stored nickname and access level of specified user."
		};

		public ChannelMessage OnChannelMessage(ChannelMessage c) {
			if (c._Args[1] != "user")
				return null;

			c._Args[2] = c._Args[2].ToLower();

			if (c._Args.Count < 3)
				c.Message = "Insufficient parameters. Type 'eve help user' to view command's help index.";
			else if (IrcBot.V.QueryName(c._Args[2]) == null)
				c.Message = $"User {c._Args[2]} does not exist in database.";

			if (!string.IsNullOrEmpty(c.Message))
				return c;

			c.Message = $"{IrcBot.V.QueryName(c._Args[2]).Realname}({IrcBot.V.QueryName(c._Args[2]).Access})";

			return c;
		}
	}

	public class Seen : IModule {
		public Dictionary<string, string> Def => new Dictionary<string, string> {
			["seen"] = "(<user>) — returns a DateTime of the last message by user in any channel."
		};

		public ChannelMessage OnChannelMessage(ChannelMessage c) {
			if (!c._Args[1].CaseEquals("seen"))
				return null;

			c._Args[2] = c._Args[2].ToLower();

			if (c._Args.Count < 3)
				c.Message = "Insufficient parameters. Type 'eve help message' to view correct usage.";
			else if (IrcBot.V.QueryName(c._Args[2]) == null)
				c.Message = "User does not exist in database.";

			if (!string.IsNullOrEmpty(c.Message))
				return c;

			Eve.User u = IrcBot.V.QueryName(c._Args[2]);
			c.Message = $"{u.Realname} was last seen on: {u.Seen} (UTC)";

			return c;
		}
	}

	public class SetAccess : IModule {
		public Dictionary<string, string> Def => new Dictionary<string, string> {
			["setaccess"] = "(<user> <new access>) — updates specified user's access level."
		};

		public ChannelMessage OnChannelMessage(ChannelMessage c) {
			if (!c._Args[1].CaseEquals("setaccess"))
				return null;

			int i = 2;
			if (c._Args.Count > 2) {
				c._Args[2] = c._Args[2].ToLower();
			}

			if (c._Args.Count < 4)
				c.Message = "Insuffient parameters. Type 'eve help setaccess' to view command's help index.";
			else if (IrcBot.V.QueryName(c._Args[2]) == null)
				c.Message = "User does not exist in database.";
			else if (!int.TryParse(c._Args[3], out i))
				c.Message = "Invalid access parameter.";
			else if (i > 9 || i < 1)
				c.Message = "Invalid access setting. Please use a number between 1 and 9.";
			else if (IrcBot.V.CurrentUser.Access > 1)
				c.Message = "Insufficient permissions. Only users with an access level of 1 or 0 can promote.";

			if (!string.IsNullOrEmpty(c.Message))
				return c;

			SQLiteCommand com = new SQLiteCommand($"UPDATE users SET access={i} WHERE id={IrcBot.V.QueryName(c._Args[2]).Id}", IrcBot.V.Db);
			com.ExecuteNonQuery();

			IrcBot.V.Users.First(e => e.Realname == c._Args[2]).Access = i;
			c.Message = $"User {c._Args[2]}'s access changed to ({i}).";

			IrcBot.V = IrcBot.V;
			return c;
		}
	}

	public class About : IModule {
		public Dictionary<string, string> Def => new Dictionary<string, string> {
			["about"] = "returns general information about Evealyn IRCBot."
		};

		public ChannelMessage OnChannelMessage(ChannelMessage c) {
			if (!c._Args[1].CaseEquals("about"))
				return null;

			c.Message = IrcBot.V.Info;
			return c;
		}
	}

	public class Modules : IModule {
		public Dictionary<string, string> Def => new Dictionary<string, string> {
			["modules"] = "returns a list of all currently active modules."
		};

		public ChannelMessage OnChannelMessage(ChannelMessage c) {
			if (c._Args[1] != "modules")
				return null;

			c.Message = $"Active modules: {string.Join(", ", IrcBot.Modules.Keys)}";
			return c;
		}
	}

	public class Reload : IModule {
		public Dictionary<string, string> Def => new Dictionary<string, string> {
			["reload"] = "(<module>) — reloads specified module."
		};

		public ChannelMessage OnChannelMessage(ChannelMessage c) {
			if (!c._Args[1].CaseEquals("reload"))
				return null;


			if (IrcBot.V.CurrentUser.Access > 1) {
				c.Message = "Insufficient permissions.";
				return c;
			}

			IrcBot.Modules.Clear();
			IrcBot.V.Commands.Clear();

			IrcBot.Modules = IrcBot.LoadModules();

			c.Message = "Modules reloaded.";
			return c;
		}
	}

	public class Quit : IModule {
		public Dictionary<string, string> Def => new Dictionary<string, string> {
			["quit"] = "ends program's execution."
		};

		public ChannelMessage OnChannelMessage(ChannelMessage c) {
			if (!c._Args[1].CaseEquals("quit"))
				return null;

			if (IrcBot.V.CurrentUser.Access < 1) {
				c.Message = "Goodybe.";
				Environment.Exit(0);
			}
			else c.Message = "Insufficient permissions.";
			
			return c;
		}
	}
}