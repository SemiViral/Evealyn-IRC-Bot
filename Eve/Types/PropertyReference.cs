using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Eve.Types {
	public class PropertyReference {
		/// <summary>
		///     Initialise connections to database and sets properties
		/// </summary>
		/// <param name="database">database to be read from/write to</param>
		public PropertyReference(string database) {
			if (!File.Exists(database))
				CreateDatabase(database);
			else
				try {
					using (SQLiteConnection db = new SQLiteConnection($"Data Source={database};Version=3;")) {
						db.Open();
						CheckUsersTableForEmptyAndFill(db);
						ReadUsers(db);
						ReadMessagesIntoUsers(db);
					}
				} catch (Exception e) {
					throw new SQLiteException("||| Unable to connect to database, error: " + e);
				}

			if (Users == null) throw new SQLiteException("||| Failed to read from database.");
		}

		private static void CreateDatabase(string database) {
			Console.WriteLine("||| Database not found, creating.");

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

				Console.WriteLine("||| Users table in database is empty. Creating initial record.");

				using (
					SQLiteCommand b =
						new SQLiteCommand($"INSERT INTO users VALUES (0, '0', '0', 9, '{DateTime.UtcNow}')",
							db))
					b.ExecuteNonQuery();
			}
		}

		private void ReadUsers(SQLiteConnection db) {
			using (SQLiteDataReader d = new SQLiteCommand("SELECT * FROM users", db).ExecuteReader())
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

		private void ReadMessagesIntoUsers(SQLiteConnection db) {
			try {
				using (SQLiteDataReader m = new SQLiteCommand("SELECT * FROM messages", db).ExecuteReader())
					while (m.Read())
						Users.FirstOrDefault(e => e.Id == Convert.ToInt32(m["id"]))?.Messages.Add(new Message {
							Sender = (string) m["sender"],
							Contents = (string) m["message"],
							Date = DateTime.Parse((string) m["datetime"])
						});
			} catch (NullReferenceException) {
				Console.WriteLine(
					"||| NullReferenceException occured upon loading messages from database. This most likely means a user record was deleted and the ID cannot be referenced from the message entry.");
			}
		}

		/// <summary>
		/// Query internal list of users
		/// </summary>
		/// <param name="name"></param>
		/// <returns><see cref="User"/> object from result, or null if query returned none</returns>
		public User QueryName(string name) {
			return Users.FirstOrDefault(e => e.Realname == name);
		}

		/// <summary>
		/// Reload all modules from Modules folder
		/// </summary>
		public void ReloadModules() {}

		/// <summary>
		/// Return list of commands or a single command, or null if the command is unmatched
		/// </summary>
		/// <param name="command">Command to be checked and returned, if specified</param>
		/// <returns></returns>
		public string GetCommands(string command = null) {
			return command == null ? string.Join(", ", CommandList.Keys) : CommandList[command];
		}

		public bool HasCommand(string command) {
			return CommandList.Keys.Contains(command);
		}

		#region Property initializations
		
		public string Info
			=> $"Evealyn is a utility IRC bot created by SemiViral as a primary learning project for C#. Version {Assembly.GetExecutingAssembly().GetName().Version}";

		public User CurrentUser { get; internal set; } = new User();

		//internal List<IModule> Modules { get; } = ModuleManager.LoadModules();
		internal List<User> Users { get; } = new List<User>();
		internal List<Channel> Channels { get; set; } = new List<Channel>();
		internal List<string> IgnoreList { get; set; } = new List<string>();

		internal Dictionary<string, string> CommandList { get; } = new Dictionary<string, string>();

		#endregion
	}
}