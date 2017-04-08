#region usings

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

#endregion

namespace Eve.Plugin {
    internal class PluginHost : MarshalByRefObject {
        private const string DOMAIN_NAME_COMMAND = "DOM_COMMAND";
        private const string DOMAIN_NAME_PLUGINS = "DOM_PLUGINS";

        private AppDomain domainPlugins;
        private AppDomain domainPrePlugins;

        private PluginController pluginController;

        public PluginHost() {
            Initialise();
        }

        public event EventHandler<PluginEventArgs> PluginCallback;

        /// <summary>
        ///     This event is fires into the plugins themselves
        /// </summary>
        public event EventHandler<ChannelMessageEventArgs> ChannelMessageCallback;

        /// <summary>
        ///     Intermediary method for activating PluginCallback
        /// </summary>
        /// <param name="e"></param>
        private void OnCallback(PluginEventArgs e) {
            PluginCallback?.Invoke(this, e);
        }

        public void TriggerChannelMessageCallback(object source, ChannelMessageEventArgs e) {
            try {
                ChannelMessageCallback?.Invoke(this, e);
            } catch (Exception f) {
                Writer.Log(f.ToString(), IrcLogEntryType.Error);
            }
        }

        public void Initialise() {
            if (domainPrePlugins == null) domainPrePlugins = AppDomain.CreateDomain(DOMAIN_NAME_COMMAND);
            if (domainPlugins == null) domainPlugins = AppDomain.CreateDomain(DOMAIN_NAME_PLUGINS);
        }

        public void LoadPluginDomain() {
            Initialise();
            pluginController =
                (PluginController)
                domainPlugins.CreateInstanceAndUnwrap(typeof(PluginController).Assembly.FullName,
                    typeof(PluginController).FullName);
            pluginController.Callback += PluginCallback;
            pluginController.LoadPlugins();
            ChannelMessageCallback += pluginController.OnChannelMessageCallback;
        }

        public void UnloadPluginDomain() {
            if (domainPlugins.Equals(null)) return;

            AppDomain.Unload(domainPlugins);
            domainPlugins = null;
        }

        public void StartPlugins() {
            pluginController?.StartPlugins();
        }

        public void StopPlugins() {
            if (pluginController.Equals(null)) return;

            pluginController.IsShuttingDown = true;
            pluginController.StopPlugins();
        }
    }

    internal class PluginController : MarshalByRefObject {
        private const string PRE_PLUGIN_MASK = "Pre.*.dll";
        private const string PLUGIN_MASK = "Eve.*.dll";

        public bool CanUnload;

        public List<PluginInstance> Plugins;

        public PluginController() {
            Initialise();
        }

        public bool IsShuttingDown { get; set; }
        public event EventHandler<ChannelMessageEventArgs> ChannelMessageCallback;
        public event EventHandler<PluginEventArgs> Callback;

        public void OnChannelMessageCallback(object source, ChannelMessageEventArgs e) {
            ChannelMessageCallback?.Invoke(this, e);
        }

        public List<IPlugin> Load(string assemblyName, EventHandler<PluginEventArgs> proxyLoaderRaiseCallbackEvent) {
            Assembly pluginAssembly = AppDomain.CurrentDomain.Load(AssemblyName.GetAssemblyName(assemblyName));
            var instances = new List<IPlugin>();

            foreach (
                IPlugin instance in
                pluginAssembly.GetTypes()
                    .Where(a => a.GetInterface("IPlugin") != null)
                    .Select(type => (IPlugin)Activator.CreateInstance(type, null, null))) {
                instance.CallbackEvent += proxyLoaderRaiseCallbackEvent;
                ChannelMessageCallback += instance.OnChannelMessage;

                foreach (KeyValuePair<string, string> kvp in instance.Commands)
                    Callback?.Invoke(this,
                        new PluginEventArgs(PluginEventMessageType.Action, kvp, PluginActionType.AddCommand));
            }

            return instances;
        }

        private void Initialise() {
            if (Plugins == null) Plugins = new List<PluginInstance>();
        }

