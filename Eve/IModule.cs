using System;
using System.Collections.Generic;

namespace Eve {
	public interface IModule {
		Dictionary<string, string> Def { get; }
		ChannelMessage OnChannelMessage(ChannelMessage c);
	}

	public class ChannelMessage {
		public List<string> _Args = new List<string>();
		public DateTime Time;

		public int ExitType = Int32.MaxValue;
		public string
			Nickname,
			Realname,
			Hostname,
			Type,
			Recipient,
			Args;
	}
}
