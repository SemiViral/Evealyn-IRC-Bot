using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Net;
using Eve.Types.Classes;
using Eve.Types.Irc;
using Newtonsoft.Json.Linq;

namespace Eve {
	public class Utils {
		public static string HttpGet(string url) {
			HttpWebRequest request = (HttpWebRequest) WebRequest.Create(url);
			request.Method = "GET";

			using (HttpWebResponse httpr = (HttpWebResponse) request.GetResponse())
				return new StreamReader(httpr.GetResponseStream()).ReadToEnd();
		}

		public static IEnumerable<string> SplitStr(string str, int maxLength) {
			for (int i = 0; i < str.Length; i += maxLength)
				yield return str.Substring(i, Math.Min(maxLength, str.Length - i));
		}

		public static bool GetUserTimeout(string who, Variables v) {
			bool doTimeout = false;

			if (v.QueryName(who) == null)
				return false;

			if (v.CurrentUser.Attempts == 4)
				// Check if user's last message happened more than 1 minute ago
				if (v.QueryName(who).Seen.AddMinutes(1) < DateTime.UtcNow)
					v.CurrentUser.Attempts = 0; // if so, reset their attempts to 0
				else doTimeout = true; // if not, timeout is true
			else if (v.QueryName(who).Access > 1)
				// if user isn't admin/op, increment their attempts
				v.CurrentUser.Attempts++;

			return doTimeout;
		}

