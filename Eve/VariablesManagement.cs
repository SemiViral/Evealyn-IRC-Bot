using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Eve.Types;

namespace Eve {
	public class VariablesManagement {
		/// <summary>
		///     Initialise connections to database and sets properties
		/// </summary>
		/// <param name="databaseLocation">Database location to be read from/write to</param>
		public VariablesManagement(string databaseLocation) {
			if (string.IsNullOrEmpty(databaseLocation)) throw new NullReferenceException("Database location cannot be empty.");
			DatabaseLocation = databaseLocation;

			if (!File.Exists(DatabaseLocation))
				CreateDatabase(DatabaseLocation);
			else {
				try {
					using (SQLiteConnection db = new SQLiteConnection($"Data Source={DatabaseLocation};Version=3;")) {
						db.Open();
						CheckUsersTableForEmptyAndFill(db);
						ReadUsers(db);
						ReadMessagesIntoUsers(db);
					}
				} catch (Exception e) {
					throw new SQLiteException("Unable to connect to database, error: " + e);
				}
			}

			if (Users == null) throw new SQLiteException("Failed to read from database.");

			Writer.Log("Loaded database.", EventLogEntryType.Information);
		}

		private void CreateDatabase(string databaseLocation) {
			Writer.Log("Database not found, creating.", EventLogEntryType.Information);

			using (SQLiteConnection db = new SQLiteConnection($"Data Source={databaseLocation};Version=3;")) {
				db.Open();

				using (SQLiteCommand com = new SQLiteCommand(
					"CREATE TABLE users (id int, nickname string, realname string, access int, seen string)", db))
					com.ExecuteNonQuery();

				using (SQLiteCommand com2 =
					new SQLiteCommand("CREATE TABLE messages (id int, sender string, message string, datetime string)", db))
					com2.ExecuteNonQuery();
			}
		}

		private void CheckUsersTableForEmptyAndFill(SQLiteConnection db) {
			using (SQLiteCommand a = new SQLiteCommand("SELECT COUNT(id) FROM users", db)) {
				if (Convert.ToInt32(a.ExecuteScalar()) != 0) return;

				Writer.Log("Users table in database is empty. Creating initial record.", EventLogEntryType.Information);

				using (
					SQLiteCommand b =
						new SQLiteCommand($"INSERT INTO users VALUES (0, '0', '0', 9, '{DateTime.UtcNow}')",
							db))
					b.ExecuteNonQuery();
			}
		}

		private void ReadUsers(SQLiteConnection db) {
			using (SQLiteDataReader d = new SQLiteCommand("SELECT * FROM users", db).ExecuteReader()) {
				while (d.Read()) {
					Users.Add(new User {
						Id = (int)d["id"],
						Nickname = (string)d["nickname"],
						Realname = (string)d["realname"],
						Access = (int)d["access"],
						Seen = DateTime.Parse((string)d["seen"]),
						Attempts = 0
					});
				}
			}
		}

		private void ReadMessagesIntoUsers(SQLiteConnection db) {
			try {
				using (SQLiteDataReader m = new SQLiteCommand("SELECT * FROM messages", db).ExecuteReader()) {
					while (m.Read()) {
						Users.FirstOrDefault(e => e.Id == Convert.ToInt32(m["id"]))?.Messages.Add(new Message {
							Sender = (string)m["sender"],
							Contents = (string)m["message"],
							Date = DateTime.Parse((string)m["datetime"])
						});
					}
				}
			} catch (NullReferenceException) {
				Writer.Log(
					"||| NullReferenceException occured upon loading messages from database. This most likely means a user record was deleted and the ID cannot be referenced from the message entry.",
					EventLogEntryType.Error);
			}
		}

		/// <summary>
		///     Execute a query on the default database
		/// </summary>
		/// <param name="query"></param>
		public string QueryDefaultDatabase(string query) {
			try {
				using (SQLiteConnection db = new SQLiteConnection($"Data Source={DatabaseLocation};Version=3;"))
				using (SQLiteCommand com = new SQLiteCommand(query, db)) {
					db.Open();
					com.ExecuteNonQuery();
				}

				return null;
			} catch (Exception e) {
				Writer.Log(e.Message, EventLogEntryType.Error);
				return e.Message;
			}
		}

