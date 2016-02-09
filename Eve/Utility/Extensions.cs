using System;
using System.Collections.Generic;
using System.Linq;
using Eve.Types;

namespace Eve.Utility {
	/* STRING */

	public static partial class Extensions {
		/// <summary>
		///     Compares the object to a string with default ignorance of casing
		/// </summary>
		/// <param name="obj">inherent object</param>
		/// <param name="query">string to compare</param>
		/// <param name="ignoreCase">whether or not to ignore case</param>
		/// <returns>true: strings equal; false: strings unequal</returns>
		public static bool CaseEquals(this string obj, string query, bool ignoreCase = true) {
			return obj.Equals(query, ignoreCase ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture);
		}
	}

	/* USER */

	public static partial class Extensions {
		/// <summary>
		///     Updates specified user's `seen` data and sets user to CurrentUser
		/// </summary>
		/// <param name="user">user to be updated</param>
		/// <param name="nickname">nickname for user's to be checked against</param>
		public static void UpdateUser(this User user, string nickname) {
			user.Seen = DateTime.UtcNow;

			IrcBot.VarManagement.QueryDefaultDatabase($"UPDATE users SET seen='{DateTime.UtcNow}' WHERE realname='{user.Realname}'");

			if (nickname != user.Nickname)
				IrcBot.VarManagement.QueryDefaultDatabase($"UPDATE users SET nickname='{user.Nickname}' WHERE realname='{user.Realname}'");
		}

		/// <summary>
		///     Adds a Message object to list
		/// </summary>
		/// <param name="user">user to add message to</param>
		/// <param name="m"><see cref="Message" /> to be added</param>
		public static bool AddMessage(this User user, Message m) {
			if (
				!string.IsNullOrEmpty(
					IrcBot.VarManagement.QueryDefaultDatabase(
						$"INSERT INTO messages VALUES ({user.Id}, '{m.Sender}', '{m.Contents}', '{m.Date}')"))) return false;
			user.Messages.Add(m);
			return true;
		}

		/// <summary>
		///     Set new access level for user
		/// </summary>
		/// <param name="user">user object to mutate on</param>
		/// <param name="access">new access level</param>
		public static bool SetAccess(this User user, int access) {
			if (!string.IsNullOrEmpty(
				IrcBot.VarManagement.QueryDefaultDatabase($"UPDATE users SET access={access} WHERE realname='{user.Realname}'")))
				return false;

			user.Access = access;
			return true;
		}

		/// <summary>
		///     Discern whether a user has exceeded command-querying limit
		/// </summary>
		/// <param name="user">user to check</param>
		/// <returns></returns>
		public static bool GetTimeout(this User user) {
			bool doTimeout = false;

			if (user.Attempts == 4) {
				// Check if user's last message happened more than 1 minute ago
				if (user.Seen.AddMinutes(1) < DateTime.UtcNow)
					user.Attempts = 0; // if so, reset their attempts to 0
				else doTimeout = true; // if not, timeout is true
			} else if (user.Access > 1)
				// if user isn't admin/op, increment their attempts
				user.Attempts++;

			return doTimeout;
		}
	}

	/* DICTIONARY */

	public static partial class Extensions {
		public static Dictionary<TKey, TValue> AddFrom<TKey, TValue>(this Dictionary<TKey, TValue> dict,
			Dictionary<TKey, TValue> postpendDictionary) {
			if (dict == null ||
				postpendDictionary == null) throw new NullReferenceException("Dictionary object must not be null.");
			if (dict.GetType() != postpendDictionary.GetType())
				throw new ArgumentException("Target dictionary type does not match adder dictionary type");
			foreach (TKey s in dict.Keys.Where(postpendDictionary.ContainsKey)) {
				throw new ArgumentException($"Dictionary already contains key `{s}' from target dictionary.");
			}
			return dict.Concat(postpendDictionary).ToDictionary(e => e.Key, e => e.Value);
		}
	}
}