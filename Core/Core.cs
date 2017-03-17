#region usings

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Eve.Plugin;
using Eve.References;

#endregion

namespace Eve.Core {
    public class Core : IPlugin {
        public string Name => "Core";
        public string Author => "SemiViral";
        public string Version => "3.0.3";
        public string Id => Guid.NewGuid().ToString();
        public bool TerminateRequestRecieved { get; private set; }

        public Dictionary<string, string> Commands => new Dictionary<string, string> {
            ["eval"] = "(<expression>) — evaluates given mathematical expression.",
            ["join"] = "(<channel>) — joins specified channel.",
            ["channels"] = "returns a list of connected channels.",
            ["reload"] = "reloads the plugin domain."
        };

        public PluginStatus Status { get; private set; } = PluginStatus.Stopped;

        public void ProcessEnded() {
            if (!TerminateRequestRecieved) {}
        }

        public void OnChannelMessage(object source, ChannelMessageEventArgs e) {
            Status = PluginStatus.Processing;

            if (!e.MainBot.Inhabitants.Any(f => f.Equals(e.Nickname)) ||
                !Commands.Keys.Contains(e.SplitArgs[1])) return;

            switch (e.SplitArgs[1]) {
                case "reload":
                    Reload(e);
                    break;
                case "eval":
                    Eval(e);
                    break;
                case "join":
                    Join(e);
                    break;
                case "channels":
                    Channels(e);
                    break;
                default:
                    break;
            }

            Status = PluginStatus.Stopped;
        }

        public bool Start() {
            DoCallback(new PluginEventArgs(PluginEventMessageType.Message, $"{Name} loaded."));
            Debug.WriteLine(" 4 >> " + AppDomain.CurrentDomain.FriendlyName);
            return true;
        }

        public bool Stop() {
            if (Status.Equals(PluginStatus.Running)) {
                TerminateRequestRecieved = true;
                DoCallback(new PluginEventArgs(PluginEventMessageType.Message,
                    $"Stop called but process is running from: {Name}"));
            } else {
                TerminateRequestRecieved = true;
                DoCallback(new PluginEventArgs(PluginEventMessageType.Message, $"Stop called from: {Name}"));
                Call_Die();
            }

            return true;
        }

        public void LogError(string message, EventLogEntryType logType) {
            Writer.Log(message, logType);
        }

        public void Call_Die() {
            Status = PluginStatus.Stopped;
            DoCallback(new PluginEventArgs(PluginEventMessageType.Message,
                $"Calling die, stopping process, sending unload —— from: {Name}"));
            DoCallback(PluginEventMessageType.Action, null, PluginActionType.Unload);
        }

        public event EventHandler<PluginEventArgs> CallbackEvent;

        public void DoCallback(PluginEventArgs e) {
            CallbackEvent?.Invoke(this, e);
        }

        public void DoCallback(PluginEventMessageType type, object result,
            PluginActionType actionType = PluginActionType.None) {
            CallbackEvent?.Invoke(this, new PluginEventArgs(type, result, actionType));
        }

        private void Reload(ChannelMessageEventArgs e) {
            PluginReturnMessage message = new PluginReturnMessage(Protocols.PRIVMSG, e.Recipient, string.Empty);

            if (e.MainBot.Users.Single(f => f.Realname.Equals(e.Realname)).Access > 1) {
                message.Args = "Insufficient permissions.";
                DoCallback(PluginEventMessageType.Message, message);
                return;
            }

            message.Args = "Attempting to reload plugins.";
            DoCallback(PluginEventMessageType.Message, message);

            try {
                DoCallback(PluginEventMessageType.Action, null, PluginActionType.Unload);
                DoCallback(PluginEventMessageType.Action, null, PluginActionType.Load);
            } catch (Exception ex) {
                message.Args = "Error occured reloading plugins.";
                DoCallback(PluginEventMessageType.Message, message);
                Writer.Log($"Error reloading plugins: {ex}", EventLogEntryType.Error);
                return;
            }

            message.Args = "Error occured reloading plugins.";
            DoCallback(PluginEventMessageType.Message, message);
        }

        private void Eval(ChannelMessageEventArgs e) {
            PluginReturnMessage message = new PluginReturnMessage(Protocols.PRIVMSG, e.Recipient, string.Empty);

            if (e.SplitArgs.Count < 3) message.Args = "Not enough parameters.";

            if (string.IsNullOrEmpty(message.Args)) {
                Status = PluginStatus.Running;
                string evalArgs = e.SplitArgs.Count > 3 ?
                    e.SplitArgs[2] + e.SplitArgs[3] : e.SplitArgs[2];

                try {
                    message.Args = new Calculator().Evaluate(evalArgs).ToString(CultureInfo.CurrentCulture);
                } catch (Exception ex) {
                    message.Args = ex.Message;
                }
            }

            DoCallback(PluginEventMessageType.Message, message);
        }

