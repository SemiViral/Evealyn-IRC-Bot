#region

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
        void LogError(string message, EventLogEntryType logType);
        void OnChannelMessage(object source, ChannelMessageEventArgs e);

        event EventHandler<PluginEventArgs> CallbackEvent;
    }

    [Serializable]
    public class PluginReturnMessage {
        public PluginReturnMessage(string protocol, string target, string message) {
            Protocol = protocol;
            Target = target;
            Message = message;
        }

        public string Protocol { get; set; }
        public string Target { get; set; }
        public string Message { get; set; }
    }

    [Serializable]
    public class PluginEventArgs {
        public PluginEventAction EventAction;
        public string ExecutingDomain;
        public string MessageId;
        public PluginEventMessageType MessageType;
        public string PluginId;
        public string PluginName;
        public object Result;

        public PluginEventArgs(PluginEventMessageType messageType = PluginEventMessageType.Message, object result = null,
            PluginEventAction eventAction = new PluginEventAction()) {
            MessageType = messageType;
            Result = result;
            EventAction = eventAction;
            ExecutingDomain = AppDomain.CurrentDomain.FriendlyName;
            PluginName = Assembly.GetExecutingAssembly().GetName().Name;
        }
    }

    [Serializable]
    public class RegisterEventArgs {
        public Dictionary<string, string> Definitions;

        public RegisterEventArgs(Dictionary<string, string> definitions) {
            Definitions = definitions;
        }
    }

    public enum PluginEventMessageType {
        Message = 0, // informational message
        EventLog, // event that needs to be logged
        Action // action the host application needs to take
    }

    public enum PluginActionType {
        None = 0,
        Load,
        Unload,
        RunProcess,
        TerminateAndUnloadPlugins,
        SignalTerminate,
        UpdatePlugin,
        AddCommand,
        SendMessage
    }

    public class PluginEventActionList {
        public List<PluginEventAction> ActionsToTake;

        public PluginEventActionList() {
            if (ActionsToTake == null) ActionsToTake = new List<PluginEventAction>();
        }
    }

    [Serializable]
    public struct PluginEventAction {
        public PluginActionType ActionToTake;
        public PluginAssemblyType TargetPluginAssemblyType;
    }

    public enum PluginAssemblyType {
        None = 0,
        PrePlugin,
        Plugin
    }

    public enum PluginStatus {
        Stopped = 0,
        Running,
        Processing
    }
}