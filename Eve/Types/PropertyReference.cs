using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Eve.Types {
	public class PropertyReference  {
		/// <summary>
		///     Initialise connections to database and sets properties
		/// </summary>
		/// <param name="database">database to be read from/write to</param>
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

			if (Users == null) throw new SQLiteException("||| Failed to read from database.");
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
		public void ReloadModules() {
			Modules.Clear();
			Modules = ModuleManager.LoadModules();
		}

		/// <summary>
		/// Return list of commands or a single command, or null if the command is unmatched
		/// </summary>
		/// <param name="command">Command to be checked and returned, if specified</param>
		/// <returns></returns>
		public string GetCommands(string command = null) {
				return (command == null) ? string.Join(", ", Modules.Select(e => e.Accessor)) : Modules.First(e => e.Accessor == command)?.Descriptor;
		}

		#region Property initializations

		private bool _disposed;

		public SQLiteConnection Db { get; private set; }

		public string Info
			=> $"Evealyn is a utility IRC bot created by SemiViral as a primary learning project for C#. Version {Assembly.GetExecutingAssembly().GetName().Version}";

		public User CurrentUser { get; set; } = new User();

		public List<Module> Modules { get; private set; } = ModuleManager.LoadModules();
		public List<User> Users { get; } = new List<User>();
		public List<Channel> Channels { get; set; } = new List<Channel>();
		public List<string> IgnoreList { get; set; } = new List<string>();

		#endregion
	}
}