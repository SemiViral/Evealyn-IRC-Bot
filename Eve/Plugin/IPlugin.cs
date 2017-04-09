#region usings

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

#endregion

namespace Eve.Plugin {
    /// <summary>
    ///     Interface for hooking a new plugin into Eve
    /// </summary>
    public interface IPlugin {
        string Name { get; }
        string Author { get; }
        string Version { get; }
        string Id { get; }
        bool TerminateRequestRecieved { get; }
        Dictionary<string, string> Commands { get; }
        PluginStatus Status { get; }

        bool Start();
        bool Stop();
        void Call_Die();
        void ProcessEnded();
        void LogError(string message, IrcLogEntryType logType);
        void OnChannelMessage(object source, ChannelMessage channelMessage);

        event EventHandler<PluginEventArgs> CallbackEvent;
    }

    [Serializable]
    public class PluginReturnMessage {
        public PluginReturnMessage(string protocol, string target, string args) {
            Protocol = protocol;
            Target = target;
            Args = args;
        }

        public string Protocol { get; set; }
        public string Target { get; set; }
        public string Args { get; set; }
    }

    [Serializable]
    public class PluginEventArgs {
        public PluginActionType ActionType;
        public string ExecutingDomain;
        public string MessageId;
        public PluginEventMessageType MessageType;
        public string PluginId;
        public string PluginName;
        public object Result;

        public PluginEventArgs(PluginEventMessageType messageType, object result = null,
            PluginActionType actionType = PluginActionType.None) {
            MessageType = messageType;
            Result = result;
            ActionType = actionType;
            ExecutingDomain = AppDomain.CurrentDomain.FriendlyName;
            PluginName = Assembly.GetExecutingAssembly().GetName().Name;
        }
    }

    [Serializable]
    public class CommandRegistrarEventArgs : EventArgs {
        public CommandRegistrarEventArgs(KeyValuePair<string, string> kvp) {
            Command = kvp;
        }

        public CommandRegistrarEventArgs(string key, string value) {
            Command = new KeyValuePair<string, string>(key, value);
        }

        public KeyValuePair<string, string> Command { get; }
        public string Key => Command.Key;
        public string Value => Command.Value;
    }

    public enum PluginEventMessageType {
        Message = 0, // informational args
        EventLog, // event that needs to be logged
        Action // action the host application needs to take
    }

    public enum PluginActionType {
        None = 0,
        Load,
        Unload,
        RunProcess,
        SignalTerminate,
        UpdatePlugin,
        AddCommand,
        SendMessage
    }

    public enum PluginStatus {
        Stopped = 0,
        Running,
        Processing
    }
}