        private void Join(ChannelMessageEventArgs e) {
            PluginReturnMessage message = new PluginReturnMessage(Protocols.PRIVMSG, e.Recipient, string.Empty);

            if (e.MainBot.Users.Single(f => f.Realname.Equals(e.Realname)).Access > 1)
                message.Args = "Insufficient permissions.";
            else if (e.SplitArgs.Count < 3)
                message.Args = "Insufficient parameters. Type 'eve help join' to view command's help index.";
            else if (!e.SplitArgs[2].StartsWith("#"))
                message.Args = "Channel name must start with '#'.";
            else if (e.MainBot.Channels.Any(f => f.Name.Equals(e.SplitArgs[2].ToLower())))
                message.Args = "I'm already in that channel.";

            Status = PluginStatus.Running;

            if (string.IsNullOrEmpty(message.Args)) {
                DoCallback(PluginEventMessageType.Message,
                    new PluginReturnMessage(Protocols.JOIN, string.Empty, e.SplitArgs[2]));
                message.Args = $"Joined channel {e.SplitArgs[2]}.";
            }

            DoCallback(PluginEventMessageType.Message, message);
        }

        private void Channels(ChannelMessageEventArgs e) {
            Status = PluginStatus.Running;
            DoCallback(PluginEventMessageType.Message,
                new PluginReturnMessage(Protocols.PRIVMSG, e.Recipient,
                    string.Join(", ", e.MainBot.Channels.SelectMany(f => f.Name))));
        }
    }

    //public class Join : IPlugin {
    //	public Dictionary<string, string> Def => new Dictionary<string, string> {
    //		
    //	};

    //	public ChannelMessage OnChannelMessage(ChannelMessage c, PassableMutableObject v) {
    //		if (!c.SplitArgs[1].CaseEquals(Def.Keys.First()))
    //			return null;

    //	}
    //}

    //public class Part : IPlugin {
    //	public Dictionary<string, string> Def => new Dictionary<string, string> {
    //		["part"] = "(<channel> *<message>) — parts from specified channel."
    //	};

    //	public ChannelMessage OnChannelMessage(ChannelMessage c, PassableMutableObject v) {
    //		if (!c.SplitArgs[1].CaseEquals(Def.Keys.First()))
    //			return null;

    //		if (v.CurrentUser.Access > 1)
    //			c.message = "Insufficient permissions.";
    //		else if (c.SplitArgs.Count < 3)
    //			c.message = "Insufficient parameters. Type 'eve help part' to view ccommand's help index.";
    //		else if (!c.SplitArgs[2].StartsWith("#"))
    //			c.message = "Channel c._Argsument must be a proper channel name (i.e. starts with '#').";
    //		else if (v.GetChannel(c.SplitArgs[2].ToLower()) == null)
    //			c.message = "I'm not in that channel.";

    //		if (!string.IsNullOrEmpty(c.message)) {
    //			c.Type = Protocols.Privmsg;
    //			return c;
    //		}

    //		string channel = c.SplitArgs[2].ToLower();
    //		v.RemoveChannel(channel);

    //		c.Target = string.Empty;
    //		c.message = c.SplitArgs.Count > 3 ? $"{channel} {c.SplitArgs[3]}" : channel;
    //		c.Type = Protocols.Part;
    //		return c;
    //	}
    //}

    //public class Say : IPlugin {
    //	public Dictionary<string, string> Def => new Dictionary<string, string> {
    //		["say"] = "(*<channel> <message>) — returns specified message to (optionally) specified channel."
    //	};

    //	public ChannelMessage OnChannelMessage(ChannelMessage c, PassableMutableObject v) {
    //		if (!c.SplitArgs[1].CaseEquals(Def.Keys.First()))
    //			return null;

    //		if (c.SplitArgs.Count < 3
    //			||
    //			(c.SplitArgs[2].StartsWith("#")
    //			 && c.SplitArgs.Count < 4)) {
    //			c.message = "Insufficient parameters. Type 'eve help say' to view ccommand's help index.";
    //			return c;
    //		}

    //		string msg = c.SplitArgs[2].StartsWith("#")
    //			? $"{c.SplitArgs[2]} {c.SplitArgs[3]}"
    //			: c.SplitArgs[2];
    //		string chan = c.SplitArgs[2].StartsWith("#") ? c.SplitArgs[2] : string.Empty;

