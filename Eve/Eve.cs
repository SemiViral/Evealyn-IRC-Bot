using System;
using System.Collections.Generic;
using System.ComponentModel;
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

		public int Id { get; set; }
		public string Nickname { get; set; }
		public string Realname { get; set; }
		public int Access { get; set; }
		public DateTime Seen { get; set; }
		public List<Message> Messages { get; set; }
		public int Attempts { get; set; }
	}

	public class Message {
		public string Sender { get; set; }
		public string Contents { get; set; }
		public DateTime Date { get; set; }
	}

	public class Channel {
		public string Name { get; set; }
		public List<string> UserList { get; set; }
	}

	public class IrcConfig {
		public List<string> IgnoreList { get; set; }

		public bool Joined { get; set; }
		public bool Identified { get; set; }

		public string Server { get; set; }
		public string[] Channels { get; set; }
		public string Realname { get; set; }
		public string Nickname { get; set; }
		public string Password { get; set; }
		public string Database { get; set; }

		public int Port { get; set; }
	}

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
				Console.WriteLine("||| Unable to connect to database, error: " + e);
				return;
			}

			CheckUsersTableForEmptyAndFill();
			ReadUsers();
			ReadMessagesIntoUsers();

			if (Users != null) return;

			Console.WriteLine("||| Failed to read from database.");
			Eve.ShouldRun = false;
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
			if (File.Exists(database)) {
				Console.WriteLine("||| Specified database already exists.");
				return;
			}

			database = database.EndsWith(".sqlite") ? database : $"{database}.sqlite";

			try {
				Db = new SQLiteConnection($"Data Source={database};Version=3;");
				Db.Open();

				using (SQLiteCommand com = new SQLiteCommand(
					"CREATE TABLE users (int id, string nickname, string realname, int access, string seen)", Db))
					com.ExecuteNonQuery();

				using (SQLiteCommand com2 =
					new SQLiteCommand("CREATE TABLE messages (int id, string sender, string message, string datetime)", Db))
					com2.ExecuteNonQuery();
			} catch (Exception e) {
				Console.WriteLine($"||| Error occured: {e}");
			}
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

		#region Variable initializations

		public SQLiteConnection Db;
		public User CurrentUser = new User();

		public string Info =
			"Evealyn is a utility IRC bot created by SemiViral as a primary learning project for C#. Version 2.0";

		public List<string> IgnoreList = new List<string>();
		public List<User> Users = new List<User>();
		public readonly List<Channel> Channels = new List<Channel>();

		public readonly Dictionary<string, string> Commands = new Dictionary<string, string>();

		#endregion
	}

	public class IrcBot : IDisposable, IModule {
		private static IrcConfig _config;

		private readonly Regex _messageRegex =
			new Regex(@"^:(?<Sender>[^\s]+)\s(?<Type>[^\s]+)\s(?<Recipient>[^\s]+)\s?:?(?<Args>.*)",
				RegexOptions.Compiled);

		private readonly Regex _pingRegex = new Regex(@"^PING :(?<Message>.+)", RegexOptions.None);
		//private readonly Regex _argMessageRegex = new Regex(@"^:(?<Arg1>[^\s]+)\s(?<Arg2>[^\s]+)\s(?<Arg3>[^\s]+)\s?:?(?<Arg4>.*)", RegexOptions.Compiled);
		private readonly Regex _senderRegex = new Regex(@"^(?<Nickname>[^\s]+)!(?<Realname>[^\s]+)@(?<Hostname>[^\s]+)",
			RegexOptions.Compiled);

		private TcpClient _connection;

		private bool _disposed;
		private StreamWriter _log;
		private NetworkStream _networkStream;
		private StreamReader _streamReader;
		private StreamWriter _streamWriter;

		/// <summary>
		///     initialises class
		/// </summary>
		/// <param name="config">configuration for object variables</param>
		public IrcBot(IrcConfig config) {
			_config = config;
			_config.IgnoreList.Add(_config.Nickname);

			V = new Variables(_config.Database) {
				IgnoreList = _config.IgnoreList
			};

			Modules = LoadModules();
		}

		public static Variables V { get; set; }

		public static Dictionary<string, Type> Modules { get; set; }

		/// <summary>
		///     Dispose of all streams
		/// </summary>
		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		public Dictionary<string, string> Def => null;

		public ChannelMessage OnChannelMessage(ChannelMessage c) {
			c._Args = c.Args?.Trim().Split(new[] {' '}, 4).ToList();
			c.Target = c.Recipient;

			try {
				foreach (ChannelMessage cm in Modules.Values
					.Select(m => ((IModule) Activator.CreateInstance(m)).OnChannelMessage(c))) {
					if (cm == null) continue;

					bool stopLoop = false;
					switch (cm.ExitType) {
						case ExitType.Exit: // Immediately end loop
							stopLoop = true;
							break;
						case ExitType.MessageAndExit: // End loop after sending message
							SendData("PRIVMSG", $"{cm.Target} {cm.Message}");
							stopLoop = true;
							break;
					}

					if (stopLoop) break;

					if (cm.MultiMessage.Any())
						foreach (string s in cm.MultiMessage)
							SendData(cm.Type, $"{cm.Target} {s}");
					else if (!string.IsNullOrEmpty(cm.Message))
						SendData(cm.Type, $"{cm.Target} {cm.Message}");

					c.Reset(c.Recipient);
				}
			} catch (Exception e) {
				Console.WriteLine($"||| Error: {e}");
			}

			return null;
		}

		protected virtual void Dispose(bool dispose) {
			if (_disposed) return;

			if (dispose) {
				_streamReader?.Dispose();
				_streamWriter?.Dispose();
				_networkStream?.Dispose();
				_log?.Dispose();

				_connection?.Close();
			}

			V = new Variables("users.sqlite");
			_config.Joined = false;
			_config.Identified = false;

			_disposed = true;
		}

		/// <summary>
		///     Send raw data to server
		/// </summary>
		/// <param name="cmd">command operation; i.e. PRIVMSG, JOIN, or PART</param>
		/// <param name="param">plain arguments to send</param>
		private void SendData(string cmd, string param) {
			try {
				if (param == null) {
					_streamWriter.WriteLine(cmd);
					_streamWriter.Flush();
					Console.WriteLine(cmd);
				} else {
					_streamWriter.WriteLine($"{cmd} {param}");
					_streamWriter.Flush();

					Console.WriteLine(cmd.Equals("PONG") ? " Pong" : $"{cmd} {param}"); // Output PingPong in a more readable fashion
				}
			} catch {
				Console.WriteLine("||| Failed to send message to server. Attempting reconnection.");
				Dispose();
				InitializeConnections();
			}
		}

		/// <summary>
		///     Writes to log
		/// </summary>
		/// <param name="text">text to written</param>
		private void WriteToLog(string text) {
			_log.WriteLine(text);
			_log.Flush();
		}

		/// <summary>
		///     Parses data into a new ChannelMessage
		/// </summary>
		/// <param name="message">data to be parsed</param>
		/// <returns>new ChannelMessage object</returns>
		private ChannelMessage ParseMessage(string message) {
			if (!_messageRegex.IsMatch(message)) return null;

			Match mVal = _messageRegex.Match(message);
			string mSender = mVal.Groups["Sender"].Value;
			Match sMatch = _senderRegex.Match(mSender);

			ChannelMessage c = new ChannelMessage {
				Nickname = mSender,
				Realname = mSender.ToLower(),
				Hostname = mSender,
				Type = mVal.Groups["Type"].Value,
				Recipient = mVal.Groups["Recipient"].Value.StartsWith(":")
					? mVal.Groups["Recipient"].Value.Substring(1)
					: mVal.Groups["Recipient"].Value,
				Args = mVal.Groups["Args"].Value,
				Time = DateTime.UtcNow
			};
			if (!sMatch.Success) return c;

			string realname = sMatch.Groups["Realname"].Value;
			c.Nickname = sMatch.Groups["Nickname"].Value;
			c.Realname = realname.StartsWith("~") ? realname.Substring(1) : realname;
			c.Hostname = sMatch.Groups["Hostname"].Value;

			return c;
		}

		/// <summary>
		///     Checks whether or not a specified user exists in database
		/// </summary>
		/// <param name="realname">username to check</param>
		/// <returns>true: user exists; false: user does not exist</returns>
		private static bool CheckUserExists(string realname) {
			return V.QueryName(realname) != null;
		}

		/// <summary>
		///     Updates specified user's `seen` data
		/// </summary>
		/// <param name="c">ChannelMessage for information to be surmised</param>
		private static void UpdateUserInfo(ChannelMessage c) {
			V.Users.First(e => e.Realname == c.Realname).Seen = c.Time;

			using (
				SQLiteCommand com = new SQLiteCommand($"UPDATE users SET seen='{c.Time}' WHERE realname='{c.Realname}'",
					V.Db))
				com.ExecuteNonQuery();

			if (V.CurrentUser == null
				|| V.CurrentUser.Nickname != c.Nickname)
				return;

			using (
				SQLiteCommand com = new SQLiteCommand($"UPDATE users SET nickname='{c.Nickname}' WHERE realname='{c.Realname}'",
					V.Db))
				com.ExecuteNonQuery();
		}

		/// <summary>
		///     Creates a new user and updates the users & userTimeouts collections
		/// </summary>
		/// <param name="u">User object to surmise information from</param>
		public static void CreateUserAndUpdateCollections(User u) {
			Console.WriteLine($"||| Creating database entry for {u.Realname}.");

			int id = -1;

			// create data adapter to obtain all id's from users table
			using (SQLiteDataReader x = new SQLiteCommand("SELECT MAX(id) FROM users", V.Db).ExecuteReader())
				while (x.Read())
					id = Convert.ToInt32(x.GetValue(0)) + 1;

			using (
				SQLiteCommand com =
					new SQLiteCommand(
						$"INSERT INTO users VALUES ({id}, '{u.Nickname}', '{u.Realname}', {u.Access}, '{u.Seen}')",
						V.Db))
				com.ExecuteNonQuery();

			V.Users.Add(u);
		}

		/// <summary>
		///     Message NickServ to identify bot and set MODE +B
		/// </summary>
		/// <param name="type">Type to be checked against</param>
		public void CheckDoIdentifyAndJoin(string type) {
			// 376 is end of MOTD command
			if (_config.Identified || !type.Equals("376")) return;

			SendData("PRIVMSG", "NICKSERV IDENTIFY evepass");
			SendData("MODE", "Eve +B");

			foreach (string s in _config.Channels) {
				SendData("JOIN", s);
				V.Channels.Add(new Channel {
					Name = s,
					UserList = new List<string>()
				});
			}

			_config.Joined = true;
			_config.Identified = true;
		}

		/// <summary>
		///     Loads all Type assemblies in ./modules/ into memory
		/// </summary>
		/// <returns>void</returns>
		public static Dictionary<string, Type> LoadModules() {
			var modules = new Dictionary<string, Type>();

			if (!Directory.Exists("modules")) {
				Console.WriteLine("||| Modules directory not found. Creating directory.");
				Directory.CreateDirectory("modules");
			}

			try {
				foreach (KeyValuePair<string, Type> kvp in Assembly.LoadFrom(Path.GetFullPath("modules/Eve.Core.dll")).GetTypes()
					.Select(t => CheckTypeAndLoad(t, modules)).Where(kvp => !kvp.Equals(default(KeyValuePair<string, Type>))))
					modules.Add(kvp.Key, kvp.Value);

				foreach (KeyValuePair<string, Type> kvp in from f in Directory.EnumerateFiles("modules", "Eve.*.dll")
					let r = new RecursiveAssemblyLoader()
					select r.GetAssembly(Path.GetFullPath(f))
					into file
					from t in file.GetTypes()
					select CheckTypeAndLoad(t, modules)
					into kvp
					where !kvp.Equals(default(KeyValuePair<string, Type>))
					select kvp)
					modules.Add(kvp.Key, kvp.Value);
			} catch (ReflectionTypeLoadException ex) {
				StringBuilder sb = new StringBuilder();

				foreach (Exception exSub in ex.LoaderExceptions) {
					sb.AppendLine(exSub.Message);
					FileNotFoundException exFileNotFound = exSub as FileNotFoundException;
					if (!string.IsNullOrEmpty(exFileNotFound?.FusionLog)) {
						sb.AppendLine("Fusion Log:");
						sb.AppendLine(exFileNotFound.FusionLog);
					}

					sb.AppendLine();
				}
				string errorMessage = sb.ToString();
				Console.WriteLine(errorMessage);
			}

			Console.WriteLine($"||| Loaded modules: {string.Join(", ", modules.Keys)}");
			return modules;
		}

		/// <summary>
		///     Handles interface checks on the Types and adds them to the module list.
		///     Commands are also added to list.
		/// </summary>
		/// <param name="type">Type to be checked against IModule interface</param>
		/// <param name="checker">Dictionary to be checked against</param>
		private static KeyValuePair<string, Type> CheckTypeAndLoad(Type type, Dictionary<string, Type> checker) {
			if (type.GetInterface("IModule") == null) return new KeyValuePair<string, Type>();

			if (!type.GetInterface("IModule").IsEquivalentTo(typeof(IModule)))
				return new KeyValuePair<string, Type>();

			if (checker.ContainsValue(type)) return new KeyValuePair<string, Type>();

			Dictionary<string, string> def = ((IModule) Activator.CreateInstance(type)).Def;

			if (def == null) return new KeyValuePair<string, Type>(type.Name.ToLower(), type);

			foreach (KeyValuePair<string, string> s in def.Where(s => !V.Commands.Contains(s)))
				V.Commands.Add(s.Key, s.Value);

			return new KeyValuePair<string, Type>(type.Name.ToLower(), type);
		}

		/// <summary>
		///     Method for initialising all data streams
		/// </summary>
		public void InitializeConnections() {
			try {
				_connection = new TcpClient(_config.Server, _config.Port);
			} catch {
				Console.WriteLine("||| Connection failed.");
				return;
			}

			try {
				_networkStream = _connection.GetStream();
				_streamReader = new StreamReader(_networkStream);
				_streamWriter = new StreamWriter(_networkStream);
				_log = new StreamWriter("./logs.txt", true);

				SendData("USER", $"{_config.Nickname} 0 * {_config.Realname}");
				SendData("NICK", _config.Nickname);
			} catch {
				Console.WriteLine("||| Communication error.");
			}
		}

		/// <summary>
		///     Recieves incoming data, parses it, and passes it to <see cref="OnChannelMessage(ChannelMessage)" />
		/// </summary>
		public void Runtime() {
			string data = _streamReader.ReadLine();
			ChannelMessage c;

			try {
				if (_pingRegex.IsMatch(data)) {
					Console.Write("Ping ...");
					SendData("PONG", _pingRegex.Match(data).Groups["Message"].Value);
					return;
				}

				c = ParseMessage(data); // raw data from stream
			} catch (NullReferenceException) {
				Console.WriteLine("||| Stream disconnected. Attempting to reconnect.");

				Dispose();
				InitializeConnections();
				return;
			}

			if (c.Nickname.Equals(_config.Nickname)) return;

			// Write data to console in a readable format
			Console.WriteLine(
				$"[{(c.Recipient.Equals(_config.Nickname) ? c.Type : c.Recipient)}]({c.Time.ToString("hh:mm:ss")}){c.Nickname}: {c.Args}");

			CheckDoIdentifyAndJoin(c.Type);

			// write timestamp and raw data to log
			WriteToLog($"({DateTime.Now}) {data}");

			// add new channel to the channel list if not contained
			if (V.Channels.All(e => e.Name != c.Recipient)
				&& c.Recipient.StartsWith("#"))
				V.Channels.Add(new Channel {
					Name = c.Recipient,
					UserList = new List<string>()
				});

			// set currentUser to the current user
			V.CurrentUser = V.Users.FirstOrDefault(e => e.Realname == c.Realname);

			if (CheckUserExists(c.Realname))
				UpdateUserInfo(c);
			else if (!V.IgnoreList.Contains(c.Realname.ToLower()))
				CreateUserAndUpdateCollections(new User {
					Access = 3,
					Nickname = c.Nickname,
					Realname = c.Realname,
					Seen = DateTime.UtcNow,
					Attempts = 0
				});

			// queue OnChannelMessage into threadpool
			ThreadPool.QueueUserWorkItem(e => OnChannelMessage(c));
		}
	}

	internal class Eve {
		private static IrcBot _bot;

		public static bool ShouldRun { get; set; } = true;

		private static void ParseAndDo(object sender, DoWorkEventArgs e) {
			while (ShouldRun)
				_bot.Runtime();
		}

		private static void Main() {
			IrcConfig conf = new IrcConfig {
				Realname = "SemiViral",
				Nickname = "Eve",
				Password = "evepass",
				Port = 6667,
				Server = "irc6.foonetic.net",
				Channels = new[] {"#testgrounds", "#ministryofsillywalks" },
				Database = "users.sqlite",
				IgnoreList = new List<string> {
					"nickserv",
					"chanserv",
					"vervet.foonetic.net",
					"belay.foonetic.net",
					"anchor.foonetic.net",
					"daemonic.foonetic.net",
					"staticfree.foonetic.net",
					"services.foonetic.net"
				},
				Joined = false,
				Identified = false
			};

			try {
				_bot = new IrcBot(conf);
			} catch (TypeInitializationException e) {
				Console.WriteLine(e);
			}
			_bot.InitializeConnections();

			BackgroundWorker backgroundDataParser = new BackgroundWorker();
			backgroundDataParser.DoWork += ParseAndDo;
			backgroundDataParser.RunWorkerAsync();

			Console.ReadLine();

			Console.WriteLine("||| Bot has shutdown.");
			Console.ReadLine();
		}
	}

	public class RecursiveAssemblyLoader : MarshalByRefObject {
		public Assembly GetAssembly(string path) {
			return Assembly.LoadFrom(path);
		}
	}

	public static class Extentions {
		/// <summary>
		///     Compares the object to a string with default ignorance of casing
		/// </summary>
		/// <param name="query">string to compare</param>
		/// <param name="ignoreCase">whether or not to ignore case</param>
		/// <returns>true: strings equal; false: strings unequal</returns>
		public static bool CaseEquals(this string obj, string query, bool ignoreCase = true) {
			return obj.Equals(query, ignoreCase ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture);
		}
	}

	public enum ExitType {
		Exit,
		MessageAndExit,
		DoNotExit
	}
}