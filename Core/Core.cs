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
        public string Version => "3.1.1";
        public string Id => Guid.NewGuid().ToString();
        public bool TerminateRequestRecieved { get; private set; }

        public Dictionary<string, string> Commands => new Dictionary<string, string> {
            ["eval"] = "(<expression>) — evaluates given mathematical expression.",
            ["join"] = "(<channel> *<message>) — joins specified channel.",
            ["part"] = "(<channel> *<message>) — parts from specified channel.",
            ["channels"] = "returns a list of connected channels.",
            ["reload"] = "reloads the plugin domain.",
            ["quit"] = "terminates bot execution"
        };

        public PluginStatus Status { get; private set; } = PluginStatus.Stopped;

        public void ProcessEnded() {
            if (!TerminateRequestRecieved) {}
        }

        public void OnChannelMessage(object source, ChannelMessageEventArgs e) {
            Status = PluginStatus.Processing;

            if (!e.MainBot.Inhabitants.Any(x => x.Equals(e.Nickname)) ||
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
                case "part":
                    Part(e);
                    break;
                case "channels":
                    Channels(e);
                    break;
                case "quit":
                    Quit(e);
                    break;
            }

            Status = PluginStatus.Stopped;
        }

        public bool Start() {
            Writer.Log($"{Name} loaded.", IrcLogEntryType.System);
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

        public void LogError(string message, IrcLogEntryType logType) {
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

            if (e.MainBot.Users.SingleOrDefault(x => x.Realname.Equals(e.Realname))?.Access > 1) {
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
                Writer.Log($"Error reloading plugins: {ex}", IrcLogEntryType.Error);
                return;
            }

            message.Args = "Error occured reloading plugins.";
            DoCallback(PluginEventMessageType.Message, message);
        }

        private void Quit(ChannelMessageEventArgs e) {
            DoCallback(PluginEventMessageType.Message, new PluginReturnMessage(Protocols.PRIVMSG, e.Recipient, "Shutting down."));
            DoCallback(PluginEventMessageType.Action, null, PluginActionType.TerminateAndUnloadPlugins);
        }

        private void GetLastId(ChannelMessageEventArgs e) {
            DoCallback(PluginEventMessageType.Message, new PluginReturnMessage(Protocols.PRIVMSG, e.Recipient, e.MainBot.GetLastDatabaseId().ToString()));
        }

        private void Eval(ChannelMessageEventArgs e) {
            PluginReturnMessage message = new PluginReturnMessage(Protocols.PRIVMSG, e.Recipient, string.Empty);

            if (e.SplitArgs.Count < 3) message.Args = "Not enough parameters.";

            if (string.IsNullOrEmpty(message.Args)) {
                Status = PluginStatus.Running;
                string evalArgs = e.SplitArgs.Count > 3 ?
                    e.SplitArgs[2] + e.SplitArgs[3] : e.SplitArgs[2];

                try {
                    message.Args = new Calculator.InlineCalculator().Evaluate(evalArgs).ToString(CultureInfo.CurrentCulture);
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
                message.Args = $"Successfully joined channel: {e.SplitArgs[2]}.";
            }

            DoCallback(PluginEventMessageType.Message, message);
        }

        private void Part(ChannelMessageEventArgs e) {
            PluginReturnMessage message = new PluginReturnMessage(Protocols.PRIVMSG, e.Recipient, string.Empty);

            if (e.MainBot.Users.First(x => x.Nickname.Equals(e.Nickname)).Access > 1)
                message.Args = "Insufficient permissions.";
            else if (e.SplitArgs.Count < 3)
                message.Args = "Insufficient parameters. Type 'eve help part' to view command's help index.";
            else if (!e.SplitArgs[2].StartsWith("#"))
                message.Args = "Channel c._Argsument must be a proper channel name (i.e. starts with '#').";
            else if (e.MainBot.Channels.First(x => x.Name.Equals(e.SplitArgs[2])) == null)
                message.Args = "I'm not in that channel.";

            if (!string.IsNullOrEmpty(message.Args)) {
                DoCallback(PluginEventMessageType.Message, message);
                return;
            }

            string channel = e.SplitArgs[2].ToLower();
            e.MainBot.Channels.RemoveAll(x => x.Name.Equals(e.SplitArgs[2]));
            message.Args = $"Successfully parted channel: {channel}";

            DoCallback(PluginEventMessageType.Message,
                new PluginReturnMessage(Protocols.PART, string.Empty, $"{channel} Channel part invoked by: {e.Nickname}"));
            DoCallback(PluginEventMessageType.Message, message);
        }

        private void Channels(ChannelMessageEventArgs e) {
            Status = PluginStatus.Running;
            DoCallback(PluginEventMessageType.Message,
                new PluginReturnMessage(Protocols.PRIVMSG, e.Recipient,
                    string.Join(", ", e.MainBot.Channels.Select(x => x.Name).ToList())));
        }
    }
}