    //		c.message = string.IsNullOrEmpty(chan) ? msg : $"{chan} {msg}";
    //		return c;
    //	}
    //}

    //public class Channels : IPlugin {
    //	public Dictionary<string, string> Def => new Dictionary<string, string> {
    //		["channels"] = "ouputs a list of channels currently connected to."
    //	};

    //	public ChannelMessage OnChannelMessage(ChannelMessage c, PassableMutableObject v) {
    //		if (!c.SplitArgs[1].CaseEquals(Def.Keys.First()))
    //			return null;

    //		c.message = string.Join(", ", v.ChannelList().Select(e => e.Name));
    //		return c;
    //	}
    //}

    //public class SaveMessage : IPlugin {
    //	public Dictionary<string, string> Def => new Dictionary<string, string> {
    //		["message"] =
    //			"(<recipient> <message>) — saves message to be sent to specified recipient upon their rejoining a channel."
    //	};

    //	public ChannelMessage OnChannelMessage(ChannelMessage c, PassableMutableObject v) {
    //		if (!c.SplitArgs[1].CaseEquals(Def.Keys.First()))
    //			return null;

    //		string who = string.Empty;

    //		if (c.SplitArgs.Count < 4) c.message = "Insufficient parameters. Type 'eve help message' to view correct usage.";
    //		else who = c.SplitArgs[2].ToLower();

    //		if (v.Get(who) == null)
    //			c.message = "User does not exist in database.";
    //		else if (!v.Get(who).AddMessage(new message {
    //			Sender = c.Nickname,
    //			Contents = Regex.Escape(c.SplitArgs[3]),
    //			Date = DateTime.UtcNow
    //		})) c.message = "Error occurred, aborting operation.";

    //		if (!string.IsNullOrEmpty(c.message)) {
    //			c.ExitType = ExitType.MessageAndExit;
    //			return c;
    //		}

    //		c.message = $"message recorded and will be sent to {who}.";
    //		return c;
    //	}
    //}

    //public class Help : IPlugin {
    //	public Dictionary<string, string> Def => new Dictionary<string, string> {
    //		["help"] = "(*<command>) — prints the definition index for specified command."
    //	};

    //	public ChannelMessage OnChannelMessage(ChannelMessage c, PassableMutableObject v) {
    //		if (!c.SplitArgs[1].CaseEquals(Def.Keys.First()))
    //			return null;

    //		if (c.SplitArgs.Count > 2 &&
    //			!v.HasCommand(c.SplitArgs[2])) {
    //			c.message = "Command does not exist.";
    //			return c;
    //		}

    //		c.message = c.SplitArgs.Count < 3
    //			? $"My commands: {string.Join(", ", v.GetCommands())}"
    //			: $"\x02{c.SplitArgs[2]}\x0F {v.GetCommands(c.SplitArgs[2]).First()}";

    //		return c;
    //	}
    //}

    //public class GetUsers : IPlugin {
    //	public Dictionary<string, string> Def => new Dictionary<string, string> {
    //		["users"] = "returns full list of stored users."
    //	};

    //	public ChannelMessage OnChannelMessage(ChannelMessage c, PassableMutableObject v) {
    //		if (!c.SplitArgs[1].CaseEquals(Def.Keys.First()))
    //			return null;

    //		c.message = $"Inhabitants in database: {string.Join(", ", v.GetUsers().Select(e => e.Realname))}";
    //		return c;
    //	}
    //}

    //public class Get : IPlugin {
    //	public Dictionary<string, string> Def => new Dictionary<string, string> {
    //		["user"] = "(<user>) — returns stored nickname and access level of specified user."
    //	};

    //	public ChannelMessage OnChannelMessage(ChannelMessage c, PassableMutableObject v) {
    //		if (!c.SplitArgs[1].CaseEquals(Def.Keys.First()))
    //			return null;

    //		if (c.SplitArgs.Count < 3)
    //			c.message = "Insufficient parameters. Type 'eve help user' to view command's help index.";
    //		else if (v.Get(c.SplitArgs[2].ToLower()) == null)
    //			c.message = $"User {c.SplitArgs[2]} does not exist in database.";

    //		if (!string.IsNullOrEmpty(c.message))
    //			return c;

    //		string who = c.SplitArgs[2].ToLower();
    //		c.message = $"{v.Get(who).Realname}({v.Get(who).Access})";

    //		return c;
    //	}
    //}

    //public class Seen : IPlugin {
    //	public Dictionary<string, string> Def => new Dictionary<string, string> {
    //		["seen"] = "(<user>) — returns a DateTime of the last message by user in any channel."
    //	};

