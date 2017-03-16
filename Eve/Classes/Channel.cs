#region usings

using System.Collections.Generic;
using System.Linq;
using Eve.Plugin;
using Eve.References;

#endregion

namespace Eve.Classes {
    public class Channel {
        public Channel(string name) {
            Name = name;
            Inhabitants = new List<string>();
            Modes = new List<IrcMode>();
        }

        public string Name { get; set; }
        public string Topic { get; set; }
        internal List<string> Inhabitants { get; }
        public List<IrcMode> Modes { get; }

        /// <summary>
        ///     Adds a user to a channel in list
        /// </summary>
        /// <param name="nickname">user to be added to list</param>
        public bool AddUser(string nickname) {
            if (Inhabitants.Contains(nickname)) return false;

            Inhabitants.Add(nickname);
            return true;
        }

        /// <summary>
        ///     Remove a user from a channel's user list
        /// </summary>
        /// <param name="realname">user to remove</param>
        public bool RemoveUser(string realname) {
            return Inhabitants.Remove(Inhabitants.Single(e => e.Equals(realname)));
        }

        public bool RemoveUser(User user) {
            return Inhabitants.RemoveAll(e => e.Equals(user.Realname)) > 0;
        }
    }

    //public class Inhabitant {
    //    public Inhabitant(string nickname) {
    //        Nickname = nickname;
    //    }

    //    public Inhabitant(string nickname, string realname) {
    //        Nickname = nickname;
    //        Realname = realname;
    //    }

    //    public Inhabitant(ChannelMessageEventArgs channelMessage) {
    //        Nickname = channelMessage.Nickname;
    //        Realname = channelMessage.Realname;
    //    }

    //    public IrcMode Mode { get; set; }
    //    public string Nickname { get; set; }
    //    public string Realname { get; }
    //}
}