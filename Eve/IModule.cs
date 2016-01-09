using System;
using System.Collections.Generic;

namespace Eve {
	public interface IModule {
		Dictionary<string, string> def { get; }
		ChannelMessage OnChannelMessage(ChannelMessage c);
	}

	public class ChannelMessage {
		public List<string> _Args;
		public DateTime Time;

		public string
			Nickname,
			Realname,
			Hostname,
			Type,
			Recipient,
			Args;
	}
}
