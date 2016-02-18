using System;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using Eve.Types;

namespace Eve {
	public class Database {
		/// <summary>
		///     Initialise connections to database and sets properties
		/// </summary>
		/// <param name="databaseLocation">Database location to be read from/write to</param>
		public Database(string databaseLocation) {
			if (string.IsNullOrEmpty(databaseLocation)) throw new NullReferenceException("Database location cannot be empty.");
			DatabaseLocation = databaseLocation;

			if (!File.Exists(DatabaseLocation))
				CreateDatabase(DatabaseLocation);
			else {
				try {
					using (SQLiteConnection db = new SQLiteConnection($"Data Source={DatabaseLocation};Version=3;")) {
						db.Open();
						CheckUsersTableForEmptyAndFill(db);
						ReadUsersIntoList(db);
						ReadMessagesIntoUsers(db);
					}
				} catch (Exception e) {
					throw new SQLiteException("Unable to connect to database, error: " + e);
				}
			}

			if (User.GetAll() == null) throw new SQLiteException("Failed to read from database.");

			Writer.Log("Loaded database.", EventLogEntryType.Information);
		}

		internal static string DatabaseLocation { get; private set; }

		private static void CreateDatabase(string databaseLocation) {
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

		private static void CheckUsersTableForEmptyAndFill(SQLiteConnection db) {
			using (SQLiteCommand a = new SQLiteCommand("SELECT COUNT(id) FROM users", db)) {
				if (Convert.ToInt32(a.ExecuteScalar()) != 0) return;

				Writer.Log("Inhabitants table in database is empty. Creating initial record.", EventLogEntryType.Information);

				using (
					SQLiteCommand b =
						new SQLiteCommand($"INSERT INTO users VALUES (0, '0', '0', 9, '{DateTime.UtcNow}')",
							db))
					b.ExecuteNonQuery();
			}
		}

		private static void ReadUsersIntoList(SQLiteConnection db) {
			using (SQLiteDataReader userEntry = new SQLiteCommand("SELECT * FROM users", db).ExecuteReader()) {
				while (userEntry.Read()) {
					User.Create((int)userEntry["access"],
						(string)userEntry["nickname"],
						(string)userEntry["realname"],
						DateTime.Parse((string)userEntry["seen"]),
						false,
						(int)userEntry["id"]);
				}
			}
		}

		private static void ReadMessagesIntoUsers(SQLiteConnection db) {
			try {
				using (SQLiteDataReader m = new SQLiteCommand("SELECT * FROM messages", db).ExecuteReader()) {
					while (m.Read()) {
						User.Get(Convert.ToInt32(m["id"]))?.Messages.Add(new Message {
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
		public static string QueryDefaultDatabase(string query) {
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
	}
}