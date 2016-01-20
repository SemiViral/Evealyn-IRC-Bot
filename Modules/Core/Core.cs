using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text.RegularExpressions;
using Eve.Types.Classes;
using Eve.Types.Irc;

namespace Eve.Core {
	public class Core : Utils, IModule {
		public Dictionary<string, string> Def => null;

		public ChannelMessage OnChannelMessage(ChannelMessage c, Variables v) {
			switch (c.Type) {
				case IrcProtocol.Nick:
					c.ExitType = ExitType.Exit;

					using (
						SQLiteCommand com =
							new SQLiteCommand($"UPDATE users SET nickname='{c.Recipient}' WHERE realname='{c.Realname}'", v.Db))
						com.ExecuteNonQuery();
					break;
				case IrcProtocol.Join:
					c.ExitType = ExitType.Exit;

					if (v.QueryName(c.Realname) != null
						&& v.CurrentUser.Messages != null) {
						c.Target = c.Nickname;

						foreach (Message m in v.CurrentUser.Messages)
							c.MultiMessage.Add($"({m.Date}) {m.Sender}: {Regex.Unescape(m.Contents)}");

						v.Users.First(e => e.Realname == c.Realname).Messages = null;

						using (SQLiteCommand x = new SQLiteCommand($"DELETE FROM messages WHERE id={v.CurrentUser.Id}", v.Db))
							x.ExecuteNonQuery();
					}

					AddUserToChannel(c.Recipient, c.Realname, v);
					break;
				case IrcProtocol.Part:
					c.ExitType = ExitType.Exit;
					RemoveUserFromChannel(c.Recipient, c.Realname, v);
					break;
				case IrcProtocol.NameReply:
					c.ExitType = ExitType.Exit;

					// splits the channel user list in half by the :, then splits each user into an array object to be iterated
					foreach (string s in c.Args.Split(':')[1].Split(' '))
						AddUserToChannel(c.Recipient, s, v);
					break;
				default:
					if (!c._Args[0].Replace(",", string.Empty).CaseEquals("eve")
						|| v.IgnoreList.Contains(c.Realname)
						|| GetUserTimeout(c.Realname, v)) {
						c.ExitType = ExitType.Exit;
						return c;
					}

					if (c._Args.Count < 2) {
						c.ExitType = ExitType.MessageAndExit;
						c.Message = "Please provide a command. Type 'eve help' to view my command list.";
					} else if (!v.Commands.ContainsKey(c._Args[1].ToLower())) {
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

		public ChannelMessage OnChannelMessage(ChannelMessage c, Variables v) {
			if (!c._Args[1].CaseEquals("join"))
				return null;

			if (v.CurrentUser.Access > 1)
				c.Message = "Insufficient permissions.";
			else if (string.IsNullOrEmpty(c._Args[2]))
				c.Message = "Insufficient parameters. Type 'eve help join' to view ccommand's help index.";
			else if (!c._Args[2].StartsWith("#"))
				c.Message = "Channel c._Argsument must be a proper channel name (i.e. starts with '#').";
			else if (Utils.CheckChannelExists(c._Args[2].ToLower(), v))
				c.Message = "I'm already in that channel.";

			if (!string.IsNullOrEmpty(c.Message)) {
				c.Type = IrcProtocol.Privmsg;
				return c;
			}

			c.Target = string.Empty;
			c.Message = c._Args[2];
			c.Type = IrcProtocol.Join;
			return c;
		}
	}

	public class Part : IModule {
		public Dictionary<string, string> Def => new Dictionary<string, string> {
			["part"] = "(<channel> *<message>) — parts from specified channel."
		};

		public ChannelMessage OnChannelMessage(ChannelMessage c, Variables v) {
			if (!c._Args[1].CaseEquals("part"))
				return null;

			if (v.CurrentUser.Access > 1)
				c.Message = "Insufficient permissions.";
			else if (string.IsNullOrEmpty(c._Args[2]))
				c.Message = "Insufficient parameters. Type 'eve help part' to view ccommand's help index.";
			else if (!c._Args[2].StartsWith("#"))
				c.Message = "Channel c._Argsument must be a proper channel name (i.e. starts with '#').";
			else if (!Utils.CheckChannelExists(c._Args[2].ToLower(), v))
				c.Message = "I'm not in that channel.";

			if (!string.IsNullOrEmpty(c.Message)) {
				c.Type = IrcProtocol.Privmsg;
				return c;
			}

			v.Channels.RemoveAll(e => e.Name == c._Args[2].ToLower());

			c.Target = string.Empty;
			c.Message = c._Args.Count > 3 ? $"{c._Args[2]} {c._Args[3]}" : c._Args[2];
			c.Type = IrcProtocol.Part;
			return c;
		}
	}

	public class Say : IModule {
		public Dictionary<string, string> Def => new Dictionary<string, string> {
			["say"] = "(*<channel> <message>) — returns specified message to (optionally) specified channel."
		};

		public ChannelMessage OnChannelMessage(ChannelMessage c, Variables v) {
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

		public ChannelMessage OnChannelMessage(ChannelMessage c, Variables v) {
			if (!c._Args[1].CaseEquals("channels"))
				return null;

			c.Message = string.Join(", ", v.Channels.Select(e => e.Name).ToArray());
			return c;
		}
	}

	public class SaveMessage : IModule {
		public Dictionary<string, string> Def => new Dictionary<string, string> {
			["message"] =
				"(<recipient> <message>) — saves message to be sent to specified recipient upon their rejoining a channel."
		};

		public ChannelMessage OnChannelMessage(ChannelMessage c, Variables v) {
			if (!c._Args[1].CaseEquals("message"))
				return null;


			if (c._Args.Count > 3)
				c._Args[2] = c._Args[2].ToLower();

			if (c._Args.Count < 4)
				c.Message = "Insufficient parameters. Type 'eve help message' to view correct usage.";
			else if (v.QueryName(c._Args[2]) == null)
				c.Message = "User does not exist in database.";

			if (!string.IsNullOrEmpty(c.Message))
				return c;

			string who = Regex.Escape(c._Args[2]);
			Message m = new Message {
				Sender = c.Nickname,
				Contents = Regex.Escape(c._Args[3]),
				Date = DateTime.UtcNow
			};

			if (v.QueryName(who).Messages == null)
				v.Users.First(e => e.Realname == who).Messages = new List<Message> { m };
			else
				v.Users.First(e => e.Realname == who).Messages.Add(m);

			using (
				SQLiteCommand x =
					new SQLiteCommand(
						$"INSERT INTO messages VALUES ({v.QueryName(who).Id}, '{m.Sender}', '{m.Contents}', '{m.Date}')",
						v.Db))
				x.ExecuteNonQuery();

			c.Message = $"Message recorded and will be sent to {who}";
			return c;
		}
	}

	public class Help : IModule {
		public Dictionary<string, string> Def => new Dictionary<string, string> {
			["help"] = "(*<command>) — prints the definition index for specified command."
		};

		public ChannelMessage OnChannelMessage(ChannelMessage c, Variables v) {
			if (!c._Args[1].CaseEquals("help"))
				return null;

			if (c._Args.Count > 2
				&& !v.Commands.ContainsKey(c._Args[2])) {
				c.Message = "Command does not exist.";
				return c;
			}

			c.Message = c._Args.Count < 3
				? $"My commands: {string.Join(", ", v.Commands.Keys)}"
				: $"{c._Args[2]}: {v.Commands[c._Args[2]]}";

			return c;
		}
	}

	public class GetUser : IModule {
		public Dictionary<string, string> Def => new Dictionary<string, string> {
			["user"] = "(<user>) — returns stored nickname and access level of specified user."
		};

		public ChannelMessage OnChannelMessage(ChannelMessage c, Variables v) {
			if (c._Args[1] != "user")
				return null;

			c._Args[2] = c._Args[2].ToLower();

			if (c._Args.Count < 3)
				c.Message = "Insufficient parameters. Type 'eve help user' to view command's help index.";
			else if (v.QueryName(c._Args[2]) == null)
				c.Message = $"User {c._Args[2]} does not exist in database.";

			if (!string.IsNullOrEmpty(c.Message))
				return c;

			c.Message = $"{v.QueryName(c._Args[2]).Realname}({v.QueryName(c._Args[2]).Access})";

			return c;
		}
	}

	public class Seen : IModule {
		public Dictionary<string, string> Def => new Dictionary<string, string> {
			["seen"] = "(<user>) — returns a DateTime of the last message by user in any channel."
		};

		public ChannelMessage OnChannelMessage(ChannelMessage c, Variables v) {
			if (!c._Args[1].CaseEquals("seen"))
				return null;

			c._Args[2] = c._Args[2].ToLower();

			if (c._Args.Count < 3)
				c.Message = "Insufficient parameters. Type 'eve help message' to view correct usage.";
			else if (v.QueryName(c._Args[2]) == null)
				c.Message = "User does not exist in database.";

			if (!string.IsNullOrEmpty(c.Message))
				return c;

			User u = v.QueryName(c._Args[2]);
			c.Message = $"{u.Realname} was last seen on: {u.Seen} (UTC)";

			return c;
		}
	}

	public class SetAccess : IModule {
		public Dictionary<string, string> Def => new Dictionary<string, string> {
			["setaccess"] = "(<user> <new access>) — updates specified user's access level."
		};

		public ChannelMessage OnChannelMessage(ChannelMessage c, Variables v) {
			if (!c._Args[1].CaseEquals("setaccess"))
				return null;

			int i = 2;
			if (c._Args.Count > 2) {
				c._Args[2] = c._Args[2].ToLower();
			}

			if (c._Args.Count < 4)
				c.Message = "Insuffient parameters. Type 'eve help setaccess' to view command's help index.";
			else if (v.QueryName(c._Args[2]) == null)
				c.Message = "User does not exist in database.";
			else if (!int.TryParse(c._Args[3], out i))
				c.Message = "Invalid access parameter.";
			else if (i > 9 || i < 1)
				c.Message = "Invalid access setting. Please use a number between 1 and 9.";
			else if (v.CurrentUser.Access > 1)
				c.Message = "Insufficient permissions. Only users with an access level of 1 or 0 can promote.";

			if (!string.IsNullOrEmpty(c.Message))
				return c;

			SQLiteCommand com = new SQLiteCommand($"UPDATE users SET access={i} WHERE id={v.QueryName(c._Args[2]).Id}", v.Db);
			com.ExecuteNonQuery();

			v.Users.First(e => e.Realname == c._Args[2]).Access = i;
			c.Message = $"User {c._Args[2]}'s access changed to ({i}).";

			return c;
		}
	}

	public class About : IModule {
		public Dictionary<string, string> Def => new Dictionary<string, string> {
			["about"] = "returns general information about Evealyn IRCBot."
		};

		public ChannelMessage OnChannelMessage(ChannelMessage c, Variables v) {
			if (!c._Args[1].CaseEquals("about"))
				return null;

			c.Message = v.Info;
			return c;
		}
	}

	public class GetModules : IModule {
		public Dictionary<string, string> Def => new Dictionary<string, string> {
			["modules"] = "returns a list of all currently active modules."
		};

		public ChannelMessage OnChannelMessage(ChannelMessage c, Variables v) {
			if (c._Args[1] != "modules")
				return null;

			c.Message = $"Active modules: {string.Join(", ", v.Modules.Keys)}";
			return c;
		}
	}

	public class Reload : IModule {
		public Dictionary<string, string> Def => new Dictionary<string, string> {
			["reload"] = "(<module>) — reloads specified module."
		};

		public ChannelMessage OnChannelMessage(ChannelMessage c, Variables v) {
			if (!c._Args[1].CaseEquals("reload"))
				return null;


			if (v.CurrentUser.Access > 1) {
				c.Message = "Insufficient permissions.";
				return c;
			}

			v.Modules.Clear();
			v.Commands.Clear();

			v.Modules = ModuleManager.LoadModules(v.Commands);

			c.ExitType = ExitType.MessageAndExit;
			c.Message = "Modules reloaded.";
			return c;
		}
	}

	public class Quit : IModule {
		public Dictionary<string, string> Def => new Dictionary<string, string> {
			["quit"] = "ends program's execution."
		};

		public ChannelMessage OnChannelMessage(ChannelMessage c, Variables v) {
			if (!c._Args[1].CaseEquals("quit"))
				return null;

			if (v.CurrentUser.Access < 1) {
				c.Message = "Goodybe.";
				Environment.Exit(0);
			} else c.Message = "Insufficient permissions.";

			return c;
		}
	}
}