    //	public ChannelMessage OnChannelMessage(ChannelMessage c, PassableMutableObject v) {
    //		if (!c.SplitArgs[1].CaseEquals(Def.Keys.First()))
    //			return null;

    //		if (c.SplitArgs.Count < 3)
    //			c.message = "Insufficient parameters. Type 'eve help seen' to view correct usage.";
    //		else if (v.Get(c.SplitArgs[2].ToLower()) == null)
    //			c.message = "User does not exist in database.";

    //		if (!string.IsNullOrEmpty(c.message))
    //			return c;

    //		User u = v.Get(c.SplitArgs[2].ToLower());
    //		c.message = $"{u.Realname} was last seen on: {u.Seen} (UTC)";

    //		return c;
    //	}
    //}

    //public class SetAccess : IPlugin {
    //	public Dictionary<string, string> Def => new Dictionary<string, string> {
    //		["setaccess"] = "(<user> <new access>) — updates specified user's access level."
    //	};

    //	public ChannelMessage OnChannelMessage(ChannelMessage c, PassableMutableObject v) {
    //		if (!c.SplitArgs[1].CaseEquals(Def.Keys.First()))
    //			return null;

    //		int i = 3;
    //		if (c.SplitArgs.Count > 2) c.SplitArgs[2] = c.SplitArgs[2].ToLower();

    //		if (c.SplitArgs.Count < 4)
    //			c.message = "Insuffient parameters. Type 'eve help setaccess' to view command's help index.";
    //		else if (v.Get(c.SplitArgs[2]) == null)
    //			c.message = "User does not exist in database.";
    //		else if (!int.TryParse(c.SplitArgs[3], out i))
    //			c.message = "Invalid access parameter.";
    //		else if (i > 9 ||
    //				 i < 1)
    //			c.message = "Invalid access setting. Please use a number between 1 and 9.";
    //		else if (v.CurrentUser.Access > 1)
    //			c.message = "Insufficient permissions. Only users with an access level of 1 or 0 can promote.";
    //		else if (!v.Get(c.SplitArgs[2]).SetAccess(i))
    //			c.message = "Error occured, aborting operation.";

    //		if (!string.IsNullOrEmpty(c.message))
    //			return c;

    //		c.message = $"User {c.SplitArgs[2]}'s access changed to ({i}).";
    //		return c;
    //	}
    //}

    //public class About : IPlugin {
    //	public Dictionary<string, string> Def => new Dictionary<string, string> {
    //		["about"] = "returns general information about Evealyn IRCBot."
    //	};

    //	public ChannelMessage OnChannelMessage(ChannelMessage c, PassableMutableObject v) {
    //		if (!c.SplitArgs[1].CaseEquals(Def.Keys.First()))
    //			return null;

    //		c.message = v.Info;
    //		return c;
    //	}
    //}

    //public class GetPlugins : IPlugin {
    //	public Dictionary<string, string> Def => new Dictionary<string, string> {
    //		["modules"] = "returns a list of all currently active modules."
    //	};

    //	public ChannelMessage OnChannelMessage(ChannelMessage c, PassableMutableObject v) {
    //		if (!c.SplitArgs[1].CaseEquals(Def.Keys.First()))
    //			return null;

    //		c.message = $"Active modules: {string.Join(", ", v.GetModules())}";
    //		return c;
    //	}
    //}

    ////public class ReloadModules : IPlugin {
    ////	public Dictionary<string, string> Def => new Dictionary<string, string> {
    ////		["reload"] = "(<module>) — reloads specified module."
    ////	};

    ////	public ChannelMessage OnChannelMessage(ChannelMessage c, PassableMutableObject v) {
    ////		if (!c.SplitArgs[1].CaseEquals(Def.Keys.First()))
    ////			return null;

    ////		if (v.CurrentUser.Access > 1) {
    ////			c.message = "Insufficient permissions.";
    ////			return c;
    ////		}

    ////		v.ReloadModules();

    ////		c.ExitType = ExitType.MessageAndExit;
    ////		c.message = "Modules reloaded.";
    ////		return c;
    ////	}
    ////}

    //public class Quit : IPlugin {
    //	public Dictionary<string, string> Def => new Dictionary<string, string> {
    //		["quit"] = "ends program's execution."
    //	};

    //	public ChannelMessage OnChannelMessage(ChannelMessage c, PassableMutableObject v) {
    //		if (!c.SplitArgs[1].CaseEquals(Def.Keys.First()))
    //			return null;

    //		if (v.CurrentUser.Access == 0) Environment.Exit(0);
    //		else c.message = "Insufficient permissions.";

    //		return c;
    //	}
    //}
}