        public void LoadPlugins() {
            try {
                string[] fileList = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, PLUGIN_MASK,
                    SearchOption.AllDirectories);

                if (fileList.Length.Equals(0)) {
                    Writer.Log("No plugins to load.", IrcLogEntryType.System);
                    return;
                }

                foreach (string plugin in fileList) {
                    AppDomain.CurrentDomain.Load(AssemblyName.GetAssemblyName(plugin));
                    AddPlugin(Load(plugin, ProxyLoader_RaiseCallbackEvent), false);
                }
            } catch (ReflectionTypeLoadException ex) {
                foreach (Exception loaderException in ex.LoaderExceptions)
                    Writer.Log($"{loaderException.Message}\n{loaderException.StackTrace}", IrcLogEntryType.Error);
            } catch (Exception ex) {
                if (ex is ArgumentException ||
                    ex is FormatException) {
                    Writer.Log($"Error loading json file.", IrcLogEntryType.Error);
                    return;
                }

                Writer.Log($"Error loading plugin: {ex.Message}\n{ex.StackTrace}", IrcLogEntryType.Error);
            }
        }

        public void AddPlugin(List<IPlugin> plugin, bool autoStart) {
            try {
                foreach (IPlugin instance in plugin) {
                    Plugins.Add(new PluginInstance {
                        Instance = instance,
                        Status = PluginStatus.Stopped
                    });

                    instance.Start();
                }
            } catch (Exception ex) {
                Writer.Log($"Error adding plugin: {ex.Message}", IrcLogEntryType.Error);
            }
        }

        /// <summary>
        ///     Triggers callback
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        public void ProxyLoader_RaiseCallbackEvent(object source, PluginEventArgs e) {
            OnCallback(e);
        }

        /// <summary>
        ///     Is called when ProxyLoader_RaiseCallbackEvent() is triggered,
        ///     sends `Callback's to PluginWrapper for deliberation
        /// </summary>
        /// <param name="e"></param>
        private void OnCallback(PluginEventArgs e) {
            if (e.MessageType.Equals(PluginEventMessageType.Action))
                switch (e.ActionType) {
                    case PluginActionType.None:
                        break;
                    case PluginActionType.Load:
                        break;
                    case PluginActionType.Unload:
                        if (Callback == null) break;

                        e.MessageType = PluginEventMessageType.Message;
                        e.Result = "Unload recieved from plugin.";
                        Callback(this, e);

                        if (!IsShuttingDown) break;

                        bool canTerminate = true;

                        foreach (PluginStatus status in Plugins.Select(plgn => plgn.Instance.Status)) {
                            e.MessageType = PluginEventMessageType.Message;
                            e.Result = $"Unload —— checking for stopped: {status}";
                            Callback?.Invoke(this, e);

                            if (status.Equals(PluginStatus.Stopped)) continue;
                            canTerminate = false;
                            break;
                        }

                        if (canTerminate) {
                            CanUnload = true;

                            e.MessageType = PluginEventMessageType.Action;
                            e.ActionType = PluginActionType.UpdatePlugin;
                            Callback?.Invoke(this, e);
                        }

                        e.MessageType = PluginEventMessageType.Message;
                        e.Result = $"Can terminate: {canTerminate}";
                        Callback?.Invoke(this, e);
                        break;
                    case PluginActionType.RunProcess:
                        break;
                    case PluginActionType.TerminateAndUnloadPlugins:
                        Writer.Log("UNLOAD ALL RECIEVED — shutting down.", IrcLogEntryType.System);
                        IsShuttingDown = true;
                        
                        if (Callback == null) break;

                        e.MessageType = PluginEventMessageType.Action;
                        e.ActionType = PluginActionType.TerminateAndUnloadPlugins;
                        Callback?.Invoke(this, e);
                        break;
                    case PluginActionType.SignalTerminate:
                        break;
                    case PluginActionType.UpdatePlugin:
                        break;
                    case PluginActionType.AddCommand:
                        break;
                    case PluginActionType.SendMessage:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            else Callback?.Invoke(this, e);
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

        public override object InitializeLifetimeService() {
            return null;
        }
    }
}