﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Eve.Ref.Irc;
using Eve.Types;

namespace Eve.Core {
	public class Join : IModule {
		public Dictionary<string, string> Def => new Dictionary<string, string> {
			["join"] = "(<channel>) — joins specified channel."
		};

		public ChannelMessage OnChannelMessage(ChannelMessage c, PropertyReference v) {
			if (!c.MultiArgs[1].CaseEquals(Def.Keys.First()))
				return null;

			if (v.CurrentUser.Access > 1)
				c.Message = "Insufficient permissions.";
			else if (string.IsNullOrEmpty(c.MultiArgs[2]))
				c.Message = "Insufficient parameters. Type 'eve help join' to view ccommand's help index.";
			else if (!c.MultiArgs[2].StartsWith("#"))
				c.Message = "Channel c._Argsument must be a proper channel name (i.e. starts with '#').";
			else if (Channel.CheckExists(c.MultiArgs[2].ToLower()))
				c.Message = "I'm already in that channel.";

			if (!string.IsNullOrEmpty(c.Message)) {
				c.Type = Protocols.Privmsg;
				return c;
			}

			c.Target = string.Empty;
			c.Message = c.MultiArgs[2];
			c.Type = Protocols.Join;
			return c;
		}
	}

	public class Part : IModule {
		public Dictionary<string, string> Def => new Dictionary<string, string> {
			["part"] = "(<channel> *<message>) — parts from specified channel."
		};

		public ChannelMessage OnChannelMessage(ChannelMessage c, PropertyReference v) {
			if (!c.MultiArgs[1].CaseEquals(Def.Keys.First()))
				return null;

			if (v.CurrentUser.Access > 1)
				c.Message = "Insufficient permissions.";
			else if (string.IsNullOrEmpty(c.MultiArgs[2]))
				c.Message = "Insufficient parameters. Type 'eve help part' to view ccommand's help index.";
			else if (!c.MultiArgs[2].StartsWith("#"))
				c.Message = "Channel c._Argsument must be a proper channel name (i.e. starts with '#').";
			else if (!Channel.CheckExists(c.MultiArgs[2].ToLower()))
				c.Message = "I'm not in that channel.";

			if (!string.IsNullOrEmpty(c.Message)) {
				c.Type = Protocols.Privmsg;
				return c;
			}

			Channel.Remove(c.MultiArgs[2].ToLower());

			c.Target = string.Empty;
			c.Message = c.MultiArgs.Count > 3 ? $"{c.MultiArgs[2]} {c.MultiArgs[3]}" : c.MultiArgs[2];
			c.Type = Protocols.Part;
			return c;
		}
	}

	public class Say : IModule {
		public Dictionary<string, string> Def => new Dictionary<string, string> {
			["say"] = "(*<channel> <message>) — returns specified message to (optionally) specified channel."
		};

		public ChannelMessage OnChannelMessage(ChannelMessage c, PropertyReference v) {
			if (!c.MultiArgs[1].CaseEquals(Def.Keys.First()))
				return null;

			if (c.MultiArgs.Count < 3
				||
				(c.MultiArgs[2].StartsWith("#")
				 && c.MultiArgs.Count < 4)) {
				c.Message = "Insufficient parameters. Type 'eve help say' to view ccommand's help index.";
				return c;
			}

			string msg = c.MultiArgs[2].StartsWith("#")
				? $"{c.MultiArgs[2]} {c.MultiArgs[3]}"
				: c.MultiArgs[2];
			string chan = c.MultiArgs[2].StartsWith("#") ? c.MultiArgs[2] : string.Empty;

			c.Message = string.IsNullOrEmpty(chan) ? msg : $"{chan} {msg}";
			return c;
		}
	}

	public class Channels : IModule {
		public Dictionary<string, string> Def => new Dictionary<string, string> {
			["channels"] = "ouputs a list of channels currently connected to."
		};

		public ChannelMessage OnChannelMessage(ChannelMessage c, PropertyReference v) {
			if (!c.MultiArgs[1].CaseEquals(Def.Keys.First()))
				return null;

			c.Message = string.Join(", ", Channel.List());
			return c;
		}
	}

