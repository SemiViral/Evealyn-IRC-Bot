using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Eve.Types {
	[Serializable]
	public class PassableMutableObject {
		/// <summary>
		///     Initialise connections to DatabaseLocation and sets properties
		/// </summary>
		/// <param name="databaseLocation">DatabaseLocation to be read from/write to</param>
		public PassableMutableObject(string databaseLocation) {
			DatabaseLocation = databaseLocation;

			if (!File.Exists(databaseLocation))
				CreateDatabase(databaseLocation);
			else
				try {
					using (SQLiteConnection db = new SQLiteConnection($"Data Source={databaseLocation};Version=3;")) {
						db.Open();
						CheckUsersTableForEmptyAndFill(db);
						ReadUsers(db);
						ReadMessagesIntoUsers(db);
					}
				} catch (Exception e) {
					throw new SQLiteException("||| Unable to connect to DatabaseLocation, error: " + e);
				}

			if (Users == null) throw new SQLiteException("||| Failed to read from DatabaseLocation.");

			Utils.Output("Loaded DatabaseLocation.");
		}

		private static void CreateDatabase(string database) {
			Utils.Output("DatabaseLocation not found, creating.");

			using (SQLiteConnection db = new SQLiteConnection($"Data Source={database};Version=3;")) {
				db.Open();

				using (SQLiteCommand com = new SQLiteCommand(
					"CREATE TABLE users (id int, nickname string, realname string, access int, seen string)", db))
					com.ExecuteNonQuery();

				using (SQLiteCommand com2 =
					new SQLiteCommand("CREATE TABLE messages (id int, sender string, message string, datetime string)", db))
					com2.ExecuteNonQuery();
			}
		}

		private static void CheckUsersTableForEmptyAndFill(SQLiteConnection db) {
			using (SQLiteCommand a = new SQLiteCommand("SELECT COUNT(id) FROM users", db)) {
				if (Convert.ToInt32(a.ExecuteScalar()) != 0) return;

				Utils.Output("Users table in DatabaseLocation is empty. Creating initial record.");

				using (
					SQLiteCommand b =
						new SQLiteCommand($"INSERT INTO users VALUES (0, '0', '0', 9, '{DateTime.UtcNow}')",
							db))
					b.ExecuteNonQuery();
			}
		}

		private void ReadUsers(SQLiteConnection db) {
			using (SQLiteDataReader d = new SQLiteCommand("SELECT * FROM users", db).ExecuteReader()) {
				while (d.Read())
					Users.Add(new User {
						Id = (int) d["id"],
						Nickname = (string) d["nickname"],
						Realname = (string) d["realname"],
						Access = (int) d["access"],
						Seen = DateTime.Parse((string) d["seen"]),
						Attempts = 0
					});
			}
		}

		private void ReadMessagesIntoUsers(SQLiteConnection db) {
			try {
				using (SQLiteDataReader m = new SQLiteCommand("SELECT * FROM messages", db).ExecuteReader()) {
					while (m.Read())
						Users.FirstOrDefault(e => e.Id == Convert.ToInt32(m["id"]))?.Messages.Add(new Message {
							Sender = (string) m["sender"],
							Contents = (string) m["message"],
							Date = DateTime.Parse((string) m["datetime"])
						});
				}
			} catch (NullReferenceException) {
				Console.WriteLine(
					"||| NullReferenceException occured upon loading messages from DatabaseLocation. This most likely means a user record was deleted and the ID cannot be referenced from the message entry.");
			}
		}

		/// <summary>
		/// Execute a query on the default IrcBot DatabaseLocation
		/// </summary>
		/// <param name="query"></param>
		public static void QueryDefaultDatabase(string query) {
			using (SQLiteConnection db = new SQLiteConnection($"Data Source={DatabaseLocation};Version=3;"))
			using (SQLiteCommand com = new SQLiteCommand(query, db)) {
				db.Open();
				com.ExecuteNonQuery();
			}
		}

		/// <summary>
		/// Returns int value of last ID in DatabaseLocation
		/// </summary>
		/// <returns><see cref="Int32"/></returns>
		public static int GetLastDatabaseId() {
			int id = -1;

			using (SQLiteConnection db = new SQLiteConnection($"Data Source={DatabaseLocation};Version=3;")) {
				db.Open();

				using (SQLiteDataReader r = new SQLiteCommand("SELECT MAX(id) FROM users", db).ExecuteReader())
					while (r.Read())
						id = Convert.ToInt32(r.GetValue(0));
			}

			return id;
		}



		/// <summary>
		/// Get all modules currently loaded
		/// </summary>
		/// <returns>List containing names of modules</returns>
		public List<string> GetModules() {
			return ModuleControl.GetModules();
		}

		/// <summary>
		///     Reload all modules from Modules folder
		/// </summary>
		public void ReloadModules() {}



		/// <summary>
		///     Return list of commands or a single command, or null if the command is unmatched
		/// </summary>
		/// <param name="command">Command to be checked and returned, if specified</param>
		/// <returns></returns>
		public List<string> GetCommands(string command = null) {
			return command == null ?
				new List<string>(CommandList.Keys) :
				new List<string> { CommandList[command] };
		}

		/// <summary>
		/// Checks whether specified comamnd exists
		/// </summary>
		/// <param name="command">comamnd name to be checked</param>
		/// <returns>True: exists; false: does not exist</returns>
		public bool HasCommand(string command) {
			return CommandList.Keys.Contains(command);
		}

		

		/// <summary>
		/// ChannelList all channels currently connected to
		/// </summary>
		/// <returns></returns>
		public List<Channel> ChannelList() {
			return Channels;
		}

		/// <summary>
		///     Checks if a channel exists within propRef.Channels
		/// </summary>
		/// <param name="channel">channel to be checked against list</param>
		/// <returns>true: channel is in list; false: channelname is not in list</returns>
		public Channel GetChannel(string channel) {
			return Channels.FirstOrDefault(e => e.Name == channel);
		}

		/// <summary>
		///     Adds channel to list of currently connected channels
		/// </summary>
		/// <param name="channelname">Channel name to be checked against and added</param>
		public bool AddChannel(string channelname) {
			if (Channels.All(e => e.Name != channelname) &&
				channelname.StartsWith("#"))
				Channels.Add(new Channel {
					Name = channelname,
					UserList = new List<string>()
				});
			else return false;

			return true;
		}

		/// <summary>
		/// Adds a user to a channel in list
		/// </summary>
		/// <param name="channelname">name of channel to add to</param>
		/// <param name="realname">user to be added</param>
		public void AddUserToChannel(string channelname, string realname) {
			if (GetChannel(channelname) == null) return;
			if (GetUser(realname) == null) return;
			GetChannel(channelname).UserList.Add(realname);
		}

		/// <summary>
		/// Removes a channel from list
		/// </summary>
		/// <param name="channelname">name of channel to remove</param>
		public void RemoveChannel(string channelname) {
			if (GetChannel(channelname) == null) return;
			Channels.RemoveAll(e => e.Name == channelname);
		}

		/// <summary>
		/// RemoveChannel a user from a channel's user list
		/// </summary>
		/// <param name="channelname">name of channel to remove from</param>
		/// <param name="realname">user to remove</param>
		public void RemoveUserFromChannel(string channelname, string realname) {
			if (GetChannel(channelname) == null) return;
			if (GetUser(realname) == null) return;
			GetChannel(channelname).UserList.Remove(realname);
		}

		/// <summary>
		///     Creates a new user and updates the users & userTimeouts collections
		/// </summary>
		/// <param name="user"><see cref="User" /> object to surmise information from</param>
		public void CreateUser(User user) {
			Utils.Output($"Creating database entry for {user.Realname}.");

			user.Id = GetLastDatabaseId() + 1;

			QueryDefaultDatabase(
				$"INSERT INTO users VALUES ({user.Id}, '{user.Nickname}', '{user.Realname}', {user.Access}, '{user.Seen}')");

			Users.Add(user);
		}

		/// <summary>
		///     Checks whether or not a specified user exists in DatabaseLocation
		/// </summary>
		/// <param name="realname">name to check</param>
		/// <returns>true: user exists; false: user does not exist</returns>
		public User GetUser(string realname) {
			return Users.FirstOrDefault(e => e.Realname == realname);
		}

		public List<User> GetUsers() {
			return Users;
        }

		#region Property initializations

		public static string DatabaseLocation { get; private set; }
		public string Info
			=>
				$"Evealyn is a utility IRC bot created by SemiViral as a primary learning project for C#. Version {Assembly.GetExecutingAssembly().GetName().Version}"
			;
		public User CurrentUser { get; internal set; } = new User();

		internal ModuleManager ModuleControl { get; set; }

		internal List<User> Users { get; } = new List<User>();
		internal List<Channel> Channels { get; } = new List<Channel>();
		internal List<string> IgnoreList { get; set; } = new List<string>();

		internal Dictionary<string, string> CommandList = new Dictionary<string, string>();

		#endregion
	}
}