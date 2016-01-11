using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Eve {
	public class User {
		public User() {
			Messages = new List<Message>();
		}

		public int ID { get; set; }
		public string Nickname { get; set; }
		public string Realname { get; set; }
		public int Access { get; set; }
		public DateTime Seen { get; set; }
		public List<Message> Messages { get; set; }
	}

	public class Message {
		public string Sender { get; set; }
		public string Contents { get; set; }
		public DateTime Date { get; set; }
	}

	public struct Config {
		public bool Joined;
		public bool Identified;
		public string Server;
		public string[] Channels;
		public string Nick;
		public string Name;
		public int Port;
	}

	public class Variables : IDisposable {
		#region Variable initializations
		public SQLiteConnection db;
		public User currentUser = new User();
		public string info = "Evealyn is a utility IRC bot created by SemiViral as a primary learning project for C#. Version 2.0";

		public List<User> users = new List<User>();
		public readonly List<string> channels = new List<string>();
		public readonly List<string> ignoreList = new List<string>
		{
			"eve",
			"nickserv",
			"chanserv",
			"vervet.foonetic.net",
			"belay.foonetic.net",
			"anchor.foonetic.net",
			"daemonic.foonetic.net",
			"staticfree.foonetic.net"
		};

		public readonly Dictionary<string, List<string>> userChannelList = new Dictionary<string, List<string>>();
		public readonly Dictionary<string, int> userAttempts = new Dictionary<string, int>();
		public readonly Dictionary<string, string> commands = new Dictionary<string, string>();
		#endregion

		public Variables(bool initDB) {
			if (initDB) {
				if (!File.Exists("users.sqlite")) {
					Console.WriteLine("||| Users database does not exist. Creating database.");

					SQLiteConnection.CreateFile("users.sqlite");

					db = new SQLiteConnection("Data Source=users.sqlite;Version=3;");
					db.Open();

					var com = new SQLiteCommand("CREATE TABLE users (int id, string nickname, string realname, int access, string seen)", db);
					var com2 = new SQLiteCommand("CREATE TABLE messages (int id, string sender, string message, string datetime)", db);
					com.ExecuteNonQuery();
					com2.ExecuteNonQuery();
				} else {
					try {
						db = new SQLiteConnection("Data Source=users.sqlite;Version=3;");
						db.Open();
					} catch (Exception e) {
						Console.WriteLine("||| Unable to connec to database, error: " + e);
					}
				}

				using (SQLiteCommand a = new SQLiteCommand("SELECT COUNT(id) FROM users", db))
					if (Convert.ToInt32(a.ExecuteScalar()) == 0) {
						Console.WriteLine("||| Users table in database is empty. Creating initial record.");

						using (SQLiteCommand b = new SQLiteCommand("INSERT INTO users (id, nickname, realname, access) VALUES (0, 'semiviral', 'semiviral', 0) ", db))
							b.ExecuteNonQuery();
					}

				using (SQLiteDataReader d = new SQLiteCommand("SELECT * FROM users", db).ExecuteReader()) {
					while (d.Read()) {
						users.Add(new User() {
							ID = (int)d["id"],
							Nickname = (string)d["nickname"],
							Realname = (string)d["realname"],
							Access = (int)d["access"],
							Seen = DateTime.Parse((string)d["seen"])
						});
					}

					try {
						using (SQLiteDataReader m = new SQLiteCommand("SELECT * FROM messages", db).ExecuteReader()) {
							while (m.Read()) {
								users.FirstOrDefault(e => e.ID == Int64.Parse(m["id"].ToString())).Messages.Add(new Message() {
									Sender = (string)m["sender"],
									Contents = (string)m["message"],
									Date = DateTime.Parse((string)m["datetime"])
								});
							}
						}
					} catch (NullReferenceException) {
						Console.WriteLine("||| NullReferenceException occured upon loading messages from database. This most likely means a user record was deleted and the ID cannot be referenced from the message entry.");
					}

					if (users == null) {
						Console.WriteLine("||| Failed to read from database.");
						return;
					}
				}
			}
		}

		public User QueryName(string name) {
			return users.FirstOrDefault(e => e.Realname == name);
		}

		public void Dispose() {
			db?.Close();
		}
	}

	public class IRCBot : IDisposable, IModule {
		private Config config;
		private TcpClient connection;
		private StreamWriter log;
		private NetworkStream networkStream;
		private StreamReader streamReader;
		private StreamWriter streamWriter;

		private static Variables _v = new Variables(true);
		public static Variables v {
			get { return _v; }
			set { _v = value; }
		}

		private static Dictionary<string, Type> _modules = LoadModules();
		public static Dictionary<string, Type> modules {
			get { return _modules; }
			set { _modules = value; }
		}

		public Dictionary<string, string> def { get { return null; } }

		// set config
		public IRCBot(Config config) {
			this.config = config;
		}

		public void Dispose() {
			streamReader?.Close();
			streamWriter?.Close();
			networkStream?.Close();
			log?.Close();
			connection?.Close();
		}

		public ChannelMessage OnChannelMessage(ChannelMessage c) {
			c._Args = (c.Args != null) ?
				c.Args.Trim().Split(new[] { ' ' }, 4).ToList() : null;

			try {
				foreach (Type m in modules.Values) {
					ChannelMessage cm = ((IModule)Activator.CreateInstance(m)).OnChannelMessage(c);

					if (cm != null && !_v.ignoreList.Contains(c.Realname)) {
						if (cm._Args != null)
							foreach (string s in cm._Args)
								SendData(cm.Type, $"{cm.Nickname} {s}");
						else if (!String.IsNullOrEmpty(cm.Args))
							SendData(cm.Type, $"{cm.Nickname} {cm.Args}");
					}
				}
			} catch (Exception e) {
				Console.WriteLine($"||| Error: {e}");
			}

			if (c.Type == "MODE" && !config.Identified) {
				SendData("PRIVMSG", "NICKSERV IDENTIFY evepass");
				SendData("MODE", "Eve +B");

				foreach (string s in config.Channels) {
					SendData("JOIN", s);
					_v.channels.Add(s);
				}

				config.Identified = true;
			} return null;
		}

		// send raw data to server
		private void SendData(string cmd, string param) {
			try {
				if (param == null) {
					streamWriter.WriteLine(cmd);
					streamWriter.Flush();
					Console.WriteLine(cmd);
				} else {
					streamWriter.WriteLine($"{cmd} {param}");
					streamWriter.Flush();
					Console.WriteLine($"{cmd} {param}");
                }
			} catch {
				Console.WriteLine("||| Failed to send message to server. Attempting reconnection.");
				Dispose();
				InitializeConnections();
			}
		}

		// <summary>
		// Method for initialising all data streams
		// </summary>
		public void InitializeConnections() {
			try {
				connection = new TcpClient(config.Server, config.Port);
			} catch {
				Console.WriteLine("||| Connection failed.");
				return;
			}

			try {
				networkStream = connection.GetStream();
				streamReader = new StreamReader(networkStream);
				streamWriter = new StreamWriter(networkStream);
				log = new StreamWriter("./logs.txt", true);

				SendData("USER", $"{config.Nick} 0 * {config.Name}");
				SendData("NICK", config.Nick);
			} catch {
				Console.WriteLine("||| Communication error.");
				return;
			}
		}

		// <summary>
		// Recieves input from stream and identifies it,
		// parsing the message into an array from regex
		// </summary>
		public void Runtime() {
			var data = streamReader.ReadLine(); // raw data from stream
			var messageTime = DateTime.UtcNow;
			var messageRegex = new Regex(@"^:(?<Sender>[^\s]+)\s(?<Type>[^\s]+)\s(?<Recipient>[^\s]+)\s?:?(?<Args>.*)",
				RegexOptions.Compiled);
			//var argMessageRegex = new Regex(@"^:(?<Arg1>[^\s]+)\s(?<Arg2>[^\s]+)\s(?<Arg3>[^\s]+)\s?:?(?<Arg4>.*)", RegexOptions.Compiled);
			var senderRegex = new Regex(@"^(?<Nickname>[^\s]+)!(?<Realname>[^\s]+)@(?<Hostname>[^\s]+)", RegexOptions.Compiled);
			var pingRegex = new Regex(@"^PING :(?<Message>.+)", RegexOptions.None);

			// write raw data if conditions not met
			if (pingRegex.IsMatch(data))
				Console.WriteLine(data);

			// write timestamp and raw data to log
			log.WriteLine($"({DateTime.Now}) {data}");
			log.Flush();

			if (messageRegex.IsMatch(data)) {
				var mVal = messageRegex.Match(data);
				var mSender = mVal.Groups["Sender"].Value;
				var sMatch = senderRegex.Match(mSender);

				// initialise new ChannelMessage to passed into OnChannelMessage()
				ChannelMessage c = new ChannelMessage {
					Nickname = mSender,
					Realname = mSender.ToLower(),
					Hostname = mSender,
					Type = mVal.Groups["Type"].Value,
					Recipient = mVal.Groups["Recipient"].Value.StartsWith(":")  // Checks if first argument starts with a colon
						? mVal.Groups["Recipient"].Value.Substring(1)           // if so, remove it
						: mVal.Groups["Recipient"].Value,
					Args = mVal.Groups["Args"].Value,
					Time = DateTime.UtcNow
				};

				// if mVal["Sender"] matches Sender regex, reset the values of ChannelMessage c
				if (sMatch.Success) {
					var realname = sMatch.Groups["Realname"].Value;
					c.Nickname = sMatch.Groups["Nickname"].Value;
					c.Realname = realname.StartsWith("~") ? realname.Substring(1) : realname;
					c.Hostname = sMatch.Groups["Hostname"].Value;
				}

				// if user exists in database, update their last seen datetime
				if (_v.QueryName(c.Realname) != null) {
					_v.users.First(e => e.Realname == c.Realname).Seen = DateTime.UtcNow;
					using (var com = new SQLiteCommand($"UPDATE users SET seen='{DateTime.UtcNow}' WHERE realname='{c.Realname}'", _v.db))
						com.ExecuteNonQuery();
				} else if (_v.QueryName(c.Realname) == null
					&& !_v.ignoreList.Contains(c.Realname)) { // checks if user exists and is also not in the ignoreList
					Console.WriteLine($"||| User {c.Realname} currently not in database. Creating database entry for user.");

					int id = -1;

					// create data adapter to obtain all id's from users table
					using (var x = new SQLiteCommand("SELECT MAX(id) FROM users", v.db).ExecuteReader())
						while (x.Read())
							Int32.TryParse(x.GetValue(0).ToString(), out id);

					// insert new user record into database
					using (var com = new SQLiteCommand($"INSERT INTO users (id, nickname, realname, access, seen) VALUES ({id + 1}, '{c.Nickname}', '{c.Realname}', 3, '{DateTime.UtcNow}')", _v.db))
						com.ExecuteNonQuery();

					// add new User instance to the list of users
					_v.users.Add(new User() {
						ID = id,
						Nickname = c.Nickname.ToLower(),
						Realname = c.Realname,
						Access = 2,
						Seen = DateTime.UtcNow
					});
				}

				// if current user doesn't exist in userAttempts, add it
				if (!_v.userAttempts.ContainsKey(c.Realname))
					_v.userAttempts.Add(c.Realname, 0);

				// add new channel to the channel list if not contained
				if (!_v.userChannelList.ContainsKey(c.Recipient)
					&& c.Recipient.StartsWith("#"))
					_v.userChannelList.Add(c.Recipient, new List<string>());

				// set currentUser to the current user or null if it doesn't exist
				_v.currentUser = _v.users.FirstOrDefault(e => e.Realname == c.Realname);

				// Write data to console in a more readable format
				Console.WriteLine($"[{c.Recipient}]({DateTime.UtcNow.ToString("hh:mm:ss")}){c.Nickname}: {c.Args}");

				// queue OnChannelMessage into threadpool
				ThreadPool.QueueUserWorkItem(e => OnChannelMessage(c));
			} else if (pingRegex.IsMatch(data)) {
				SendData("PONG", pingRegex.Match(data).Groups["Message"].Value);
			}
		}

		public static Dictionary<string, Type> LoadModules() {
			Dictionary<string, Type> modules = new Dictionary<string, Type>();

			if (!Directory.Exists("modules")) {
				Console.WriteLine("||| Modules directory not found. Creating directory.");
				Directory.CreateDirectory("modules");
			}

			foreach (string f in Directory.EnumerateFiles("modules", "Eve.*.dll")) {
				RecursiveAssemblyLoader r = new RecursiveAssemblyLoader();
				Assembly file = r.GetAssembly(Path.GetFullPath(f));

				try {
					foreach (Type t in file.GetTypes()) {
						if (t.GetInterface("IModule") == null) continue;

						if (t.GetInterface("IModule").IsEquivalentTo(typeof(IModule))) {
							if (modules.ContainsValue(t)) continue;
							modules.Add(t.Name.ToLower(), t); // add module to module dictionary

							// instance the current type and set it's def clause equal to def
							var def = ((IModule)Activator.CreateInstance(t)).def;

							if (def != null)
								foreach (var s in def)
									if (!_v.commands.Contains(s))
										_v.commands.Add(s.Key, s.Value);
						}
					}
				} catch (InvalidCastException e) {
					Console.WriteLine(e.ToString());
				} catch (NullReferenceException e) {
					Console.WriteLine(e.ToString());
				} catch (ReflectionTypeLoadException ex) {
					StringBuilder sb = new StringBuilder();
					foreach (Exception exSub in ex.LoaderExceptions) {
						sb.AppendLine(exSub.Message);
						FileNotFoundException exFileNotFound = exSub as FileNotFoundException;
						if (exFileNotFound != null) {
							if (!string.IsNullOrEmpty(exFileNotFound.FusionLog)) {
								sb.AppendLine("Fusion Log:");
								sb.AppendLine(exFileNotFound.FusionLog);
							}
						}
						sb.AppendLine();
					}
					string errorMessage = sb.ToString();
					Console.WriteLine(errorMessage);
				}
			}

			Console.WriteLine($"||| Loaded modules: {String.Join(", ", modules.Keys)}");
            return modules;
		}
	}

	internal class Eve {
		private static bool Run = true;
		public static bool shouldRun {
			get { return Run; }
			set { Run = value; }
		}

		private static void Main(string[] args) {
			Config conf = new Config {
				Name = "SemiViral",
				Nick = "Eve",
				Port = 6667,
				Server = "irc6.foonetic.net",
				Channels = new string[] { "#testgrounds2" },// "#ministryofsillywalks" },
				Joined = false,
				Identified = false
			};

			using (IRCBot bot = new IRCBot(conf)) {
				bot.InitializeConnections();

				while (Run)
					bot.Runtime();
			}

			Console.WriteLine("||| Bot has shutdown.");
			Console.ReadLine();
		}
	}

	public class RecursiveAssemblyLoader : MarshalByRefObject {
		public Assembly GetAssembly(string path) {
			return Assembly.LoadFrom(path);
		}
	}
}