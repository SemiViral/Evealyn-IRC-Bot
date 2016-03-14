using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Eve.Plugin {
	internal class PluginWrapper : MarshalByRefObject {
		public static Dictionary<string, string> Commands = new Dictionary<string, string>();
		public PluginHost PluginHost;

		private static void PluginsCallback(object source, PluginEventArgs e) {
			switch (e.MessageType) {
				case PluginEventMessageType.Message:
					if (e.Result is PluginReturnMessage) {
						PluginReturnMessage response = (PluginReturnMessage)e.Result;
						Writer.SendData(response.Protocol, $"{response.Target} {response.Message}");
						break;
					}

					Writer.Log(e.Result.ToString(), EventLogEntryType.Information);
					break;
				case PluginEventMessageType.EventLog:
					break;
				case PluginEventMessageType.Action:
					if (e.EventAction.ActionToTake == PluginActionType.AddCommand) {
						if (!(e.Result is KeyValuePair<string, string>)) break;

						var temp = (KeyValuePair<string, string>)e.Result;
						Commands.Add(temp.Key, temp.Value);
					}
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