﻿#region usings

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Eve.Core.Calculator;
using Eve.Plugin;
using Eve.Types;
using Eve.Types.Irc;
using Eve.Types.References;
using Newtonsoft.Json.Linq;

#endregion

namespace Eve.Core {
    public class Core : IPlugin {
        private readonly InlineCalculator calculator = new InlineCalculator();
        private readonly Regex youtubeRegex = new Regex(@"(?i)http(?:s?)://(?:www\.)?youtu(?:be\.com/watch\?v=|\.be/)(?<ID>[\w\-]+)(&(amp;)?[\w\?=‌​]*)?", RegexOptions.Compiled);

        public string Name => "Core";
        public string Author => "SemiViral";
        public string Version => "3.1.3";
        public string Id => Guid.NewGuid().ToString();

        public PluginStatus Status { get; private set; } = PluginStatus.Stopped;

        public void Start() {
            calculator.LogEntryEventHandler += Log;

            DoCallback(PluginActionType.RegisterMethod, new PluginRegistrar(Protocols.PRIVMSG, Quit, new KeyValuePair<string, string>("quit", "terminates bot execution")));

            DoCallback(PluginActionType.RegisterMethod, new PluginRegistrar(Protocols.PRIVMSG, Reload, new KeyValuePair<string, string>("reload", "reloads the plugin domain. bot execution")));

            DoCallback(PluginActionType.RegisterMethod, new PluginRegistrar(Protocols.PRIVMSG, Eval, new KeyValuePair<string, string>("eval", "(<expression>) — evaluates given mathematical expression.")));

            DoCallback(PluginActionType.RegisterMethod, new PluginRegistrar(Protocols.PRIVMSG, Join, new KeyValuePair<string, string>("join", "(<channel> *<message>) — joins specified channel.")));

            DoCallback(PluginActionType.RegisterMethod, new PluginRegistrar(Protocols.PRIVMSG, Part, new KeyValuePair<string, string>("part", "(<channel> *<message>) — parts from specified channel.")));

            DoCallback(PluginActionType.RegisterMethod, new PluginRegistrar(Protocols.PRIVMSG, ListChannels, new KeyValuePair<string, string>("channels", "returns a list of connected channels.")));

            DoCallback(PluginActionType.RegisterMethod, new PluginRegistrar(Protocols.PRIVMSG, Define, new KeyValuePair<string, string>("define", "(<word> *<part of speech>) — returns definition for given word.")));

            DoCallback(PluginActionType.RegisterMethod, new PluginRegistrar(Protocols.PRIVMSG, Lookup, new KeyValuePair<string, string>("lookup", "(<term/phrase>) — returns the wikipedia summary of given term or phrase.")));

            DoCallback(PluginActionType.RegisterMethod, new PluginRegistrar(Protocols.PRIVMSG, ListUsers, new KeyValuePair<string, string>("users", "returns a list of stored user realnames.")));

            DoCallback(PluginActionType.RegisterMethod, new PluginRegistrar(Protocols.PRIVMSG, YouTubeLinkResponse));

            Log(IrcLogEntryType.System, $"{Name} loaded.");
        }

        public void Stop() {
            if (Status.Equals(PluginStatus.Running) ||
                Status.Equals(PluginStatus.Processing)) {
                Log(IrcLogEntryType.System, $"Stop called but process is running from: {Name}");
            } else {
                Log(IrcLogEntryType.System, $"Stop called from: {Name}");
                Call_Die();
            }
        }