		/// <summary>
		///     Checks if a channel exists within V.Channels
		/// </summary>
		/// <param name="channel">channel to be checked against list</param>
		/// <param name="v"><see cref="Variables"/> objec to be checked against</param>
		/// <returns>true: channel is in list; false: channel is not in list</returns>
		public static bool CheckChannelExists(string channel, Variables v) {
			return v.Channels.Any(e => e.Name == channel);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="channel">channel for user to be added to</param>
		/// <param name="realname">user to be added</param>
		/// <param name="v"><see cref="Variables"/> object to be manipulated</param>
		public static void AddUserToChannel(string channel, string realname, Variables v) {
			if (!CheckChannelExists(channel, v)) return;

			v.Channels.FirstOrDefault(e => e.Name == channel)?.UserList.Add(realname);
		}

		public static void RemoveUserFromChannel(string channel, string realname, Variables v) {
			if (!CheckChannelExists(channel, v)) return;

			Channel firstOrDefault = v.Channels.FirstOrDefault(e => e.Name == channel);
			if (firstOrDefault != null &&
				!firstOrDefault.UserList.Contains(realname)) {
				Console.WriteLine($"||| '{realname}' does not exist in that channel.");
				return;
			}

			v.Channels.FirstOrDefault(e => e.Name == channel)?.UserList.Remove(realname);
		}

		/// <summary>
		///     Send raw data to server
		/// </summary>
		/// <param name="streamWriter"></param>
		/// <param name="cmd">command operation; i.e. PRIVMSG, JOIN, or PART</param>
		/// <param name="param">plain arguments to send</param>
		public static void SendData(StreamWriter streamWriter, string cmd, string param) {
			if (param == null) {
				streamWriter.WriteLine(cmd);
				streamWriter.Flush();
				Console.WriteLine(cmd);
			} else {
				streamWriter.WriteLine($"{cmd} {param}");
				streamWriter.Flush();

				if (cmd.Equals(IrcProtocol.Ping)
					||
					cmd.Equals(IrcProtocol.Pong))
					return;

				Console.WriteLine($"{cmd} {param}");
			}
		}

		/// <summary>
		///     Checks whether or not a specified user exists in database
		/// </summary>
		/// <param name="v"><see cref="Variables" /> object to be checked against</param>
		/// <param name="realname">name to check</param>
		/// <returns>true: user exists; false: user does not exist</returns>
		public static bool CheckUserExists(Variables v, string realname) {
			return v.QueryName(realname) != null;
		}

		/// <summary>
		///     Updates specified user's `seen` data
		/// </summary>
		/// <param name="v">
		///     <see cref="Variables" /> object to be manipulated</param>
		/// <param name="c">ChannelMessage for information to be surmised</param>
		public static void UpdateCurrentUserAndInfo(Variables v, ChannelMessage c) {
			v.Users.First(e => e.Realname == c.Realname).Seen = c.Time;

			using (SQLiteCommand com = new SQLiteCommand($"UPDATE users SET seen='{c.Time}' WHERE realname='{c.Realname}'", v.Db)
				)
				com.ExecuteNonQuery();

			if (v.CurrentUser == null ||
				v.CurrentUser.Nickname != c.Nickname)
				return;

			using (
				SQLiteCommand com = new SQLiteCommand($"UPDATE users SET nickname='{c.Nickname}' WHERE realname='{c.Realname}'",
					v.Db))
				com.ExecuteNonQuery();

			v.CurrentUser = v.Users.FirstOrDefault(e => e.Realname == c.Realname);
		}

		/// <summary>
		///     Creates a new user and updates the users & userTimeouts collections
		/// </summary>
		/// <param name="v"><see cref="Variables" /> object to be manipulated</param>
		/// <param name="u"><see cref="User" /> object to surmise information from</param>
		public static void CreateUserAndUpdateCollections(Variables v, User u) {
			Console.WriteLine($"||| Creating database entry for {u.Realname}.");

			int id = -1;

			// create data adapter to obtain all id's from users table, for setting new id
			using (SQLiteDataReader x = new SQLiteCommand("SELECT MAX(id) FROM users", v.Db).ExecuteReader())
				while (x.Read())
					id = Convert.ToInt32(x.GetValue(0)) + 1;

			using (
				SQLiteCommand com =
					new SQLiteCommand($"INSERT INTO users VALUES ({id}, '{u.Nickname}', '{u.Realname}', {u.Access}, '{u.Seen}')", v.Db)
				)
				com.ExecuteNonQuery();

			v.Users.Add(u);
		}

		/// <summary>
		///     Adds channel to list of currently connected channels
		/// </summary>
		/// <param name="v"></param>
		/// <param name="channel">Channel name to be checked against and added</param>
		public static void CheckValidChannelAndAdd(Variables v, string channel) {
			if (v.Channels.All(e => e.Name != channel) &&
				channel.StartsWith("#"))
				v.Channels.Add(new Channel {
					Name = channel,
					UserList = new List<string>()
				});
		}

		/// <summary>
		///     Check that the default config file exists, then return an object of it
		/// </summary>
		/// <returns>
		///     <see cref="IrcConfig" />
		/// </returns>
		public static IrcConfig CheckConfigExistsAndReturn() {
			const string baseConfig =
				"{ \"Nickname\": \"Eve\", \"Realname\": \"SemiViral\", \"Password\": \"evepass\", \"Server\": \"irc.foonetic.net\", \"Port\": 6667, \"Channels\": [\"#testgrounds\"],  \"IgnoreList\": [], \"Database\": \"users.sqlite\" }";

			if (!File.Exists("config.json"))
				using (FileStream stream = new FileStream(@"config.json", FileMode.OpenOrCreate, FileAccess.ReadWrite)) {
					Console.WriteLine("||| Configuration file not found, creating.");

					using (StreamWriter writer = new StreamWriter(stream)) {
						writer.Write(baseConfig);
						writer.Flush();
					}
				}

			JObject config = JObject.Parse(File.ReadAllText("config.json"));

			return new IrcConfig {
				Nickname = (string) config.SelectToken("Nickname"),
				Realname = (string) config.SelectToken("Realname"),
				Password = (string) config.SelectToken("Password"),
				Port = (int) config.SelectToken("Port"),
				Server = (string) config.SelectToken("Server"),
				Channels = config.SelectToken("Channels")
					.Select(e => e.ToString())
					.ToArray(),
				Database = (string) config.SelectToken("Database"),
				Identified = false,
				Joined = false
			};
		}
	}

	public static class Extentions {
		/// <summary>
		///     Compares the object to a string with default ignorance of casing
		/// </summary>
		/// <param name="query">string to compare</param>
		/// <param name="ignoreCase">whether or not to ignore case</param>
		/// <returns>true: strings equal; false: strings unequal</returns>
		public static bool CaseEquals(this string obj, string query, bool ignoreCase = true) {
			return obj.Equals(query, ignoreCase ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture);
		}
	}
}