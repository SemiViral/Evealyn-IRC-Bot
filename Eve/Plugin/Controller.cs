#region usings

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

#endregion

namespace Eve.Plugin {
    internal class PluginController : MarshalByRefObject {
        private const string PLUGIN_MASK = "Eve.*.dll";

        public List<PluginInstance> Plugins;

        public PluginController() {
            Initialise();
        }

        public bool IsShuttingDown { get; set; }
        public event EventHandler<ChannelMessage> ChannelMessageCallback;
        public event EventHandler<PluginReturnActionEventArgs> PluginsCallback;

        public void OnChannelMessageCallback(object source, ChannelMessage e) {
            ChannelMessageCallback?.Invoke(this, e);
        }

        private void Initialise() {
            if (Plugins == null)
                Plugins = new List<PluginInstance>();
        }

        /// <summary>
        ///     Loads all plugins
        /// </summary>
        public void LoadPlugins() {
            // array of all filepaths that are found to match the PLUGIN_MASK
            string[] pluginMatchAddresses = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, PLUGIN_MASK, SearchOption.AllDirectories);

            if (pluginMatchAddresses.Length.Equals(0)) {
                Log(IrcLogEntryType.System, "No plugins to load.");
                return;
            }

            foreach (string plugin in pluginMatchAddresses) {
                IPlugin pluginInstance;

                try {
                    AppDomain.CurrentDomain.Load(AssemblyName.GetAssemblyName(plugin));
                    pluginInstance = GetPluginInstance(plugin);
                } catch (ReflectionTypeLoadException ex) {
                    foreach (Exception loaderException in ex.LoaderExceptions)
                        Log(IrcLogEntryType.Error, loaderException.ToString());

                    continue;
                } catch (Exception ex) {
                    Log(IrcLogEntryType.Error, ex.ToString());
                    continue;
                }

                pluginInstance.CallbackEvent += PluginInstanceCallback;

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

                if (autoStart)
                    plugin.Start();
            } catch (Exception ex) {
                Log(IrcLogEntryType.Error, $"Error adding plugin: {ex.Message}");
            }
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
        private void PluginInstanceCallback(object source, PluginReturnActionEventArgs e) {
            PluginsCallback?.Invoke(this, e);
        }

        public void StartPlugins() {
            foreach (PluginInstance pluginInstance in Plugins)
                pluginInstance.Instance.Start();
        }

        public void StopPlugins() {
            foreach (PluginInstance pluginInstance in Plugins)
                pluginInstance.Instance.Stop();
        }

        private void Log(IrcLogEntryType entryType, string message, [CallerMemberName] string memeberName = "", [CallerLineNumber] int lineNumber = 0) {
            PluginsCallback?.Invoke(this, new PluginReturnActionEventArgs(PluginActionType.Log, new LogEntry(entryType, message, memeberName, lineNumber)));
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