        public void Log(IrcLogEntryType logType, string message, [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0) {
            DoCallback(PluginActionType.Log, new LogEntry(logType, message, memberName, lineNumber));
        }

        public void Call_Die() {
            Status = PluginStatus.Stopped;
            Log(IrcLogEntryType.System, $"Calling die, stopping process, sending unload —— from: {Name}");
        }

        public event EventHandler<ActionEventArgs> CallbackEvent;

        private void Log(object source, LogEntry logEntry) {
            Log(logEntry.EntryType, logEntry.Message);
        }

        public void DoCallback(ActionEventArgs e) {
            CallbackEvent?.Invoke(this, e);
        }

        /// <summary>
        ///     Calls back to PluginWrapper
        /// </summary>
        /// <param name="actionType"></param>
        /// <param name="result"></param>
        public void DoCallback(PluginActionType actionType, object result = null) {
            CallbackEvent?.Invoke(this, new ActionEventArgs(actionType, result));
        }

        private void Reload(object source, ChannelMessage channelMessage) {
            if (!channelMessage.SplitArgs[1].Equals("eval"))
                return;

            SimpleMessageEventArgs message = new SimpleMessageEventArgs(Protocols.PRIVMSG, channelMessage.Recipient, string.Empty);

            if (channelMessage.MainBotRef.GetUser(channelMessage.Realname).Access > 1) {
                message.Args = "Insufficient permissions.";
                DoCallback(PluginActionType.SendMessage, message);
                return;
            }

            message.Args = "Attempting to reload plugins.";
            DoCallback(PluginActionType.SendMessage, message);

            try {
                DoCallback(PluginActionType.Unload);
                DoCallback(PluginActionType.Load);
            } catch (Exception ex) {
                message.Args = "Error occured reloading plugins.";
                DoCallback(PluginActionType.SendMessage, message);
                Log(IrcLogEntryType.Error, $"Error reloading plugins: {ex}");
                return;
            }

            message.Args = "Sucessfully reloaded plugins.";
            DoCallback(PluginActionType.SendMessage, message);
        }

        private void Quit(object source, ChannelMessage channelMessage) {
            if (!channelMessage.SplitArgs[1].Equals("quit"))
                return;

            DoCallback(PluginActionType.SendMessage, new SimpleMessageEventArgs(Protocols.PRIVMSG, channelMessage.Recipient, "Shutting down."));
            DoCallback(PluginActionType.SignalTerminate);
        }

        private void Eval(object source, ChannelMessage channelMessage) {
            if (!channelMessage.SplitArgs[1].Equals("eval"))
                return;

            Status = PluginStatus.Running;

            SimpleMessageEventArgs message = new SimpleMessageEventArgs(Protocols.PRIVMSG, channelMessage.Recipient, string.Empty);

            if (channelMessage.SplitArgs.Count < 3)
                message.Args = "Not enough parameters.";

            if (string.IsNullOrEmpty(message.Args)) {
                Status = PluginStatus.Running;
                string evalArgs = channelMessage.SplitArgs.Count > 3
                    ? channelMessage.SplitArgs[2] + channelMessage.SplitArgs[3]
                    : channelMessage.SplitArgs[2];

                try {
                    message.Args = calculator.Evaluate(evalArgs).ToString(CultureInfo.CurrentCulture);
                } catch (Exception ex) {
                    message.Args = ex.Message;
                }
            }

            DoCallback(PluginActionType.SendMessage, message);
        }

        private void Join(object source, ChannelMessage channelMessage) {
            if (!channelMessage.SplitArgs[1].Equals("join"))
                return;

            SimpleMessageEventArgs message = new SimpleMessageEventArgs(Protocols.PRIVMSG, channelMessage.Recipient, string.Empty);

            if (channelMessage.MainBotRef.GetUser(channelMessage.Realname).Access > 1)
                message.Args = "Insufficient permissions.";
            else if (channelMessage.SplitArgs.Count < 3)
                message.Args = "Insufficient parameters. Type 'eve help join' to view command's help index.";
            else if (!channelMessage.SplitArgs[2].StartsWith("#"))
                message.Args = "Channel name must start with '#'.";
            else if (channelMessage.MainBotRef.ChannelExists(channelMessage.SplitArgs[2].ToLower()))
                message.Args = "I'm already in that channel.";

            Status = PluginStatus.Running;

            if (string.IsNullOrEmpty(message.Args)) {
                DoCallback(PluginActionType.SendMessage, new SimpleMessageEventArgs(Protocols.JOIN, string.Empty, channelMessage.SplitArgs[2]));
                message.Args = $"Successfully joined channel: {channelMessage.SplitArgs[2]}.";
            }

            DoCallback(PluginActionType.SendMessage, message);
        }

        private void Part(object source, ChannelMessage channelMessage) {
            if (!channelMessage.SplitArgs[1].Equals("part"))
                return;

            SimpleMessageEventArgs message = new SimpleMessageEventArgs(Protocols.PRIVMSG, channelMessage.Recipient, string.Empty);

            if (channelMessage.MainBotRef.GetUser(channelMessage.Nickname).Access > 1)
                message.Args = "Insufficient permissions.";
            else if (channelMessage.SplitArgs.Count < 3)
                message.Args = "Insufficient parameters. Type 'eve help part' to view command's help index.";
            else if (!channelMessage.SplitArgs[2].StartsWith("#"))
                message.Args = "Channel parameter must be a proper name (starts with '#').";
            else if (channelMessage.MainBotRef.ChannelExists(channelMessage.SplitArgs[2]))
                message.Args = "I'm not in that channel.";

            if (!string.IsNullOrEmpty(message.Args)) {
                DoCallback(PluginActionType.SendMessage, message);
                return;
            }

            string channel = channelMessage.SplitArgs[2].ToLower();

            channelMessage.MainBotRef.RemoveChannels(channel);

            message.Args = $"Successfully parted channel: {channel}";

            DoCallback(PluginActionType.SendMessage, message);
            DoCallback(PluginActionType.SendMessage, new SimpleMessageEventArgs(Protocols.PART, string.Empty, $"{channel} Channel part invoked by: {channelMessage.Nickname}"));
        }

        private void ListChannels(object source, ChannelMessage channelMessage) {
            if (!channelMessage.SplitArgs[1].Equals("channels"))
                return;

            Status = PluginStatus.Running;
            DoCallback(PluginActionType.SendMessage, new SimpleMessageEventArgs(Protocols.PRIVMSG, channelMessage.Recipient, string.Join(", ", channelMessage.MainBotRef.GetAllChannels())));
        }

        private void YouTubeLinkResponse(object source, ChannelMessage channelMessage) {
            if (!youtubeRegex.IsMatch(channelMessage.Args))
                return;

            Status = PluginStatus.Running;

            const int maxDescriptionLength = 100;

            string getResponse = Utility.HttpGet($"https://www.googleapis.com/youtube/v3/videos?part=snippet&id={youtubeRegex.Match(channelMessage.Args).Groups["ID"]}&key={channelMessage.MainBotRef.GetApiKey("YouTube")}");

            JToken video = JObject.Parse(getResponse)["items"][0]["snippet"];
            string channel = (string)video["channelTitle"];
            string title = (string)video["title"];
            string description = video["description"].ToString().Split('\n')[0];
            string[] descArray = description.Split(' ');

            if (description.Length > maxDescriptionLength) {
                description = string.Empty;

                for (int i = 0; description.Length < maxDescriptionLength; i++)
                    description += $" {descArray[i]}";

                if (!description.EndsWith(" "))
                    description.Remove(description.LastIndexOf(' '));

                description += "....";
            }

            DoCallback(PluginActionType.SendMessage, new SimpleMessageEventArgs(Protocols.PRIVMSG, channelMessage.Recipient, $"{title} (by {channel}) — {description}"));
        }

        private void Define(object source, ChannelMessage channelMessage) {
            if (!channelMessage.SplitArgs[1].Equals("define"))
                return;

            SimpleMessageEventArgs message = new SimpleMessageEventArgs(Protocols.PRIVMSG, channelMessage.Recipient, string.Empty);

            if (channelMessage.SplitArgs.Count < 3) {
                message.Args = "Insufficient parameters. Type 'eve help define' to view correct usage.";
                DoCallback(PluginActionType.SendMessage, message);
                return;
            }

            string partOfSpeech = channelMessage.SplitArgs.Count > 3
                ? $"&part_of_speech={channelMessage.SplitArgs[3]}"
                : string.Empty;

            JObject entry = JObject.Parse(Utility.HttpGet($"http://api.pearson.com/v2/dictionaries/laad3/entries?headword={channelMessage.SplitArgs[2]}{partOfSpeech}&limit=1"));

            if ((int)entry.SelectToken("count") < 1) {
                message.Args = "Query returned no results.";
                DoCallback(PluginActionType.SendMessage, message);
                return;
            }

            var _out = new Dictionary<string, string> {
                {"word", (string)entry["results"][0]["headword"]},
                {"pos", (string)entry["results"][0]["part_of_speech"]}
            };

            // this 'if' block seems messy and unoptimised.
            // I'll likely change it in the future.
            if (entry["results"][0]["senses"][0]["subsenses"] != null) {
                _out.Add("definition", (string)entry["results"][0]["senses"][0]["subsenses"][0]["definition"]);

                if (entry["results"][0]["senses"][0]["subsenses"][0]["examples"] != null)
                    _out.Add("example", (string)entry["results"][0]["senses"][0]["subsenses"][0]["examples"][0]["text"]);
            } else {
                _out.Add("definition", (string)entry["results"][0]["senses"][0]["definition"]);

                if (entry["results"][0]["senses"][0]["examples"] != null)
                    _out.Add("example", (string)entry["results"][0]["senses"][0]["examples"][0]["text"]);
            }

            string returnMessage = $"{_out["word"]} [{_out["pos"]}] — {_out["definition"]}";

            if (_out.ContainsKey("example"))
                returnMessage += $" (ex. {_out["example"]})";

            message.Args = returnMessage;
            DoCallback(PluginActionType.SendMessage, message);
        }

        private void Lookup(object source, ChannelMessage channelMessage) {
            Status = PluginStatus.Processing;

            if (!channelMessage.SplitArgs[1].Equals("lookup"))
                return;

            SimpleMessageEventArgs message = new SimpleMessageEventArgs(Protocols.PRIVMSG, channelMessage.Recipient, string.Empty);

            if (channelMessage.SplitArgs.Count < 3) {
                message.Args = "Insufficient parameters. Type 'eve help lookup' to view correct usage.";
                DoCallback(PluginActionType.SendMessage, message);
                return;
            }

            string query = string.Join(" ", channelMessage.SplitArgs.Skip(1));
            string response = Utility.HttpGet($"https://en.wikipedia.org/w/api.php?format=json&action=query&prop=extracts&exintro=&explaintext=&titles={query}");

            JToken pages = JObject.Parse(response)["query"]["pages"].Values().First();

            if (string.IsNullOrEmpty((string)pages["extract"])) {
                message.Args = "Query failed to return results. Perhaps try a different term?";
                DoCallback(PluginActionType.SendMessage, message);
                return;
            }

            string fullReplyStr = $"\x02{(string)pages["title"]}\x0F — {Regex.Replace((string)pages["extract"], @"\n\n?|\n", " ")}";

            message.Target = channelMessage.Nickname;

            foreach (string splitMessage in Utility.SplitStr(fullReplyStr, 440)) {
                message.Args = splitMessage;
                DoCallback(PluginActionType.SendMessage, message);
            }
        }

        private void ListUsers(object source, ChannelMessage channelMessage) {
            if (!channelMessage.SplitArgs[1].Equals("users"))
                return;

            Status = PluginStatus.Running;
            DoCallback(PluginActionType.SendMessage, new SimpleMessageEventArgs(Protocols.PRIVMSG, channelMessage.Recipient, string.Join(", ", channelMessage.MainBotRef.GetAllUsernames())));
        }

        private void Set(object source, ChannelMessage channelMessage) {
            Status = PluginStatus.Processing;

            if (!channelMessage.SplitArgs[1].Equals("set"))
                return;

            SimpleMessageEventArgs message = new SimpleMessageEventArgs(Protocols.PRIVMSG, channelMessage.Recipient, string.Empty);

            if (channelMessage.SplitArgs.Count < 5) {
                message.Args = "Insufficient parameters. Type 'eve help lookup' to view correct usage.";
                DoCallback(PluginActionType.SendMessage, message);
                return;
            }
            
            if (channelMessage.MainBotRef.GetUser(channelMessage.Nickname).Access > 0)
                message.Args = "Insufficient permissions.";

            //channelMessage.MainBotRef.GetUser()
        }
    }
}