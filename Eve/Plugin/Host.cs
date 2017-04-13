#region usings

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Eve.Types;
using Eve.Types.Irc;

#endregion

namespace Eve.Plugin {
    internal class PluginHost : MarshalByRefObject {
        private const string DOMAIN_NAME_PLUGINS = "DOM_PLUGINS";

        private PluginController pluginController;

        private AppDomain pluginDomain;

        public PluginHost() {
            Initialise();
        }

        private Dictionary<string, string> Commands { get; set; }

        private Dictionary<string, List<PluginMethodWrapper>> PluginEvents { get; set; }

        public event EventHandler<ActionEventArgs> PluginCallback;

        public Dictionary<string, string> GetCommands() => Commands;

        /// <summary>
        ///     Intermediary method for activating PluginCallback
        /// </summary>
        private void PluginsCallback(object sender, ActionEventArgs e) {
            PluginCallback?.Invoke(this, e);
        }

        public void Initialise() {
            if (pluginDomain != null)
                return;

            pluginDomain = AppDomain.CreateDomain(DOMAIN_NAME_PLUGINS);
            PluginEvents = new Dictionary<string, List<PluginMethodWrapper>>();
            Commands = new Dictionary<string, string>();
        }

        public void LoadPluginDomain() {
            Initialise();
            InitialiseController();
        }

        public void UnloadPluginDomain() {
            if (pluginDomain.Equals(null))
                return;

            StopPlugins();

            AppDomain.Unload(pluginDomain);
            pluginDomain = null;
        }

        private void InitialiseController() {
            pluginController = (PluginController)pluginDomain.CreateInstanceAndUnwrap(typeof(PluginController).Assembly.FullName, typeof(PluginController).FullName);

            pluginController.PluginsCallback += PluginsCallback;
            pluginController.LoadPlugins();
        }

        public void StartPlugins() {
            pluginController?.StartPlugins();
        }

        public void StopPlugins() {
            Log(IrcLogEntryType.System, $"<{DOMAIN_NAME_PLUGINS}> UNLOAD ALL RECIEVED — shutting down.");

            if (pluginController.Equals(null))
                return;

            pluginController.IsShuttingDown = true;
            pluginController.StopPlugins();
        }

        public void InvokeMethods(ChannelMessage channelMessage) {
            if (!PluginEvents.ContainsKey(channelMessage.Type))
                return;

            foreach (PluginMethodWrapper pluginRegistrar in PluginEvents[channelMessage.Type])
                pluginRegistrar.Invoke(this, channelMessage);
        }

        public void RegisterMethod(PluginRegistrar pluginRegistrar) {
            if (!PluginEvents.ContainsKey(pluginRegistrar.CommandType))
                PluginEvents.Add(pluginRegistrar.CommandType, new List<PluginMethodWrapper>());

            // check whether commands exist and add to list
            if (!pluginRegistrar.Definition.Equals(default(KeyValuePair<string, string>)))
                if (Commands.ContainsKey(pluginRegistrar.Definition.Key))
                    Log(IrcLogEntryType.Warning, $"'{pluginRegistrar.Definition.Key}' command already exists, skipping entry.");
                else
                    Commands.Add(pluginRegistrar.Definition.Key, pluginRegistrar.Definition.Value);

            PluginEvents[pluginRegistrar.CommandType].Add(pluginRegistrar.Method);
        }

        private void Log(IrcLogEntryType entryType, string message, [CallerMemberName] string memeberName = "", [CallerLineNumber] int lineNumber = 0) {
            PluginsCallback(this, new ActionEventArgs(PluginActionType.Log, new LogEntry(entryType, message, memeberName, lineNumber)));
        }
    }
}