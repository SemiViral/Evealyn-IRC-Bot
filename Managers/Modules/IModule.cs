using System.Collections.Generic;
using Eve.Managers.Classes;

namespace Eve.Managers.Modules {
	public interface IModule {
		Dictionary<string, string> Def { get; }
		ChannelMessage OnChannelMessage(ChannelMessage c, Variables v);
	}
}