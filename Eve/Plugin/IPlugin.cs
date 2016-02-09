using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Eve.Plugin {
	/// <summary>
	///     Interface for hooking a new plugin into Eve
	/// </summary>
	public interface IPlugin {
		string Id { get; }
		string Name { get; }
		string Version { get; }
		bool TerminateRequestRecieved { get; }
		PluginStatus Status { get; }
		Dictionary<string, string> Definition { get; }

		bool Start();
		bool Stop();
		void Call_Die();
		void ProcessEnded();
		void LogError(string message, EventLogEntryType logType);
		void OnChannelMessage(object source, ChannelMessageEventArgs e);

		event EventHandler<PluginEventArgs> CallbackEvent;
	}

	[Serializable]
	public class PluginEventArgs {
		public PluginEventAction EventAction;
		public string ExecutingDomain;
		public string MessageId;
		public PluginEventMessageType MessageType;
		public string PluginId;
		public string PluginName;
		public string ResultMessage;

		public PluginEventArgs(PluginEventMessageType messageType = PluginEventMessageType.Message, string resultMessage = "",
			PluginEventAction eventAction = new PluginEventAction()) {
			MessageType = messageType;
			ResultMessage = resultMessage;
			EventAction = eventAction;
			ExecutingDomain = AppDomain.CurrentDomain.FriendlyName;
			PluginName = Assembly.GetExecutingAssembly().GetName().Name;
		}
	}

	[Serializable]
	public class ChannelMessageEventArgs {
		// Unused regexs
		// private readonly Regex _argMessageRegex = new Regex(@"^:(?<Arg1>[^\s]+)\s(?<Arg2>[^\s]+)\s(?<Arg3>[^\s]+)\s?:?(?<Arg4>.*)", RegexOptions.Compiled);
		//private static readonly Regex PingRegex = new Regex(@"^PING :(?<Message>.+)", RegexOptions.None);

		// Regex for parsing raw messages
		private static readonly Regex MessageRegex =
			new Regex(@"^:(?<Sender>[^\s]+)\s(?<Type>[^\s]+)\s(?<Recipient>[^\s]+)\s?:?(?<Args>.*)", RegexOptions.Compiled);

		private static readonly Regex SenderRegex = new Regex(@"^(?<Nickname>[^\s]+)!(?<Realname>[^\s]+)@(?<Hostname>[^\s]+)",
			RegexOptions.Compiled);

		private static readonly string[] ShortIgnoreList = {
			"nickserv",
			"chanserv"
		};

		public ChannelMessageEventArgs(string rawData) {
			RawMessage = rawData;
			ParseMessage(RawMessage);
		}

		public string RawMessage { get; }

		/// <summary>
		///     Represents whether the realname processed was contained in the specified identifier list (ChanServ, NickServ)
		/// </summary>
		public bool IsRealUser { get; private set; }

		public string Nickname { get; private set; }
		public string Realname { get; private set; }
		public string Hostname { get; private set; }
		public string Recipient { get; private set; }
		public string Type { get; set; }
		public string Args { get; private set; }
		public List<string> SplitArgs { get; private set; } = new List<string>();

		public void ParseMessage(string rawData) {
			if (!MessageRegex.IsMatch(rawData)) return;

			// begin parsing message into sections
			Match mVal = MessageRegex.Match(rawData);
			string mSender = mVal.Groups["Sender"].Value;
			Match sMatch = SenderRegex.Match(mSender);

			// class property setting
			Nickname = mSender;
			Realname = mSender.ToLower();
			Hostname = mSender;
			Type = mVal.Groups["Type"].Value;
			Recipient = mVal.Groups["Recipient"].Value.StartsWith(":")
				? mVal.Groups["Recipient"].Value.Substring(1)
				: mVal.Groups["Recipient"].Value;
			Args = mVal.Groups["Args"].Value;
			SplitArgs = Args?.Trim().Split(new[] {' '}, 4).ToList();
			IsRealUser = false;

			if (!sMatch.Success) return;

			string realname = sMatch.Groups["Realname"].Value;
			Nickname = sMatch.Groups["Nickname"].Value;
			Realname = realname.StartsWith("~") ? realname.Substring(1) : realname;
			Hostname = sMatch.Groups["Hostname"].Value;
			IsRealUser = !ShortIgnoreList.Contains(Realname.ToLower());
		}
	}

	public enum PluginEventMessageType {
		Message = 0, // informational message
		EventLog, // event that needs to be logged
		Action // action the host application needs to take
	}

	public enum PluginActionType {
		None = 0,
		Load,
		Unload,
		RunProcess,
		TerminateAndUnloadPlugins,
		SignalTerminate,
		UpdatePlugin,
		ProcessChannelMessage
	}

	public class PluginEventActionList {
		public List<PluginEventAction> ActionsToTake;

		public PluginEventActionList() {
			if (ActionsToTake == null) ActionsToTake = new List<PluginEventAction>();
		}
	}

	[Serializable]
	public struct PluginEventAction {
		public PluginActionType ActionToTake;
		public PluginAssemblyType TargetPluginAssemblyType;
	}

	public enum PluginAssemblyType {
		None = 0,
		PrePlugin,
		Plugin
	}

	public enum PluginStatus {
		Stopped = 0,
		Running,
		Processing
	}
}