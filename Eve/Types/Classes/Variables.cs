using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;

namespace Eve.Types.Classes {
	public class Variables : IDisposable {
		/// <summary>
		///     Initialise connections to database and fill collections
		/// </summary>
		/// <param name="database">database to be assigned to</param>
		public Variables(string database) {
			if (!File.Exists(database))
				CreateDatabase(database);

			try {
				Db = new SQLiteConnection($"Data Source={database};Version=3;");
				Db.Open();
			} catch (Exception e) {
				throw new SQLiteException("||| Unable to connect to database, error: " + e);
			}

			CheckUsersTableForEmptyAndFill();
			ReadUsers();
			ReadMessagesIntoUsers();

			if (Users != null) return;
			throw new SQLiteException("||| Failed to read from database.");
		}

		public void Dispose() {
			Db?.Close();
		}

		/// <summary>
		///     Creates a database at specified location
		/// </summary>
		/// <param name="database">name and/or path for the database to be made at</param>
		/// <returns>true: database created; false: database not created</returns>
		private void CreateDatabase(string database) {
			if (File.Exists(database))
				throw new IOException("||| Specified database already exists.");

			database = database.EndsWith(".sqlite") ? database : $"{database}.sqlite";

			Db = new SQLiteConnection($"Data Source={database};Version=3;");
			Db.Open();

			using (SQLiteCommand com = new SQLiteCommand(
				"CREATE TABLE users (int id, string nickname, string realname, int access, string seen)", Db))
				com.ExecuteNonQuery();

			using (SQLiteCommand com2 =
				new SQLiteCommand("CREATE TABLE messages (int id, string sender, string message, string datetime)", Db))
				com2.ExecuteNonQuery();
		}

		private void CheckUsersTableForEmptyAndFill() {
			using (SQLiteCommand a = new SQLiteCommand("SELECT COUNT(id) FROM users", Db))
				if (Convert.ToInt32(a.ExecuteScalar()) == 0) {
					Console.WriteLine("||| Users table in database is empty. Creating initial record.");

					using (
						SQLiteCommand b =
							new SQLiteCommand($"INSERT INTO users VALUES (0, '000000000', '000000000', 3, '{DateTime.UtcNow}')",
								Db))
						b.ExecuteNonQuery();
				}
		}

		private void ReadUsers() {
			using (SQLiteDataReader d = new SQLiteCommand("SELECT * FROM users", Db).ExecuteReader())
				while (d.Read())
					Users.Add(new User {
						Id = (int)d["id"],
						Nickname = (string)d["nickname"],
						Realname = (string)d["realname"],
						Access = (int)d["access"],
						Seen = DateTime.Parse((string)d["seen"]),
						Attempts = 0
					});
		}

		private void ReadMessagesIntoUsers() {
			try {
				using (SQLiteDataReader m = new SQLiteCommand("SELECT * FROM messages", Db).ExecuteReader())
					while (m.Read())
						Users.FirstOrDefault(e => e.Id == Convert.ToInt32(m["id"]))?.Messages.Add(new Message {
							Sender = (string)m["sender"],
							Contents = (string)m["message"],
							Date = DateTime.Parse((string)m["datetime"])
						});
			} catch (NullReferenceException) {
				Console.WriteLine(
					"||| NullReferenceException occured upon loading messages from database. This most likely means a user record was deleted and the ID cannot be referenced from the message entry.");
			}
		}

		public User QueryName(string name) {
			return Users.FirstOrDefault(e => e.Realname == name);
		}

		#region Variable initializations

		public SQLiteConnection Db;
		public User CurrentUser = new User();

		public string Info =
			"Evealyn is a utility IRC bot created by SemiViral as a primary learning project for C#. Version 2.0";

		public List<string> IgnoreList = new List<string>();
		public List<User> Users = new List<User>();
		public readonly List<Channel> Channels = new List<Channel>();

		public Dictionary<string, string> Commands = new Dictionary<string, string>();
		public Dictionary<string, Type> Modules;

		#endregion
	}
}
