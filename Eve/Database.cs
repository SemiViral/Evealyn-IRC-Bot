#region usings

using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using Eve.Classes;

#endregion

namespace Eve {
    public class Database {
        /// <summary>
        ///     Initialise connections to database and sets properties
        /// </summary>
        /// <param name="databaseLocation">MainDatabase location to be read from/write to</param>
        public Database(string databaseLocation) {
            Location = databaseLocation;

            if (!File.Exists(Location)) CreateDatabase();
            else CheckUsersTableForEmptyAndFill();

            Writer.Log("Loaded database.", IrcLogEntryType.System);
        }

        internal static string Location { get; private set; }
        internal static bool Connected { get; private set; }

        internal static void InitialiseUsersIntoList(List<User> users) {
            CheckUsersTableForEmptyAndFill();
            ReadUsersIntoList(users);
            ReadMessagesIntoUsers(users);

            Writer.Log("Users successfully loaded from database.", IrcLogEntryType.System);
        }

        private static void CreateDatabase() {
            Writer.Log("MainDatabase not found, creating.", IrcLogEntryType.System);

            using (SQLiteConnection db = new SQLiteConnection($"Data Source={Location};Version=3;")) {
                db.Open();
                using (SQLiteCommand com = new SQLiteCommand("CREATE TABLE users (id int, nickname string, realname string, access int, seen string)", db)) {
                    com.ExecuteNonQuery();
                }

                using (SQLiteCommand com2 = new SQLiteCommand("CREATE TABLE messages (id int, sender string, message string, datetime string)", db)) {
                    com2.ExecuteNonQuery();
                }
            }

            Connected = true;
        }

        private static void CheckUsersTableForEmptyAndFill() {
            using (SQLiteConnection db = new SQLiteConnection($"Data Source={Location};Version=3;")) {
                db.Open();

                using (SQLiteCommand a = new SQLiteCommand("SELECT COUNT(id) FROM users", db)) {
                    if (Convert.ToInt32(a.ExecuteScalar()) != 0) return;

                    Writer.Log("Inhabitants table in database is empty. Creating initial record.", IrcLogEntryType.System);

                    using (SQLiteCommand b = new SQLiteCommand($"INSERT INTO users VALUES (0, '0', '0', 9, '{DateTime.UtcNow}')", db)) {
                        b.ExecuteNonQuery();
                    }
                }
            }
        }

        private static void ReadUsersIntoList(ICollection<User> users) {
            using (SQLiteConnection db = new SQLiteConnection($"Data Source={Location};Version=3;")) {
                db.Open();

                using (SQLiteDataReader userEntry = new SQLiteCommand("SELECT * FROM users", db).ExecuteReader()) {
                    while (userEntry.Read())
                        users.Add(new User((int)userEntry["access"], (string)userEntry["nickname"], (string)userEntry["realname"], DateTime.Parse((string)userEntry["seen"]), (int)userEntry["id"]));
                }
            }

            Writer.Log("User list loaded.", IrcLogEntryType.System);
        }

        private static void ReadMessagesIntoUsers(IReadOnlyCollection<User> users) {
            if (users.Count.Equals(0)) return;
            using (SQLiteConnection db = new SQLiteConnection($"Data Source={Location};Version=3;")) {
                db.Open();

                using (SQLiteDataReader m = new SQLiteCommand("SELECT * FROM messages", db).ExecuteReader()) {
                    while (m.Read())
                        users.Single(e => e.Id.Equals(Convert.ToInt32(m["id"]))).Messages.Add(new Message {
                            Sender = (string)m["sender"],
                            Contents = (string)m["message"],
                            Date = DateTime.Parse((string)m["datetime"])
                        });
                }
            }

            Writer.Log("Messages loaded.", IrcLogEntryType.System);
        }

        /// <summary>
        ///     Execute a query on the database
        /// </summary>
        /// <param name="comamnd"></param>
        internal string Query(SQLiteCommand command) {
            try {
                using (SQLiteConnection db = new SQLiteConnection($"Data Source={Location};Version=3;")) {
                    db.Open();

                    command.Connection = db;
                    command.ExecuteNonQuery();
                }

                return null;
            } catch (Exception e) {
                Writer.Log(e.Message, IrcLogEntryType.Error);
                return e.Message;
            }
        }

        public string Query(string query) {
            try {
                using (SQLiteConnection db = new SQLiteConnection($"Data Source={Location};Version=3;")) {
                    db.Open();

                    using (SQLiteCommand com = new SQLiteCommand(query, db)) {
                        com.ExecuteNonQuery();
                    }
                }

                return null;
            } catch (Exception e) {
                Writer.Log(e.Message, IrcLogEntryType.Error);
                return e.Message;
            }
        }

        /// <summary>
        ///     Returns int value of last ID in default database
        /// </summary>
        /// <returns>
        ///     <see cref="int" />
        /// </returns>
        internal int GetLastDatabaseId() {
            int id = -1;

            using (SQLiteConnection db = new SQLiteConnection($"Data Source={Location};Version=3;")) {
                db.Open();

                using (SQLiteDataReader r = new SQLiteCommand("SELECT MAX(id) FROM users", db).ExecuteReader()) {
                    while (r.Read()) id = Convert.ToInt32(r.GetValue(0));
                }
            }

            return id;
        }
    }
}