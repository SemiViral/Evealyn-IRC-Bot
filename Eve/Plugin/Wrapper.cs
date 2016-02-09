using System;
using System.Diagnostics;

namespace Eve.Plugin {
	internal class PluginWrapper {
		public PluginHost PluginHost;

		private static void PluginsCallback(object source, PluginEventArgs e) {
			switch (e.MessageType) {
				case PluginEventMessageType.Message:
					Writer.Log(e.ResultMessage, EventLogEntryType.Information);
					break;
				case PluginEventMessageType.EventLog:
					break;
				case PluginEventMessageType.Action:
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public void Start() {
			if (PluginHost != null) return;

			PluginHost = new PluginHost();
			PluginHost.PluginCallback += PluginsCallback;
			PluginHost.LoadAllDomains();
			PluginHost.StartAllPlugins();
		}
	}
}