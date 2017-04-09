#region usings

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

#endregion

namespace Eve.Plugin {
    internal class PluginHost : MarshalByRefObject {
        private const string DOMAIN_NAME_PLUGINS = "DOM_PLUGINS";

        private PluginController pluginController;

        private AppDomain pluginDomain;

        public PluginHost() {
            Initialise();
        }

        public event EventHandler<PluginEventArgs> PluginCallback;

        /// <summary>
        ///     This event is fires into the plugins themselves
        /// </summary>
        public event EventHandler<ChannelMessage> ChannelMessageCallbackEvent;

        /// <summary>
        ///     Intermediary method for activating PluginCallback
        /// </summary>
        private void PluginsCallback(object sender, PluginEventArgs e) {
            PluginCallback?.Invoke(this, e);
        }

        public void ChannelMessageCallback(object source, ChannelMessage channelMessage) {
            try {
                ChannelMessageCallbackEvent?.Invoke(this, channelMessage);
            } catch (Exception ex) {
                Writer.Log(ex.ToString(), IrcLogEntryType.Error);
            }
        }

        public void Initialise() {
            if (pluginDomain == null) pluginDomain = AppDomain.CreateDomain(DOMAIN_NAME_PLUGINS);
        }

        public void LoadPluginDomain() {
            Initialise();
            InitialiseController();

            ChannelMessageCallbackEvent += pluginController.OnChannelMessageCallback;
        }

        public void UnloadPluginDomain() {
            if (pluginDomain.Equals(null)) return;

            StopPlugins();

            AppDomain.Unload(pluginDomain);
            pluginDomain = null;
        }

        private void InitialiseController() {
            pluginController = (PluginController)pluginDomain.CreateInstanceAndUnwrap(typeof(PluginController).Assembly.FullName, typeof(PluginController).FullName);

            pluginController.Callback += PluginsCallback;
            pluginController.LoadPlugins();
        }

        public void StartPlugins() {
            pluginController?.StartPlugins();
        }

        public void StopPlugins() {
            Writer.Log($"<{DOMAIN_NAME_PLUGINS}> UNLOAD ALL RECIEVED — shutting down.", IrcLogEntryType.System);

            if (pluginController.Equals(null)) return;

            pluginController.IsShuttingDown = true;
            pluginController.StopPlugins();
        }
    }

    internal class PluginController : MarshalByRefObject {
        private const string PLUGIN_MASK = "Eve.*.dll";

        public List<PluginInstance> Plugins;

        public PluginController() {
            Initialise();
        }

        public bool IsShuttingDown { get; set; }
        public event EventHandler<ChannelMessage> ChannelMessageCallback;
        public event EventHandler<PluginEventArgs> Callback;

        public void OnChannelMessageCallback(object source, ChannelMessage e) {
            ChannelMessageCallback?.Invoke(this, e);
        }

        private void Initialise() {
            if (Plugins == null) Plugins = new List<PluginInstance>();
        }

        /// <summary>
        ///     Loads all plugins
        /// </summary>
        public void LoadPlugins() {
            string[] pluginMatchAddresses = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, PLUGIN_MASK, SearchOption.AllDirectories);

            if (pluginMatchAddresses.Length.Equals(0)) {
                Writer.Log("No plugins to load.", IrcLogEntryType.System);
                return;
            }

            foreach (string plugin in pluginMatchAddresses) {
                IPlugin pluginInstance;

                try {
                    AppDomain.CurrentDomain.Load(AssemblyName.GetAssemblyName(plugin));
                    pluginInstance = GetPluginInstance(plugin);
                } catch (Exception ex) {
                    Writer.Log(ex.ToString(), IrcLogEntryType.Error);
                    continue;
                }

                pluginInstance.CallbackEvent += PluginInstanceCallback;
                ChannelMessageCallback += pluginInstance.OnChannelMessage;

                LoadCommands(pluginInstance);

                AddPlugin(pluginInstance, false);
            }
        }

        /// <summary>
        ///     Adds IPlugin instance to internal list
        /// </summary>
        /// <param name="plugin">plugin instance</param>
        /// <param name="autoStart">start plugin immediately</param>
        public void AddPlugin(IPlugin plugin, bool autoStart) {
            try {
                Plugins.Add(new PluginInstance(plugin, PluginStatus.Stopped));

                if (autoStart) plugin.Start();
            } catch (Exception ex) {
                Writer.Log($"Error adding plugin: {ex.Message}", IrcLogEntryType.Error);
            }
        }

        /// <summary>
        ///     Loads command into internal list from plugin instance
        /// </summary>
        /// <param name="pluginInstance"></param>
        private void LoadCommands(IPlugin pluginInstance) {
            // this adds the commands from the plugin to the master list in IrcBot
            foreach (KeyValuePair<string, string> kvp in pluginInstance.Commands)
                Callback?.Invoke(this, new PluginEventArgs(PluginEventMessageType.Action, kvp, PluginActionType.AddCommand));
        }

        /// <summary>
        ///     Gets instance of plugin by assembly name
        /// </summary>
        /// <param name="assemblyName">full name of assembly</param>
        /// <returns></returns>
        private static IPlugin GetPluginInstance(string assemblyName) {
            Type pluginTypeInstance = GetTypeInstance(assemblyName);

            if (pluginTypeInstance == null)
                throw new TypeLoadException("Type loader was unable to load any types from assembly file.");

            IPlugin pluginInstance = (IPlugin)Activator.CreateInstance(pluginTypeInstance, null, null);

            return pluginInstance;
        }

        /// <summary>
        ///     Gets the IPlugin type instance from an assembly name
        /// </summary>
        /// <param name="assemblyName">full name of assembly</param>
        /// <returns></returns>
        private static Type GetTypeInstance(string assemblyName) {
            Assembly pluginAssembly = AppDomain.CurrentDomain.Load(AssemblyName.GetAssemblyName(assemblyName));

            Type[] assemblyTypes = pluginAssembly.GetTypes();

            return assemblyTypes.FirstOrDefault(x => x.GetInterface("IPlugin") != null);
        }

        /// <summary>
        ///     This method is triggered when the plugin instance invokes its callback event
        /// </summary>
        private void PluginInstanceCallback(object source, PluginEventArgs e) {
            Callback?.Invoke(this, e);
        }

        public void StartPlugins() {
            foreach (PluginInstance pluginInstance in Plugins) pluginInstance.Instance.Start();
        }

        public void StopPlugins() {
            foreach (PluginInstance pluginInstance in Plugins) pluginInstance.Instance.Stop();
        }
    }

    public class AssemblyInstanceInfo : MarshalByRefObject {
        public Assembly Assembly;
        public object ObjectInstance;

        public override object InitializeLifetimeService() {
            return null;
        }
    }

    public class PluginInstance : MarshalByRefObject {
        public IPlugin Instance;
        public PluginStatus Status;

        public PluginInstance(IPlugin instance, PluginStatus status) {
            Instance = instance;
            Status = status;
        }

        public override object InitializeLifetimeService() {
            return null;
        }
    }
}