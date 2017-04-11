#region usings

using System;
using System.Reflection;
using System.Runtime.CompilerServices;

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
        PluginStatus Status { get; }

        void Start();
        void Stop();
        void Call_Die();
        void Log(IrcLogEntryType logType, string message, [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0);

        event EventHandler<PluginReturnActionEventArgs> CallbackEvent;
    }

    [Serializable]
    public class PluginSimpleReturnMessage {
        public PluginSimpleReturnMessage(string protocol, string target, string args) {
            Protocol = protocol;
            Target = target;
            Args = args;
        }

        public string Protocol { get; set; }
        public string Target { get; set; }
        public string Args { get; set; }
    }

    [Serializable]
    public class PluginReturnActionEventArgs : EventArgs {
        public PluginActionType ActionType;
        public string ExecutingDomain;
        public string MessageId;
        public string PluginId;
        public string PluginName;
        public object Result;

        public PluginReturnActionEventArgs(PluginActionType actionType, object result = null) {
            Result = result;
            ActionType = actionType;
            ExecutingDomain = AppDomain.CurrentDomain.FriendlyName;
            PluginName = Assembly.GetExecutingAssembly().GetName().Name;
        }
    }

    public enum PluginActionType {
        None = 0,
        Log,
        Load,
        Unload,
        RegisterMethod,
        SendMessage,
        RunProcess,
        SignalTerminate
    }

    public enum PluginStatus {
        Stopped = 0,
        Running,
        Processing
    }
}