using System.Collections.Generic;
using System.Linq;
using Eve.Ref;

namespace Eve.Classes {
	public class ChannelOverlord {
		private List<Channel> List { get; } = new List<Channel>();

		/// <summary>
		///     ChannelList all channels currently connected to
		/// </summary>
		/// <returns></returns>
		public List<Channel> GetAll() {
			return List;
		}

		/// <summary>
		///     Checks if a channel exists within propRef.Channels
		/// </summary>
		/// <param name="channel">channel to be checked against list</param>
		/// <returns>true: channel is in list; false: channelname is not in list</returns>
		public Channel Get(string channel) {
			return List.FirstOrDefault(e => e.Name == channel);
		}

		/// <summary>
		///     Adds channel to list of currently connected channels
		/// </summary>
		/// <param name="channelname">Channel name to be checked against and added</param>
		public bool Add(string channelname) {
			if (List.All(e => e.Name != channelname) &&
				channelname.StartsWith("#")) {
				List.Add(new Channel {
					Name = channelname,
					Inhabitants = new List<string>()
				});
			} else return false;

			return true;
		}

		/// <summary>
		///     Removes a channel from list
		/// </summary>
		/// <param name="channelname">name of channel to remove</param>
		public bool Remove(string channelname) {
			if (Get(channelname) == null) return false;

			List.RemoveAll(e => e.Name == channelname);
			return true;
		}
	}

	public class Channel {
		public string Name { get; set; }
		public string Topic { get; set; }
		public List<string> Inhabitants { get; set; }
		public List<IrcMode> Modes { get; set; }

		/// <summary>
		///     Adds a user to a channel in list
		/// </summary>
		/// <param name="realname">user to be added</param>
		public bool AddUser(string realname) {
			if (Program.Bot.Users.Get(realname) == null) return false;

			Inhabitants.Add(realname);
			return true;
		}

		/// <summary>
		///     Remove a user from a channel's user list
		/// </summary>
		/// <param name="realname">user to remove</param>
		public bool RemoveUser(string realname) {
			if (Program.Bot.Users.Get(realname) == null) return false;

			Inhabitants.Remove(realname);
			return true;
		}
	}
}