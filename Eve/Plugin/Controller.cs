using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Eve.Plugin {
	internal class PluginHost : MarshalByRefObject {
		private const string DOMAIN_NAME_COMMAND = "DOM_COMMAND";
		private const string DOMAIN_NAME_PLUGINS = "DOM_PLUGINS";

		private AppDomain _domainPlugins;
		private AppDomain _domainPrePlugins;

		private PluginController _pluginController;
		private PluginController _prePluginController;

		public bool HostIsTerminating = false;

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
			ChannelMessageCallback?.Invoke(this, e);
		}

		public void Initialise() {
			if (_domainPrePlugins == null) _domainPrePlugins = AppDomain.CreateDomain(DOMAIN_NAME_COMMAND);
			if (_domainPlugins == null) _domainPlugins = AppDomain.CreateDomain(DOMAIN_NAME_PLUGINS);
		}

		public void LoadAllDomains() {
			Initialise();

			//LoadDomain(PluginAssemblyType.PrePlugin);
			LoadDomain(PluginAssemblyType.Plugin);
		}

		public void UnloadAllDomains() {
			//UnloadDomain(PluginAssemblyType.PrePlugin);
			UnloadDomain(PluginAssemblyType.Plugin);
		}

		public void LoadDomain(PluginAssemblyType controllerToLoad) {
			Initialise();

			switch (controllerToLoad) {
				case PluginAssemblyType.PrePlugin:
					_prePluginController =
						(PluginController)
							_domainPrePlugins.CreateInstanceAndUnwrap(typeof(PluginController).Assembly.FullName,
								typeof(PluginController).FullName);
					_prePluginController.Callback += PluginCallback;
					_prePluginController.LoadPlugins(PluginAssemblyType.PrePlugin);
					return;
				case PluginAssemblyType.Plugin:
					_pluginController =
						(PluginController)
							_domainPlugins.CreateInstanceAndUnwrap(typeof(PluginController).Assembly.FullName,
								typeof(PluginController).FullName);
					_pluginController.Callback += PluginCallback;
					_pluginController.LoadPlugins(PluginAssemblyType.Plugin);
					ChannelMessageCallback += _pluginController.OnChannelMessageCallback;
					break;
				case PluginAssemblyType.None:
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(controllerToLoad), controllerToLoad, null);
			}
		}

		public void UnloadDomain(PluginAssemblyType typeToUnload) {
			Initialise();

			switch (typeToUnload) {
				case PluginAssemblyType.None:
					break;
				case PluginAssemblyType.PrePlugin:
					if (_domainPrePlugins == null) break;

					AppDomain.Unload(_domainPrePlugins);
					_domainPrePlugins = null;
					break;
				case PluginAssemblyType.Plugin:
					if (_domainPlugins == null) break;

					AppDomain.Unload(_domainPlugins);
					_domainPlugins = null;
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(typeToUnload), typeToUnload, null);
			}
		}

		public void StartAllPlugins() {
			StartPlugins(PluginAssemblyType.PrePlugin);
			StartPlugins(PluginAssemblyType.Plugin);
		}

		public void StartPlugins(PluginAssemblyType typeToStart) {
			switch (typeToStart) {
				case PluginAssemblyType.None:
					break;
				case PluginAssemblyType.PrePlugin:
					_prePluginController?.StartPluginType(typeToStart);
					break;
				case PluginAssemblyType.Plugin:
					_pluginController?.StartPluginType(typeToStart);
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(typeToStart), typeToStart, null);
			}
		}

		private void StopAllPlugins() { /* todo make this do something */
		}

		public void StopPlugins(PluginAssemblyType typeToStop) {
			switch (typeToStop) {
				case PluginAssemblyType.None:
					break;
				case PluginAssemblyType.PrePlugin: {
					if (_prePluginController == null) return;

					_prePluginController.IsShuttingDown = true;
					_prePluginController.StopPluginType(typeToStop);
					return;
				}
				case PluginAssemblyType.Plugin: {
					if (_pluginController == null) return;

					_pluginController.IsShuttingDown = true;
					_pluginController.StopPluginType(typeToStop);
					return;
				}
				default:
					throw new ArgumentOutOfRangeException(nameof(typeToStop), typeToStop, null);
			}
		}

		private bool AllDomainPluginsStopped() {
			return _prePluginController.CanUnload & _pluginController.CanUnload;
		}

		/// <summary>
		///     Raises self object callback to be hooked
		///     padd through callback messages recieved
		/// </summary>
		/// <param name="source"></param>
		/// <param name="e"></param>
		public void PluginsCallback(object source, PluginEventArgs e) {
			switch (e.EventAction.ActionToTake) {
				case PluginActionType.None:
					break;
				case PluginActionType.Load:
					break;
				case PluginActionType.Unload:
					break;
				case PluginActionType.RunProcess:
					break;
				case PluginActionType.TerminateAndUnloadPlugins:
					break;
				case PluginActionType.SignalTerminate:
					StopAllPlugins();
					break;
				case PluginActionType.UpdatePlugin:
					if (AllDomainPluginsStopped()) OnCallback(e);
					break;
				case PluginActionType.AddCommand:
					break;
				default:
					OnCallback(e);
					break;
			}
		}
	}

	internal class PluginController : MarshalByRefObject {
		private const string DOMAIN_COMMAND = "COMMAND";
		private const string DOMAIN_PLUGIN = "PLUGIN";

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

				foreach (KeyValuePair<string, string> kvp in instance.Commands) {
					Callback?.Invoke(this, new PluginEventArgs(PluginEventMessageType.Action, kvp, new PluginEventAction {
						ActionToTake = PluginActionType.AddCommand
					}));
				}

				instances.Add(instance);
			}

			return instances;
		}

		private void Initialise() {
			if (Plugins == null) Plugins = new List<PluginInstance>();
		}

		public void LoadPlugins(PluginAssemblyType type) {
			try {
				string[] fileList = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, PLUGIN_MASK, SearchOption.AllDirectories);

				if (fileList.Length == 0) {
					Writer.Log("No plugins to load.", EventLogEntryType.Information);
					return;
				}

				foreach (string plugin in fileList) {
					AppDomain.CurrentDomain.Load(AssemblyName.GetAssemblyName(plugin));
					AddPlugin(Load(plugin, ProxyLoader_RaiseCallbackEvent), PluginAssemblyType.Plugin, false);
				}
			} catch (ReflectionTypeLoadException ex) {
				foreach (Exception loaderException in ex.LoaderExceptions) {
					Writer.Log($"{loaderException.Message}\n{loaderException.StackTrace}", EventLogEntryType.Error);
				}
			} catch (Exception ex) {
				if (ex is ArgumentException ||
					ex is FormatException) {
					Writer.Log($"Error loading json file.", EventLogEntryType.Error);
					return;
				}

				Writer.Log($"Error loading plugin: {ex.Message}\n{ex.StackTrace}", EventLogEntryType.Error);
			}
		}

		public void AddPlugin(List<IPlugin> plugin, PluginAssemblyType pluginType, bool autoStart) {
			try {
				foreach (IPlugin instance in plugin) {
					Plugins.Add(new PluginInstance {
						Instance = instance,
						PluginType = pluginType,
						Status = PluginStatus.Stopped
					});
				}
			} catch (Exception ex) {
				Writer.Log($"Error adding plugin: {ex.Message}", EventLogEntryType.Error);
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
			if (e.MessageType == PluginEventMessageType.Action) {
				switch (e.EventAction.ActionToTake) {
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
							Callback(this, e);

							if (status == PluginStatus.Stopped) continue;
							canTerminate = false;
							break;
						}

						if (canTerminate) {
							CanUnload = true;

							e.MessageType = PluginEventMessageType.Action;
							e.EventAction.ActionToTake = PluginActionType.UpdatePlugin;
							Callback(this, e);
						}

						e.MessageType = PluginEventMessageType.Message;
						e.Result = $"Can terminate: {canTerminate}";
						Callback(this, e);
						break;
					case PluginActionType.RunProcess:
						break;
					case PluginActionType.TerminateAndUnloadPlugins:
						Writer.Log("UNLOAD ALL RECIEVED — shutting down plugins.", EventLogEntryType.Information);
						IsShuttingDown = true;

						// todo tell all plugins to stop
						if (Callback == null) break;

						e.MessageType = PluginEventMessageType.Message;
						e.Result = "UNLOAD ALL RECIEVED — shutting down.";
						Callback(this, e);

						e.MessageType = PluginEventMessageType.Action;
						e.EventAction.ActionToTake = PluginActionType.SignalTerminate;
						Callback(this, e);
						break;
					case PluginActionType.SignalTerminate:
						break;
					case PluginActionType.UpdatePlugin:
						break;
					case PluginActionType.AddCommand:
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			} else Callback?.Invoke(this, e);
		}

		public void StartPluginType(PluginAssemblyType startType) {
			for (int i = Plugins.Count - 1; i > -1; i--) {
				if (Plugins[i].PluginType == startType) Plugins[i].Instance.Start();
			}
		}

		public void StopPluginType(PluginAssemblyType stopType) {
			for (int i = Plugins.Count - 1; i > -1; i--) {
				if (Plugins[i].PluginType == stopType) Plugins[i].Instance.Stop();
			}
		}

		public void RemovePluginFromList(PluginAssemblyType unloadType) {
			for (int i = Plugins.Count - 1; i > -1; i--) {
				if (Plugins[i].PluginType == unloadType) Plugins.RemoveAt(i);
			}
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
		public PluginAssemblyType PluginType;
		public PluginStatus Status;

		public override object InitializeLifetimeService() {
			return null;
		}
	}
}