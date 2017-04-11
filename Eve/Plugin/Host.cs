using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

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

        public event EventHandler<PluginReturnActionEventArgs> PluginCallback;

        public Dictionary<string, string> GetCommands() => Commands;

        /// <summary>
        ///     Intermediary method for activating PluginCallback
        /// </summary>
        private void PluginsCallback(object sender, PluginReturnActionEventArgs e) {
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

        public void RegisterMethod(PluginRegistrarEventArgs pluginRegistrar) {
            if (!PluginEvents.ContainsKey(pluginRegistrar.ProtocolType))
                PluginEvents.Add(pluginRegistrar.ProtocolType, new List<PluginMethodWrapper>());

            // check whether commands exist and add to list
            if (!pluginRegistrar.Definition.Equals(default(KeyValuePair<string, string>)))
                Commands.Add(pluginRegistrar.Definition.Key, pluginRegistrar.Definition.Value);

            PluginEvents[pluginRegistrar.ProtocolType].Add(pluginRegistrar.Method);
        }

        private void Log(IrcLogEntryType entryType, string message, [CallerMemberName] string memeberName = "", [CallerLineNumber] int lineNumber = 0) {
            PluginsCallback(this, new PluginReturnActionEventArgs(PluginActionType.Log, new LogEntry(entryType, message, memeberName, lineNumber)));
        }
    }
}
