using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;

namespace Eve.Types.Classes {
	public class PropertyReference : IDisposable {
		/// <summary>
		///     Initialise connections to database and sets properties
		/// </summary>
		/// <param name="database">database to be assigned to</param>
		public PropertyReference(string database) {
			if (!File.Exists(database))
				CreateDatabase(database);
			else
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
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool dispose) {
			if (!dispose || _disposed) return;

			Db.Dispose();
			_disposed = true;
		}

		private void CreateDatabase(string database) {
			database = database.EndsWith(".sqlite") ? database : $"{database}.sqlite";
			File.Create(database).Close();

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
						Id = (int) d["id"],
						Nickname = (string) d["nickname"],
						Realname = (string) d["realname"],
						Access = (int) d["access"],
						Seen = DateTime.Parse((string) d["seen"]),
						Attempts = 0
					});
		}

		private void ReadMessagesIntoUsers() {
			try {
				using (SQLiteDataReader m = new SQLiteCommand("SELECT * FROM messages", Db).ExecuteReader())
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

		public User QueryName(string name) {
			return Users.FirstOrDefault(e => e.Realname == name);
		}

		#region Property initializations

		private bool _disposed;

		public SQLiteConnection Db { get; private set; }

		public string Info
			=> "Evealyn is a utility IRC bot created by SemiViral as a primary learning project for C#. Version 2.0";

		public User CurrentUser { get; set; } = new User();

		public List<string> IgnoreList { get; set; } = new List<string>();
		public List<User> Users { get; } = new List<User>();
		public List<Channel> Channels { get; private set; } = new List<Channel>();

		public Dictionary<string, string> Commands { get; private set; } = new Dictionary<string, string>();
		public Dictionary<string, Type> Modules { get; set; } = new Dictionary<string, Type>();

		#endregion
	}
}