		/// <summary>
		///     Returns int value of last ID in default database
		/// </summary>
		/// <returns>
		///     <see cref="int" />
		/// </returns>
		public int GetLastDatabaseId() {
			int id = -1;

			using (SQLiteConnection db = new SQLiteConnection($"Data Source={DatabaseLocation};Version=3;")) {
				db.Open();

				using (SQLiteDataReader r = new SQLiteCommand("SELECT MAX(id) FROM users", db).ExecuteReader()) {
					while (r.Read()) {
						id = Convert.ToInt32(r.GetValue(0));
					}
				}
			}

			return id;
		}

		/// <summary>
		///     Reload all plugins from plugins folder
		/// </summary>
		public void ReloadPlugins() {}

		/// <summary>
		///     Return list of commands or a single command, or null if the command is unmatched
		/// </summary>
		/// <param name="command">Command to be checked and returned, if specified</param>
		/// <returns></returns>
		public List<string> GetCommands(string command = null) {
			return command == null ?
				new List<string>(CommandList.Keys) :
				new List<string> {
					CommandList[command]
				};
		}

		/// <summary>
		///     Checks whether specified comamnd exists
		/// </summary>
		/// <param name="command">comamnd name to be checked</param>
		/// <returns>True: exists; false: does not exist</returns>
		public bool HasCommand(string command) {
			return CommandList.Keys.Contains(command);
		}

		/// <summary>
		///     ChannelList all channels currently connected to
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
				channelname.StartsWith("#")) {
				Channels.Add(new Channel {
					Name = channelname,
					UserList = new List<string>()
				});
			} else return false;

			return true;
		}

		/// <summary>
		///     Adds a user to a channel in list
		/// </summary>
		/// <param name="channelname">name of channel to add to</param>
		/// <param name="realname">user to be added</param>
		public bool AddUserToChannel(string channelname, string realname) {
			if (GetChannel(channelname) == null ||
				GetUser(realname) == null) return false;

			GetChannel(channelname).UserList.Add(realname);
			return true;
		}

		/// <summary>
		///     Removes a channel from list
		/// </summary>
		/// <param name="channelname">name of channel to remove</param>
		public bool RemoveChannel(string channelname) {
			if (GetChannel(channelname) == null) return false;

			Channels.RemoveAll(e => e.Name == channelname);
			return true;
		}

		/// <summary>
		///     RemoveChannel a user from a channel's user list
		/// </summary>
		/// <param name="channelname">name of channel to remove from</param>
		/// <param name="realname">user to remove</param>
		public bool RemoveUserFromChannel(string channelname, string realname) {
			if (GetChannel(channelname) == null ||
				GetUser(realname) == null) return false;

			GetChannel(channelname).UserList.Remove(realname);
			return true;
		}

		/// <summary>
		///     Creates a new user and updates the users & userTimeouts collections
		/// </summary>
		/// <param name="user"><see cref="User" /> object to surmise information from</param>
		public void CreateUser(User user) {
			Writer.Log($"Creating database entry for {user.Realname}.", EventLogEntryType.Information);

			user.Id = GetLastDatabaseId() + 1;

			QueryDefaultDatabase(
				$"INSERT INTO users VALUES ({user.Id}, '{user.Nickname}', '{user.Realname}', {user.Access}, '{user.Seen}')");

			Users.Add(user);
		}

		/// <summary>
		///     Checks whether or not a specified user exists in default database
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

		public string Info
			=>
				$"Evealyn is a utility IRC bot created by SemiViral as a primary learning project for C#. Version {Assembly.GetExecutingAssembly().GetName().Version}"
			;

		public User CurrentUser { get; internal set; } = new User();

		public string DatabaseLocation { get; }

		internal List<User> Users { get; } = new List<User>();
		internal List<Channel> Channels { get; } = new List<Channel>();
		internal List<string> IgnoreList { get; set; } = new List<string>();

		internal Dictionary<string, string> CommandList = new Dictionary<string, string>();

		#endregion
	}
}