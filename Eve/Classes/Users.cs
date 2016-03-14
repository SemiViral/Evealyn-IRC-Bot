using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Eve.Classes {
	public class UserOverlord : MarshalByRefObject {
		public Users Current { get; internal set; }

		private List<Users> List { get; } = new List<Users>();

		/// <summary>
		///     Checks whether or not a specified user exists in default database
		/// </summary>
		/// <param name="identifier">user identifier to check</param>
		/// <returns>true: user exists; false: user does not exist</returns>
		public Users Get(object identifier) {
			if (identifier is int) return List.FirstOrDefault(a => a.Id == (int)identifier);
			if (identifier is string) return List.FirstOrDefault(a => a.Realname == (string)identifier);

			return null;
		}

		public List<Users> GetAll() {
			return List;
		}

		/// <summary>
		///     Creates a new user and updates the users & userTimeouts collections
		/// </summary>
		/// <param name="access">access level of user</param>
		/// <param name="nickname">nickname of user</param>
		/// <param name="realname">realname of user</param>
		/// <param name="seen">last time user was seen</param>
		/// <param name="addToDatabase">whether or not to add user to database as well</param>
		/// <param name="id">id of user</param>
		public void Create(int access, string nickname, string realname, DateTime seen, bool addToDatabase = false, int id = -1) {
			Users users = new Users {
				Id = id,
				Access = access,
				Nickname = nickname,
				Realname = realname,
				Seen = seen,
				Attempts = 0
			};

			List.Add(users);

			if (!addToDatabase) return;

			Writer.Log($"Creating database entry for {users.Realname}.", EventLogEntryType.Information);

			users.Id = Program.Bot.Database.GetLastDatabaseId() + 1;

			Database.QueryDefaultDatabase(
				$"INSERT INTO users VALUES ({users.Id}, '{users.Nickname}', '{users.Realname}', {users.Access}, '{users.Seen}')");
		}
	}

	public class Users {
		public int Id { get; set; }
		public int Attempts { get; set; }
		public string Nickname { get; set; }
		public string Realname { get; set; }
		public int Access { get; set; }
		public DateTime Seen { get; set; }

		public List<Message> Messages { get; set; } = new List<Message>();

		/// <summary>
		///     Updates specified user's `seen` data and sets user to Current
		/// </summary>
		/// <param name="nickname">nickname for user's to be checked against</param>
		public void UpdateUser(string nickname) {
			Seen = DateTime.UtcNow;

			Database.QueryDefaultDatabase($"UPDATE users SET seen='{DateTime.UtcNow}' WHERE realname='{Realname}'");

			if (nickname != Nickname)
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