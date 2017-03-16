#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Eve.Plugin;

#endregion

namespace Eve.Classes {
    public class User {
        public int Id { get; set; }
        public int Attempts { get; set; }
        public string Nickname { get; set; }
        public string Realname { get; set; }
        public int Access { get; set; }
        public DateTime Seen { get; set; }

        public List<Message> Messages { get; } = new List<Message>();
        public List<Channel> Channels { get; } = new List<Channel>();

        public User(int access, string nickname, string realname, DateTime seen, int id = -1) {
            Access = access;
            Nickname = nickname;
            Realname = realname;
            Seen = seen;
            Id = id;
        }

        /// <summary>
        ///     Updates specified user's `seen` data and sets user to LastSeen
        /// </summary>
        /// <param name="nickname">nickname for user's to be checked against</param>
        public void UpdateUser(string nickname) {
            Seen = DateTime.UtcNow;

            Database.QueryDefaultDatabase($"UPDATE users SET seen='{DateTime.UtcNow}' WHERE realname='{Realname}'");

            if (nickname != Nickname) // checks if nickname has changed
                Database.QueryDefaultDatabase($"UPDATE users SET nickname='{nickname}' WHERE realname='{Realname}'");
        }

        /// <summary>
        ///     Adds a Message object to list
        /// </summary>
        /// <param name="m"><see cref="Message" /> to be added</param>
        public bool AddMessage(Message m) {
            if (
                !string.IsNullOrEmpty(
                    Database.QueryDefaultDatabase(
                        $"INSERT INTO messages VALUES ({Id}, '{m.Sender}', '{m.Contents}', '{m.Date}')"))) return false;
            Messages.Add(m);
            return true;
        }

        /// <summary>
        ///     Set new access level for user
        /// </summary>
        /// <param name="access">new access level</param>
        public bool SetAccess(int access) {
            if (!string.IsNullOrEmpty(
                Database.QueryDefaultDatabase($"UPDATE users SET access={access} WHERE realname='{Realname}'")))
                return false;

            Access = access;
            return true;
        }

        /// <summary>
        ///     Discern whether a user has exceeded command-querying limit
        /// </summary>
        /// <returns>true: user timeout</returns>
        public bool GetTimeout() {
            bool doTimeout = false;

            if (Attempts == 4) {
                // Check if user's last message happened more than 1 minute ago
                if (Seen.AddMinutes(1) < DateTime.UtcNow)
                    Attempts = 0; // if so, reset their attempts to 0
                else doTimeout = true; // if not, timeout is true
            } else if (Access > 1)
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