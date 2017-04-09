#region usings

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Eve.Core.Calculator;
using Eve.Plugin;
using Eve.References;
using Newtonsoft.Json.Linq;

#endregion

namespace Eve.Core {
    public class Core : IPlugin {
        private readonly Regex youtubeRegex = new Regex(@"(?i)http(?:s?)://(?:www\.)?youtu(?:be\.com/watch\?v=|\.be/)(?<ID>[\w\-]+)(&(amp;)?[\w\?=‌​]*)?", RegexOptions.Compiled);

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

        public void OnChannelMessage(object source, ChannelMessage channelMessage) {
            Status = PluginStatus.Processing;

            if (!channelMessage.MainBot.Inhabitants.Any(x => x.Equals(channelMessage.Nickname))) return;

            switch (channelMessage.SplitArgs[0]) {
                case "reload":
                    Reload(channelMessage);
                    break;
                case "eval":
                    Eval(channelMessage);
                    break;
                case "join":
                    Join(channelMessage);
                    break;
                case "part":
                    Part(channelMessage);
                    break;
                case "channels":
                    Channels(channelMessage);
                    break;
                case "quit":
                    Quit(channelMessage);
                    break;
                default:
                    if (!youtubeRegex.IsMatch(channelMessage.Args)) break;

                    YouTubeLinkResponse(channelMessage);
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
                DoCallback(new PluginEventArgs(PluginEventMessageType.Message, $"Stop called but process is running from: {Name}"));
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
            DoCallback(PluginEventMessageType.Message, $"Calling die, stopping process, sending unload —— from: {Name}");
            DoCallback(PluginEventMessageType.Action, null, PluginActionType.Unload);
        }

        public event EventHandler<PluginEventArgs> CallbackEvent;

        public void DoCallback(PluginEventArgs e) {
            CallbackEvent?.Invoke(this, e);
        }

        public void DoCallback(PluginEventMessageType type, object result, PluginActionType actionType = PluginActionType.None) {
            CallbackEvent?.Invoke(this, new PluginEventArgs(type, result, actionType));
        }

        private void Reload(ChannelMessage channelMessage) {
            PluginReturnMessage message = new PluginReturnMessage(Protocols.PRIVMSG, channelMessage.Recipient, string.Empty);

            if (channelMessage.MainBot.Users.SingleOrDefault(x => x.Realname.Equals(channelMessage.Realname))?.Access > 1) {
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

        private void Quit(ChannelMessage channelMessage) {
            DoCallback(PluginEventMessageType.Message, new PluginReturnMessage(Protocols.PRIVMSG, channelMessage.Recipient, "Shutting down."));
            DoCallback(PluginEventMessageType.Action, null, PluginActionType.SignalTerminate);
        }

        private void GetLastId(ChannelMessage channelMessage) {
            DoCallback(PluginEventMessageType.Message, new PluginReturnMessage(Protocols.PRIVMSG, channelMessage.Recipient, channelMessage.MainBot.GetLastDatabaseId().ToString()));
        }

        private void Eval(ChannelMessage channelMessage) {
            PluginReturnMessage message = new PluginReturnMessage(Protocols.PRIVMSG, channelMessage.Recipient, string.Empty);

            if (channelMessage.SplitArgs.Count < 3) message.Args = "Not enough parameters.";

            if (string.IsNullOrEmpty(message.Args)) {
                Status = PluginStatus.Running;
                string evalArgs = channelMessage.SplitArgs.Count > 3 ? channelMessage.SplitArgs[2] + channelMessage.SplitArgs[3] : channelMessage.SplitArgs[2];

                try {
                    message.Args = new InlineCalculator().Evaluate(evalArgs).ToString(CultureInfo.CurrentCulture);
                } catch (Exception ex) {
                    message.Args = ex.Message;
                }
            }

            DoCallback(PluginEventMessageType.Message, message);
        }

        private void Join(ChannelMessage channelMessage) {
            PluginReturnMessage message = new PluginReturnMessage(Protocols.PRIVMSG, channelMessage.Recipient, string.Empty);

            if (channelMessage.MainBot.Users.Single(f => f.Realname.Equals(channelMessage.Realname)).Access > 1)
                message.Args = "Insufficient permissions.";
            else if (channelMessage.SplitArgs.Count < 3)
                message.Args = "Insufficient parameters. Type 'eve help join' to view command's help index.";
            else if (!channelMessage.SplitArgs[2].StartsWith("#"))
                message.Args = "Channel name must start with '#'.";
            else if (channelMessage.MainBot.Channels.Any(f => f.Name.Equals(channelMessage.SplitArgs[2].ToLower())))
                message.Args = "I'm already in that channel.";

            Status = PluginStatus.Running;

            if (string.IsNullOrEmpty(message.Args)) {
                DoCallback(PluginEventMessageType.Message, new PluginReturnMessage(Protocols.JOIN, string.Empty, channelMessage.SplitArgs[2]));
                message.Args = $"Successfully joined channel: {channelMessage.SplitArgs[2]}.";
            }

            DoCallback(PluginEventMessageType.Message, message);
        }

        private void Part(ChannelMessage channelMessage) {
            PluginReturnMessage message = new PluginReturnMessage(Protocols.PRIVMSG, channelMessage.Recipient, string.Empty);

            if (channelMessage.MainBot.Users.First(x => x.Nickname.Equals(channelMessage.Nickname)).Access > 1)
                message.Args = "Insufficient permissions.";
            else if (channelMessage.SplitArgs.Count < 3)
                message.Args = "Insufficient parameters. Type 'eve help part' to view command's help index.";
            else if (!channelMessage.SplitArgs[2].StartsWith("#"))
                message.Args = "Channel c._Argsument must be a proper channel name (i.e. starts with '#').";
            else if (channelMessage.MainBot.Channels.First(x => x.Name.Equals(channelMessage.SplitArgs[2])) == null)
                message.Args = "I'm not in that channel.";

            if (!string.IsNullOrEmpty(message.Args)) {
                DoCallback(PluginEventMessageType.Message, message);
                return;
            }

            string channel = channelMessage.SplitArgs[2].ToLower();
            channelMessage.MainBot.Channels.RemoveAll(x => x.Name.Equals(channelMessage.SplitArgs[2]));
            message.Args = $"Successfully parted channel: {channel}";

            DoCallback(PluginEventMessageType.Message, new PluginReturnMessage(Protocols.PART, string.Empty, $"{channel} Channel part invoked by: {channelMessage.Nickname}"));
        }

        private void Channels(ChannelMessage channelMessage) {
            Status = PluginStatus.Running;
            DoCallback(PluginEventMessageType.Message, new PluginReturnMessage(Protocols.PRIVMSG, channelMessage.Recipient, string.Join(", ", channelMessage.MainBot.Channels.Select(x => x.Name).ToList())));
        }

        private void YouTubeLinkResponse(ChannelMessage channelMessage) {
            Status = PluginStatus.Running;

            int maxDescriptionLength = 100;

            string getResponse = Utility.HttpGet($"https://www.googleapis.com/youtube/v3/videos?part=snippet&id={youtubeRegex.Match(channelMessage.Args).Groups["ID"]}&key={channelMessage.MainBot.YouTubeAPIKey}");

            JToken video = JObject.Parse(getResponse)["items"][0]["snippet"];
            string channel = (string)video["channelTitle"];
            string title = (string)video["title"];
            string description = video["description"].ToString().Split('\n')[0];
            string[] descArray = description.Split(' ');

            if (description.Length > maxDescriptionLength) {
                description = string.Empty;

                for (int i = 0; description.Length < maxDescriptionLength; i++)
                    description += $" {descArray[i]}";

                if (!description.EndsWith(" ")) description.Remove(description.LastIndexOf(' '));

                description += "....";
            }

            DoCallback(PluginEventMessageType.Message, new PluginReturnMessage(Protocols.PRIVMSG, channelMessage.Recipient, $"{title} (by {channel}) — {description}"));
        }
    }
}