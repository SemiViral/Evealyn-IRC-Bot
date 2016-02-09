using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Eve.Plugin;
using Eve.Utility;

namespace Eve.Core.Calc {
	public partial class Calculator : IPlugin {
		public string Id => Guid.NewGuid().ToString();
		public string Name => "Calculator";
		public string Version => "1.1";
		public bool TerminateRequestRecieved { get; private set; }
		public PluginStatus Status { get; private set; } = PluginStatus.Stopped;

		public Dictionary<string, string> Definition => new Dictionary<string, string> {
			["calc"] = "(<expression>) — evaluates given mathematical expression."
		};

		public void ProcessEnded() {
			if (!TerminateRequestRecieved) {}
		}

		public void OnChannelMessage(object source, ChannelMessageEventArgs e) {
			Status = PluginStatus.Processing;

			if (!e.SplitArgs[1].CaseEquals(Definition.Keys.First())) {
				Status = PluginStatus.Stopped;
				return;
			}

			if (e.SplitArgs.Count < 3)
				Writer.Privmsg(e.Recipient, "Not enough parameters.");

			string evalArgs = e.SplitArgs.Count > 3 ?
				e.SplitArgs[2] + e.SplitArgs[3] : e.SplitArgs[2];

			try {
				Status = PluginStatus.Running;
				Writer.Privmsg(e.Recipient, Evaluate(evalArgs).ToString(CultureInfo.CurrentCulture));
			} catch (Exception ex) {
				Writer.Log(ex.ToString(), EventLogEntryType.Error);
			}

			Status = PluginStatus.Stopped;
		}

		public bool Start() {
			DoCallback(new PluginEventArgs(PluginEventMessageType.Message, $"{Name} loaded."));
			return true;
		}

		public bool Stop() {
			if (Status == PluginStatus.Running) {
				TerminateRequestRecieved = true;
				DoCallback(new PluginEventArgs(PluginEventMessageType.Message, $"Stop called but process is running from: {Name}"));
			} else {
				TerminateRequestRecieved = true;
				DoCallback(new PluginEventArgs(PluginEventMessageType.Message, $"Stop called from: {Name}"));
				Call_Die();
			}

			return true;
		}

		public void LogError(string message, EventLogEntryType logType) {
			Writer.Log(message, logType);
		}

		public void Call_Die() {
			Status = PluginStatus.Stopped;
			DoCallback(new PluginEventArgs(PluginEventMessageType.Message,
				$"Calling die, stopping process, sending unload —— from: {Name}"));
			DoCallback(new PluginEventArgs(PluginEventMessageType.Action, null, new PluginEventAction {
				ActionToTake = PluginActionType.Unload
			}));
		}

		public event EventHandler<PluginEventArgs> CallbackEvent;

		public void DoCallback(PluginEventArgs e) {
			CallbackEvent?.Invoke(this, e);
		}
	}
}