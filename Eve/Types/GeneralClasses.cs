using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Eve.Ref;
using Newtonsoft.Json.Linq;

namespace Eve.Types {
	public class User {
		public int Id { get; set; }
		public int Attempts { get; set; }
		public string Nickname { get; set; }
		public string Realname { get; set; }
		public int Access { get; set; }
		public DateTime Seen { get; set; }

		public List<Message> Messages { get; set; } = new List<Message>();
		
		public static User Current { get; internal set; }

		private static List<User> List { get; } = new List<User>();

		/// <summary>
		///     Checks whether or not a specified user exists in default database
		/// </summary>
		/// <param name="identifier">user identifier to check</param>
		/// <returns>true: user exists; false: user does not exist</returns>
		public static User Get(object identifier) {
			if (identifier is int) return List.FirstOrDefault(a => a.Id == (int)identifier);
			if (identifier is string) return List.FirstOrDefault(a => a.Realname == (string)identifier);

			return null;
		}

		public static List<User> GetAll() {
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
		public static void Create(int access, string nickname, string realname, DateTime seen, bool addToDatabase = false, int id = -1) {
			User user = new User {
				Id = id,
				Access = access,
				Nickname = nickname,
				Realname = realname,
				Seen = seen,
				Attempts = 0
			};

			List.Add(user);

			if (!addToDatabase) return;

			Writer.Log($"Creating database entry for {user.Realname}.", EventLogEntryType.Information);

			user.Id = IrcBot.Database.GetLastDatabaseId() + 1;

			Database.QueryDefaultDatabase(
				$"INSERT INTO users VALUES ({user.Id}, '{user.Nickname}', '{user.Realname}', {user.Access}, '{user.Seen}')");
		}

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

	public class Channel {
		public string Name { get; set; }
		public string Topic { get; set; }
		public List<string> Inhabitants { get; set; }
		public List<IrcMode> Modes { get; set; }

		private static List<Channel> List { get; } = new List<Channel>();

		/// <summary>
		///     ChannelList all channels currently connected to
		/// </summary>
		/// <returns></returns>
		public static List<Channel> GetAll() {
			return List;
		}

		/// <summary>
		///     Checks if a channel exists within propRef.Channels
		/// </summary>
		/// <param name="channel">channel to be checked against list</param>
		/// <returns>true: channel is in list; false: channelname is not in list</returns>
		public static Channel Get(string channel) {
			return List.FirstOrDefault(e => e.Name == channel);
		}

		/// <summary>
		///     Adds channel to list of currently connected channels
		/// </summary>
		/// <param name="channelname">Channel name to be checked against and added</param>
		public static bool Add(string channelname) {
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
		public static bool Remove(string channelname) {
			if (Get(channelname) == null) return false;

			List.RemoveAll(e => e.Name == channelname);
			return true;
		}

		/// <summary>
		///     Adds a user to a channel in list
		/// </summary>
		/// <param name="realname">user to be added</param>
		public bool AddUser(string realname) {
			if (User.Get(realname) == null) return false;

			Inhabitants.Add(realname);
			return true;
		}

		/// <summary>
		///     Remove a user from a channel's user list
		/// </summary>
		/// <param name="realname">user to remove</param>
		public bool RemoveUser(string realname) {
			if (User.Get(realname) == null) return false;

			Inhabitants.Remove(realname);
			return true;
		}
	}

	public class IrcConfig {
		public List<string> IgnoreList { get; set; } = new List<string>();

		public bool Identified { get; set; }

		public string Server { get; set; }
		public string[] Channels { get; set; }
		public string Realname { get; set; }
		public string Nickname { get; set; }
		public string Password { get; set; }
		public string Database { get; set; }

		public int Port { get; set; }

		/// <summary>
		///     Check that the default config file exists, then return an object of it
		/// </summary>
		/// <returns>
		///     <see cref="IrcConfig" />
		/// </returns>
		public static IrcConfig GetDefaultConfig() {
			const string baseConfig =
				"{ \"Nickname\": \"TestyBot\", \"Realname\": \"SemiViral\", \"Password\": \"testypass\", \"Server\": \"irc.foonetic.net\", \"Port\": 6667, \"Channels\": [\"#testgrounds\"],  \"IgnoreList\": [], \"DatabaseLocation\": \"users.sqlite\" }";

			if (!File.Exists("config.json")) {
				using (FileStream stream = new FileStream(@"config.json", FileMode.OpenOrCreate, FileAccess.ReadWrite)) {
					Writer.Log("Configuration file not found, creating.", EventLogEntryType.Information);

					StreamWriter writer = new StreamWriter(stream);
					writer.Write(baseConfig);
					writer.Flush();
				}
			}

			JObject config = JObject.Parse(File.ReadAllText("config.json"));

			return new IrcConfig {
				Nickname = (string)config.SelectToken("Nickname"),
				Realname = (string)config.SelectToken("Realname"),
				Password = (string)config.SelectToken("Password"),
				Port = (int)config.SelectToken("Port"),
				Server = (string)config.SelectToken("Server"),
				Channels = config.SelectToken("Channels")
					.Select(e => e.ToString())
					.ToArray(),
				Database = (string)config.SelectToken("DatabaseLocation")
			};
		}
	}
}