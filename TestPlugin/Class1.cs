using System;
using System.Diagnostics;
using Eve.Plugin;

namespace TestPlugin {
	[Serializable]
	internal class Plugin001 : IPlugin {
		public string Id => Guid.NewGuid().ToString();
		public string Name => "Plugin 001";
		public string Version => "1.0";
		public PluginStatus Status { get; private set; } = PluginStatus.Stopped;
		public bool TerminateRequestRecieved { get; private set; }

		private System.Timers.Timer _counter;
		private int _timerIntreval = 1000;

		public void ProcessEnded() {
			if (!TerminateRequestRecieved) return;

			_counter?.Stop();
			PluginEventAction actionCommand = new PluginEventAction {
				ActionToTake = PluginActionType.Unload
			};
		}

		public bool Start() {
			DoCallback(new PluginEventArgs(PluginEventMessageType.Message, "001 started"));
			RunProcess();
			return true;
		}

		public bool Stop() {
			if (Status == PluginStatus.Running) {
				TerminateRequestRecieved = true;
				DoCallback(new PluginEventArgs(PluginEventMessageType.Message, $"Stop called but process is running from: {Name}"));
			} else {
				_counter?.Stop();
				TerminateRequestRecieved = true;
				DoCallback(new PluginEventArgs(PluginEventMessageType.Message, $"Stop called from: {Name}"));
				Call_Die();
			}

			return true;
		}

		public void LogError(string message, EventLogEntryType logType) {
			EventLogger.LogEvent(message, logType);
		}

		public void Call_Die() {
			Status = PluginStatus.Stopped;
			DoCallback(new PluginEventArgs(PluginEventMessageType.Message,
				$"Calling die, stopping process, sending unload —— from: {Name}"));
			DoCallback(new PluginEventArgs(PluginEventMessageType.Action, null, new PluginEventAction {
				ActionToTake = PluginActionType.Unload
			}));
		}

		public void OnCounterElapsed(object sender, EventArgs e) {
			Status = PluginStatus.Processing;
			DoCallback(new PluginEventArgs(PluginEventMessageType.Message, $"Counter elapsed from: {Name}"));

			if (TerminateRequestRecieved) {
				_counter.Stop();
				DoCallback(new PluginEventArgs(PluginEventMessageType.Message, $"Acting on terminate signal: {Name}"));
				Status = PluginStatus.Stopped;
				Call_Die();
			} else {
				Status = PluginStatus.Running;
			}
		}

		public void RunProcess() {
			Status = PluginStatus.Running;
			if (_counter == null)
				_counter = new System.Timers.Timer(_timerIntreval);
			else {
				_counter.Stop();
				_counter.Enabled = false;
				_counter.Interval = _timerIntreval;
			}

			_counter.Elapsed += OnCounterElapsed;
			_counter.Start();
		}

		public event EventHandler<PluginEventArgs> CallbackEvent;

		public void DoCallback(PluginEventArgs e) {
			CallbackEvent?.Invoke(this, e);
		}
	}
}