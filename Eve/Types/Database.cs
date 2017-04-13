#region usings

using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Eve.Types.Irc;

#endregion

namespace Eve.Types {
    public class Database {
        internal static string Location { get; private set; }
        internal static bool Connected { get; private set; }
        internal event EventHandler<LogEntry> LogEntryEventHandler;

        /// <summary>
        ///     Initialise connections to database and sets properties
        /// </summary>
        internal void Initialise(string databaseLocation) {
            Location = databaseLocation;

            if (!File.Exists(Location))
                CreateDatabase();
            else
                CheckUsersTableForEmptyAndFill();

            Log(IrcLogEntryType.System, "Loaded database.");
        }

        internal void InitialiseUsersIntoList(ICollection<User> users) {
            CheckUsersTableForEmptyAndFill();
            ReadUsersIntoList(users);
            ReadMessagesIntoUsers(users);

            Log(IrcLogEntryType.System, "Users successfully loaded from database.");
        }

        private void CreateDatabase() {
            Log(IrcLogEntryType.System, "MainDatabase not found, creating.");

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

        private void CheckUsersTableForEmptyAndFill() {
            using (SQLiteConnection db = new SQLiteConnection($"Data Source={Location};Version=3;")) {
                db.Open();

                using (SQLiteCommand a = new SQLiteCommand("SELECT COUNT(id) FROM users", db)) {
                    if (Convert.ToInt32(a.ExecuteScalar()) != 0)
                        return;

                    Log(IrcLogEntryType.System, "Inhabitants table in database is empty. Creating initial record.");

                    using (SQLiteCommand b = new SQLiteCommand($"INSERT INTO users VALUES (0, '0', '0', 9, '{DateTime.UtcNow}')", db)) {
                        b.ExecuteNonQuery();
                    }
                }
            }
        }

        private void ReadUsersIntoList(ICollection<User> users) {
            using (SQLiteConnection db = new SQLiteConnection($"Data Source={Location};Version=3;")) {
                db.Open();

                using (SQLiteDataReader userEntry = new SQLiteCommand("SELECT * FROM users", db).ExecuteReader()) {
                    while (userEntry.Read())
                        users.Add(new User((int)userEntry["access"], (string)userEntry["nickname"], (string)userEntry["realname"], DateTime.Parse((string)userEntry["seen"]), (int)userEntry["id"]));
                }
            }

            Log(IrcLogEntryType.System, "User list loaded.");
        }

        private void ReadMessagesIntoUsers(ICollection<User> users) {
            if (users.Count.Equals(0))
                return;
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

            Log(IrcLogEntryType.System, "Messages loaded.");
        }

        /// <summary>
        ///     Execute a query on the database
        /// </summary>
        internal void Query(SQLiteCommand command) {
            using (SQLiteConnection db = new SQLiteConnection($"Data Source={Location};Version=3;")) {
                db.Open();

                command.Connection = db;
                command.ExecuteNonQuery();
            }
        }

        public void Query(string query) {
            using (SQLiteConnection db = new SQLiteConnection($"Data Source={Location};Version=3;")) {
                db.Open();

                using (SQLiteCommand com = new SQLiteCommand(query, db)) {
                    com.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        ///     Returns int value of last ID in default database
        /// </summary>
        internal int GetLastDatabaseId() {
            int id = -1;

            using (SQLiteConnection db = new SQLiteConnection($"Data Source={Location};Version=3;")) {
                db.Open();

                using (SQLiteDataReader r = new SQLiteCommand("SELECT MAX(id) FROM users", db).ExecuteReader()) {
                    while (r.Read())
                        id = Convert.ToInt32(r.GetValue(0));
                }
            }

            return id;
        }

        private void Log(IrcLogEntryType entryType, string message, [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0) {
            LogEntryEventHandler?.Invoke(this, new LogEntry(entryType, message, memberName, lineNumber));
        }
    }
}