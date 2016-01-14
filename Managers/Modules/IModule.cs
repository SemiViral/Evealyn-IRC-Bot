using System;
using System.Collections.Generic;
using Eve.Enums;
using Eve.Managers.Classes;

namespace Eve.Managers.Modules {
	public interface IModule {
		Dictionary<string, string> Def { get; }
		ChannelMessage OnChannelMessage(ChannelMessage c, Variables v);
	}

	public class ChannelMessage {
		public List<string> _Args = new List<string>();
		public DateTime Time;

		public string
			Nickname,
			Realname,
			Hostname,
			Type,
			Recipient,
			Args;

		public List<string> MultiMessage = new List<string>(); 
		public string
			Target,
			Message;

		public ExitType ExitType;

		/// <summary>
		/// Resets this ChannelMessage's output variables
		/// </summary>
		/// <param name="newTarget">new string for this.Target to equate to</param>
		public void Reset(string newTarget = null) {
			MultiMessage = new List<string>();
			ExitType = ExitType.DoNotExit;
			Target = Message = string.Empty;

			Target = newTarget ?? newTarget;

		}
	}
}

namespace Eve.Enums {
	public enum ExitType {
		DoNotExit,
		MessageAndExit,
		Exit
	}
}