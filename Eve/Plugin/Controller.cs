#region usings

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Eve.Types;

#endregion

namespace Eve.Plugin {
    internal class PluginController : MarshalByRefObject {
        private const string PLUGIN_MASK = "Eve.*.dll";

        public List<PluginInstance> Plugins;

        public PluginController() {
            Initialise();
        }

        public bool IsShuttingDown { get; set; }
        public event EventHandler<ActionEventArgs> PluginsCallback;

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

            foreach (string plugin in pluginMatchAddresses)
                try {
                    AppDomain.CurrentDomain.Load(AssemblyName.GetAssemblyName(plugin));

                    foreach (IPlugin instance in GetPluginInstances(plugin)) {
                        instance.CallbackEvent += PluginInstanceCallback;
                        AddPlugin(instance, false);
                    }
                } catch (ReflectionTypeLoadException ex) {
                    foreach (Exception loaderException in ex.LoaderExceptions)
                        Log(IrcLogEntryType.Error, loaderException.ToString());
                } catch (Exception ex) {
                    Log(IrcLogEntryType.Error, ex.ToString());
                }
        }

        /// <summary>
        ///     Gets instance of plugin by assembly name
        /// </summary>
        /// <param name="assemblyName">full name of assembly</param>
        /// <returns></returns>
        private static IEnumerable<IPlugin> GetPluginInstances(string assemblyName) {
            IEnumerable<Type> pluginTypeInstances = GetTypeInstances(assemblyName);

            foreach (Type type in pluginTypeInstances)
                yield return (IPlugin)Activator.CreateInstance(type, null, null);
        }

        /// <summary>
        ///     Gets the IPlugin type instance from an assembly name
        /// </summary>
        /// <param name="assemblyName">full name of assembly</param>
        /// <returns></returns>
        private static IEnumerable<Type> GetTypeInstances(string assemblyName) {
            AssemblyInstanceInfo pluginAssembly = new AssemblyInstanceInfo(AppDomain.CurrentDomain.Load(AssemblyName.GetAssemblyName(assemblyName)));

            Type[] assemblyTypes = pluginAssembly.GetTypes();

            return assemblyTypes.Where(type => type.GetInterface("IPlugin") != null);
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
        ///     This method is triggered when the plugin instance invokes its callback event
        /// </summary>
        private void PluginInstanceCallback(object source, ActionEventArgs e) {
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
            PluginsCallback?.Invoke(this, new ActionEventArgs(PluginActionType.Log, new LogEntry(entryType, message, memeberName, lineNumber)));
        }
    }

    public class AssemblyInstanceInfo : MarshalByRefObject {
        public AssemblyInstanceInfo(Assembly baseAssembly) {
            Assembly = baseAssembly;
        }

        public Assembly Assembly { get; }

        public Type[] GetTypes() => Assembly.GetTypes();

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