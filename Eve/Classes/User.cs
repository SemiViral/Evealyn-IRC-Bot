#region usings

using System;
using System.Collections.Generic;

#endregion

namespace Eve.Classes {
    public class User : MarshalByRefObject {
        public User(int access, string nickname, string realname, DateTime seen, int id = -1) {
            Access = access;
            Nickname = nickname;
            Realname = realname;
            Seen = seen;
            Id = id;
        }

        public int Id { get; set; }
        public int Attempts { get; set; }
        public string Nickname { get; set; }
        public string Realname { get; set; }
        public int Access { get; set; }
        public DateTime Seen { get; set; }

        public List<Message> Messages { get; } = new List<Message>();
        public List<Channel> Channels { get; } = new List<Channel>();

        /// <summary>
        ///     Discern whether a user has exceeded command-querying limit
        /// </summary>
        /// <returns>true: user timeout</returns>
        public bool GetTimeout() {
            bool doTimeout = false;

            if (Attempts.Equals(4))
                if (Seen.AddMinutes(1) < DateTime.UtcNow)
                    Attempts = 0; // if so, reset their attempts to 0
                else
                    doTimeout = true; // if not, timeout is true
            else if (Access > 1)
                // if user isn't admin/op, increment their attempts
                Attempts++;

            return doTimeout;
        }
    }

    public class Message {
        public string Sender { get; set; }
        public string Contents { get; set; }
        public DateTime Date { get; set; }
    }
}