	public class SaveMessage : IModule {
		public Dictionary<string, string> Def => new Dictionary<string, string> {
			["message"] =
				"(<recipient> <message>) — saves message to be sent to specified recipient upon their rejoining a channel."
		};

		public ChannelMessage OnChannelMessage(ChannelMessage c, PropertyReference v) {
			if (!c.MultiArgs[1].CaseEquals(Def.Keys.First()))
				return null;

			if (c.MultiArgs.Count > 3)
				c.MultiArgs[2] = c.MultiArgs[2].ToLower();

			if (c.MultiArgs.Count < 4)
				c.Message = "Insufficient parameters. Type 'eve help message' to view correct usage.";
			else if (v.QueryName(c.MultiArgs[2]) == null)
				c.Message = "User does not exist in database.";

			if (!string.IsNullOrEmpty(c.Message)) {
				c.ExitType = ExitType.MessageAndExit;
				return c;
			}

			string who = Regex.Escape(c.MultiArgs[2]);
			Message m = new Message {
				Sender = c.Nickname,
				Contents = Regex.Escape(c.MultiArgs[3]),
				Date = DateTime.UtcNow
			};

			User.AddMessage(who, m);

			c.Message = $"Message recorded and will be sent to {who}";

			try {
				IrcBot.QueryDefaultDatabase($"INSERT INTO messages VALUES ({v.QueryName(who).Id}, '{m.Sender}', '{m.Contents}', '{m.Date}')");
			} catch (Exception e) {
				Console.WriteLine($"Error occured attepting to add message to database: {e}");
				c.Message = "Error occured attempting to save message.";
			}

			return c;
		}
	}

	public class Help : IModule {
		public Dictionary<string, string> Def => new Dictionary<string, string> {
			["help"] = "(*<command>) — prints the definition index for specified command."
		};

		public ChannelMessage OnChannelMessage(ChannelMessage c, PropertyReference v) {
			if (!c.MultiArgs[1].CaseEquals(Def.Keys.First()))
				return null;

			if (c.MultiArgs.Count > 2 &&
				!v.HasCommand(c.MultiArgs[2])) {
				c.Message = "Command does not exist.";
				return c;
			}

			c.Message = c.MultiArgs.Count < 3
				? $"My commands: {v.GetCommands()}"
				: $"{c.MultiArgs[2]}: {v.GetCommands(c.MultiArgs[2])}";

			return c;
		}
	}

	public class GetUser : IModule {
		public Dictionary<string, string> Def => new Dictionary<string, string> {
			["user"] = "(<user>) — returns stored nickname and access level of specified user."
		};

		public ChannelMessage OnChannelMessage(ChannelMessage c, PropertyReference v) {
			if (!c.MultiArgs[1].CaseEquals(Def.Keys.First()))
				return null;

			c.MultiArgs[2] = c.MultiArgs[2].ToLower();

			if (c.MultiArgs.Count < 3)
				c.Message = "Insufficient parameters. Type 'eve help user' to view command's help index.";
			else if (v.QueryName(c.MultiArgs[2]) == null)
				c.Message = $"User {c.MultiArgs[2]} does not exist in database.";

			if (!string.IsNullOrEmpty(c.Message))
				return c;

			c.Message = $"{v.QueryName(c.MultiArgs[2]).Realname}({v.QueryName(c.MultiArgs[2]).Access})";

			return c;
		}
	}

	public class Seen : IModule {
		public Dictionary<string, string> Def => new Dictionary<string, string> {
			["seen"] = "(<user>) — returns a DateTime of the last message by user in any channel."
		};

		public ChannelMessage OnChannelMessage(ChannelMessage c, PropertyReference v) {
			if (!c.MultiArgs[1].CaseEquals(Def.Keys.First()))
				return null;

			if (c.MultiArgs.Count < 3)
				c.Message = "Insufficient parameters. Type 'eve help seen' to view correct usage.";
			else if (v.QueryName(c.MultiArgs[2].ToLower()) == null)
				c.Message = "User does not exist in database.";

			if (!string.IsNullOrEmpty(c.Message))
				return c;

			User u = v.QueryName(c.MultiArgs[2].ToLower());
			c.Message = $"{u.Realname} was last seen on: {u.Seen} (UTC)";

			return c;
		}
	}

