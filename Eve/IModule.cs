﻿using System.Collections.Generic;
using Eve.Types.Classes;

namespace Eve {
	/// <summary>
	///     Interface for hooking a new module into Eve's type-assembly list
	/// </summary>
	public interface IModule {
		Dictionary<string, string> Def { get; }
		ChannelMessage OnChannelMessage(ChannelMessage c, PropertyReference v);
	}
}