	public class SetAccess : IModule {
		public Dictionary<string, string> Def => new Dictionary<string, string> {
			["setaccess"] = "(<user> <new access>) — updates specified user's access level."
		};

		public ChannelMessage OnChannelMessage(ChannelMessage c, PropertyReference v) {
			if (!c.MultiArgs[1].CaseEquals(Def.Keys.First()))
				return null;

			int i = 3;
			if (c.MultiArgs.Count > 2) c.MultiArgs[2] = c.MultiArgs[2].ToLower();

			if (c.MultiArgs.Count < 4)
				c.Message = "Insuffient parameters. Type 'eve help setaccess' to view command's help index.";
			else if (v.QueryName(c.MultiArgs[2]) == null)
				c.Message = "User does not exist in database.";
			else if (!int.TryParse(c.MultiArgs[3], out i))
				c.Message = "Invalid access parameter.";
			else if (i > 9 ||
					 i < 1)
				c.Message = "Invalid access setting. Please use a number between 1 and 9.";
			else if (v.CurrentUser.Access > 1)
				c.Message = "Insufficient permissions. Only users with an access level of 1 or 0 can promote.";

			if (!string.IsNullOrEmpty(c.Message))
				return c;

			IrcBot.QueryDefaultDatabase($"UPDATE users SET access={i} WHERE id={v.QueryName(c.MultiArgs[2]).Id}");

			User.SetAccess(c.MultiArgs[2], i);
			c.Message = $"User {c.MultiArgs[2]}'s access changed to ({i}).";

			return c;
		}
	}

	public class About : IModule {
		public Dictionary<string, string> Def => new Dictionary<string, string> {
			["about"] = "returns general information about Evealyn IRCBot."
		};

		public ChannelMessage OnChannelMessage(ChannelMessage c, PropertyReference v) {
			if (!c.MultiArgs[1].CaseEquals(Def.Keys.First()))
				return null;

			c.Message = v.Info;
			return c;
		}
	}

	//public class GetModules : IModule {
	//	public Dictionary<string, string> Def => new Dictionary<string, string> {
	//		["modules"] = "returns a list of all currently active modules."
	//	};

	//	public ChannelMessage OnChannelMessage(ChannelMessage c, PropertyReference v) {
	//		if (!c.MultiArgs[1].CaseEquals(Def.Keys.First()))
	//			return null;

	//		c.Message = $"Active modules: {string.Join(", ", v.Modules)}";
	//		return c;
	//	}
	//}

	//public class ReloadModules : IModule {
	//	public Dictionary<string, string> Def => new Dictionary<string, string> {
	//		["reload"] = "(<module>) — reloads specified module."
	//	};

	//	public ChannelMessage OnChannelMessage(ChannelMessage c, PropertyReference v) {
	//		if (!c.MultiArgs[1].CaseEquals(Def.Keys.First()))
	//			return null;

	//		if (v.CurrentUser.Access > 1) {
	//			c.Message = "Insufficient permissions.";
	//			return c;
	//		}

	//		v.ReloadModules();

	//		c.ExitType = ExitType.MessageAndExit;
	//		c.Message = "Modules reloaded.";
	//		return c;
	//	}
	//}

	public class Quit : IModule {
		public Dictionary<string, string> Def => new Dictionary<string, string> {
			["quit"] = "ends program's execution."
		};

		public ChannelMessage OnChannelMessage(ChannelMessage c, PropertyReference v) {
			if (!c.MultiArgs[1].CaseEquals(Def.Keys.First()))
				return null;

			if (v.CurrentUser.Access == 0) {
				Environment.Exit(0);
			} else c.Message = "Insufficient permissions.";

			return c;
		}
	}
}