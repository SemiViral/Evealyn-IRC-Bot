using System;
using System.Collections.Generic;
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
	}

	public class Message {
		public string Sender { get; set; }
		public string Contents { get; set; }
		public DateTime Date { get; set; }
	}

	public class Config {
		public bool Joined { get; set; }
		public bool Identified { get; set; }
		public string Server { get; set; }
		public string[] Channels { get; set; }
		public string Nick { get; set; }
		public string Name { get; set; }
		public int Port { get; set; }
	}

	public class Variables : IDisposable {
		public Variables(bool initDb) {
			if (!initDb) return;

			if (!File.Exists("users.sqlite")) {
				Console.WriteLine("||| Users database does not exist. Creating database.");

				SQLiteConnection.CreateFile("users.sqlite");

				Db = new SQLiteConnection("Data Source=users.sqlite;Version=3;");
				Db.Open();

				SQLiteCommand com = new SQLiteCommand(
					"CREATE TABLE users (int id, string nickname, string realname, int access, string seen)", Db);
				SQLiteCommand com2 =
					new SQLiteCommand("CREATE TABLE messages (int id, string sender, string message, string datetime)", Db);
				com.ExecuteNonQuery();
				com2.ExecuteNonQuery();
			}
			else
				try {
					Db = new SQLiteConnection("Data Source=users.sqlite;Version=3;");
					Db.Open();
				}
				catch (Exception e) {
					Console.WriteLine("||| Unable to connec to database, error: " + e);
				}

			using (SQLiteCommand a = new SQLiteCommand("SELECT COUNT(id) FROM users", Db))
				if (Convert.ToInt32(a.ExecuteScalar()) == 0) {
					Console.WriteLine("||| Users table in database is empty. Creating initial record.");

					using (
						SQLiteCommand b =
							new SQLiteCommand("INSERT INTO users VALUES (0, 'semiviral', 'semiviral', 0) ",
								Db))
						b.ExecuteNonQuery();
				}

			using (SQLiteDataReader d = new SQLiteCommand("SELECT * FROM users", Db).ExecuteReader()) {
				while (d.Read())
					Users.Add(new User {
						Id = (int) d["id"],
						Nickname = (string) d["nickname"],
						Realname = (string) d["realname"],
						Access = (int) d["access"],
						Seen = DateTime.Parse((string) d["seen"])
					});

				try {
					using (SQLiteDataReader m = new SQLiteCommand("SELECT * FROM messages", Db).ExecuteReader())
						while (m.Read())
							Users.FirstOrDefault(e => e.Id == Convert.ToInt32(m["id"]))?.Messages.Add(new Message {
								Sender = (string) m["sender"],
								Contents = (string) m["message"],
								Date = DateTime.Parse((string) m["datetime"])
							});
				}
				catch (NullReferenceException) {
					Console.WriteLine(
						"||| NullReferenceException occured upon loading messages from database. This most likely means a user record was deleted and the ID cannot be referenced from the message entry.");
				}

				if (Users != null) return;

				Console.WriteLine("||| Failed to read from database.");
			}
		}

		public void Dispose() {
			Db?.Close();
		}

		public User QueryName(string name) {
			return Users.FirstOrDefault(e => e.Realname == name);
		}

		#region Variable initializations

		public SQLiteConnection Db;
		public User CurrentUser = new User();

		public string Info =
			"Evealyn is a utility IRC bot created by SemiViral as a primary learning project for C#. Version 2.0";

		public List<User> Users = new List<User>();
		public readonly List<string> Channels = new List<string>();

		public readonly List<string> IgnoreList = new List<string> {
			"eve",
			"nickserv",
			"chanserv",
			"vervet.foonetic.net",
			"belay.foonetic.net",
			"anchor.foonetic.net",
			"daemonic.foonetic.net",
			"staticfree.foonetic.net"
		};

		public readonly Dictionary<string, List<string>> UserChannelList = new Dictionary<string, List<string>>();
		public readonly Dictionary<string, int> UserAttempts = new Dictionary<string, int>();
		public readonly Dictionary<string, string> Commands = new Dictionary<string, string>();

		#endregion
	}

	public class IrcBot : IDisposable, IModule {
		private Config _config;
		private TcpClient _connection;
		private StreamWriter _log;
		private NetworkStream _networkStream;
		private StreamReader _streamReader;
		private StreamWriter _streamWriter;

		// set config
		public IrcBot(Config config) {
			_config = config;
		}

		public static Variables V { get; set; } = new Variables(true);

		public static Dictionary<string, Type> Modules { get; set; } = LoadModules();
		
		public void Dispose() {
			_streamReader?.Close();
			_streamWriter?.Close();
			_networkStream?.Close();
			_log?.Close();
			_connection?.Close();
		}

		public Dictionary<String, String> Def => null;
		
		public ChannelMessage OnChannelMessage(ChannelMessage c) {
			// 376 is end of MOTD command
			if (c.Type == "376" && !_config.Identified) {
				SendData("PRIVMSG", "NICKSERV IDENTIFY evepass");
				SendData("MODE", "Eve +B");

				foreach (string s in _config.Channels) {
					SendData("JOIN", s);
					V.Channels.Add(s);
				}

				_config.Joined = true;
				_config.Identified = true;
			}

			c._Args = c.Args?.Trim().Split(new[] {' '}, 4).ToList();
			c.Target = c.Recipient;

			try {
				foreach (ChannelMessage cm in Modules.Values
					.Select(m => ((IModule) Activator.CreateInstance(m)).OnChannelMessage(c))) {
					if (cm == null) continue;

					var stopLoop = false;
					switch (cm.ExitType) {
						case 0: // Immediately end loop
							stopLoop = true;
							break;
						case 1: // End loop after sending message
							SendData("PRIVMSG", $"{cm.Target} {cm.Message}");
							stopLoop = true;
							break;
					}

					if (stopLoop) break;

					if (cm.MultiMessage.Any())
						foreach (string s in cm.MultiMessage)
							SendData(cm.Type, $"{cm.Target} {s}");
					else if (!String.IsNullOrEmpty(cm.Message))
						SendData(cm.Type, $"{cm.Target} {cm.Message}");

					c.Reset(c.Recipient);
				}
			}
			catch (Exception e) {
				Console.WriteLine($"||| Error: {e}");
			}
			return null;
		}

		// send raw data to server
		private void SendData(string cmd, string param) {
			try {

				if (param == null) {
					_streamWriter.WriteLine(cmd);
					_streamWriter.Flush();
					Console.WriteLine(cmd);
				}
				else {
					_streamWriter.WriteLine($"{cmd} {param}");
					_streamWriter.Flush();
					
					Console.WriteLine(cmd == "PONG" ? "Pong" : $"{cmd} {param}");
				}
			}
			catch {
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
				_connection = new TcpClient(_config.Server, _config.Port);
			}
			catch {
				Console.WriteLine("||| Connection failed.");
				return;
			}

			try {
				_networkStream = _connection.GetStream();
				_streamReader = new StreamReader(_networkStream);
				_streamWriter = new StreamWriter(_networkStream);
				_log = new StreamWriter("./logs.txt", true);

				SendData("USER", $"{_config.Nick} 0 * {_config.Name}");
				SendData("NICK", _config.Nick);
			}
			catch {
				Console.WriteLine("||| Communication error.");
			}
		}

		/// <summary>
		///     Recieves incoming data, parses it, and passes it to <see cref="OnChannelMessage(ChannelMessage)" />
		/// </summary>
		public void Runtime() {
			String data = _streamReader.ReadLine(); // raw data from stream
			if (data == null) return;

			DateTime messageTime = DateTime.UtcNow;
			Regex messageRegex = new Regex(@"^:(?<Sender>[^\s]+)\s(?<Type>[^\s]+)\s(?<Recipient>[^\s]+)\s?:?(?<Args>.*)",
				RegexOptions.Compiled);
			//var argMessageRegex = new Regex(@"^:(?<Arg1>[^\s]+)\s(?<Arg2>[^\s]+)\s(?<Arg3>[^\s]+)\s?:?(?<Arg4>.*)", RegexOptions.Compiled);
			Regex senderRegex = new Regex(@"^(?<Nickname>[^\s]+)!(?<Realname>[^\s]+)@(?<Hostname>[^\s]+)", RegexOptions.Compiled);
			Regex pingRegex = new Regex(@"^PING :(?<Message>.+)", RegexOptions.None);

			// write raw data if conditions not met
			if (pingRegex.IsMatch(data))
				Console.Write("Ping ... ");

			// write timestamp and raw data to log
			_log.WriteLine($"({DateTime.Now}) {data}");
			_log.Flush();

			if (messageRegex.IsMatch(data)) {
				Match mVal = messageRegex.Match(data);
				String mSender = mVal.Groups["Sender"].Value;
				Match sMatch = senderRegex.Match(mSender);

				// initialise new ChannelMessage to passed into OnChannelMessage()
				ChannelMessage c = new ChannelMessage {
					Nickname = mSender,
					Realname = mSender.ToLower(),
					Hostname = mSender,
					Type = mVal.Groups["Type"].Value,
					Recipient = mVal.Groups["Recipient"].Value.StartsWith(":") // Checks if first argument starts with a colon
						? mVal.Groups["Recipient"].Value.Substring(1) // if so, remove it
						: mVal.Groups["Recipient"].Value,
					Args = mVal.Groups["Args"].Value,
					Time = messageTime
				};

				// if mVal["Sender"] matches Sender regex, reset the values of ChannelMessage c
				if (sMatch.Success) {
					String realname = sMatch.Groups["Realname"].Value;
					c.Nickname = sMatch.Groups["Nickname"].Value;
					c.Realname = realname.StartsWith("~") ? realname.Substring(1) : realname;
					c.Hostname = sMatch.Groups["Hostname"].Value;
				}

				// if user exists in database, update their last seen datetime and check if their nickname has changed
				if (V.QueryName(c.Realname) != null) {
					V.Users.First(e => e.Realname == c.Realname).Seen = messageTime;
					using (
						SQLiteCommand com = new SQLiteCommand($"UPDATE users SET seen='{messageTime}' WHERE realname='{c.Realname}'",
							V.Db))
						com.ExecuteNonQuery();
			
					if (V.CurrentUser != null)
						if (V.CurrentUser.Nickname != c.Nickname)
							using (
								SQLiteCommand com = new SQLiteCommand($"UPDATE users SET nickname='{c.Nickname}' WHERE realname='{c.Realname}'",
									V.Db))
								com.ExecuteNonQuery();
				}
				else if (V.QueryName(c.Realname) == null
						 && !V.IgnoreList.Contains(c.Realname.ToLower())) {
					// checks if user exists and is also not in the ignoreList
					Console.WriteLine($"||| User {c.Realname} currently not in database. Creating database entry for user.");

					int id = -1;

					// create data adapter to obtain all id's from users table
					using (SQLiteDataReader x = new SQLiteCommand("SELECT MAX(id) FROM users", V.Db).ExecuteReader())
						while (x.Read())
							id = Convert.ToInt32(x.GetValue(0)) + 1;

					// insert new user record into database
					using (
						SQLiteCommand com =
							new SQLiteCommand(
								$"INSERT INTO users VALUES ({id}, '{c.Nickname}', '{c.Realname}', 3, '{messageTime}')",
								V.Db))
						com.ExecuteNonQuery();

					// add new User instance to the list of users
					V.Users.Add(new User {
						Id = id,
						Nickname = c.Nickname.ToLower(),
						Realname = c.Realname,
						Access = 2,
						Seen = messageTime
					});
				}

				// if current user doesn't exist in userAttempts, add it
				if (!V.UserAttempts.ContainsKey(c.Realname))
					V.UserAttempts.Add(c.Realname, 0);

				// add new channel to the channel list if not contained
				if (!V.UserChannelList.ContainsKey(c.Recipient)
					&& c.Recipient.StartsWith("#"))
					V.UserChannelList.Add(c.Recipient, new List<string>());

				// set currentUser to the current user
				V.CurrentUser = V.Users.FirstOrDefault(e => e.Realname == c.Realname);

				// Write data to console in a more readable format
				Console.WriteLine($"[{c.Recipient}]({messageTime.ToString("hh:mm:ss")}){c.Nickname}: {c.Args}");
			
				// queue OnChannelMessage into threadpool
				ThreadPool.QueueUserWorkItem(e => OnChannelMessage(c));
			}
			else if (pingRegex.IsMatch(data))
				SendData("PONG", pingRegex.Match(data).Groups["Message"].Value);
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
				foreach (KeyValuePair<String, Type> kvp in Assembly.LoadFrom(Path.GetFullPath("modules/Eve.Core.dll")).GetTypes()
					.Select(t => TypeCheckAndDo(t, modules)).Where(kvp => !kvp.Equals(default(KeyValuePair<string, Type>))))
					modules.Add(kvp.Key, kvp.Value);

				foreach (KeyValuePair<String, Type> kvp in from f in Directory.EnumerateFiles("modules", "Eve.*.dll")
					let r = new RecursiveAssemblyLoader()
					select r.GetAssembly(Path.GetFullPath(f))
					into file
					from t in file.GetTypes()
					select TypeCheckAndDo(t, modules)
					into kvp
					where !kvp.Equals(default(KeyValuePair<string, Type>))
					select kvp)
					modules.Add(kvp.Key, kvp.Value);
			}
			catch (InvalidCastException e) {
				Console.WriteLine(e.ToString());
			}
			catch (NullReferenceException e) {
				Console.WriteLine(e.ToString());
			}
			catch (ReflectionTypeLoadException ex) {
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

			Console.WriteLine($"||| Loaded modules: {String.Join(", ", modules.Keys)}");
			return modules;
		}

		/// <summary>
		///     Handles interface checks on the Types and adds them to the module list.
		///     Commands are also added to list.
		/// </summary>
		/// <param name="type">Type to be checked against IModule interface</param>
		/// <param name="checker">Dictionary to be checked against</param>
		private static KeyValuePair<string, Type> TypeCheckAndDo(Type type, Dictionary<string, Type> checker) {
			if (type.GetInterface("IModule") == null) return new KeyValuePair<string, Type>();

			if (!type.GetInterface("IModule").IsEquivalentTo(typeof(IModule)))
				return new KeyValuePair<string, Type>();
		
			if (checker.ContainsValue(type)) return new KeyValuePair<string, Type>();
			// instance the current type and set it's def clause equal to def
			Dictionary<String, String> def = ((IModule) Activator.CreateInstance(type)).Def;

			if (def == null) return new KeyValuePair<string, Type>(type.Name.ToLower(), type);
		
			foreach (KeyValuePair<String, String> s in def.Where(s => !V.Commands.Contains(s)))
				V.Commands.Add(s.Key, s.Value);

			return new KeyValuePair<string, Type>(type.Name.ToLower(), type);
		}
	}

	internal class Eve {
		public static bool ShouldRun { get; set; } = true;

		private static void Main() {
			Config conf = new Config {
				Name = "SemiViral",
				Nick = "Eve",
				Port = 6667,
				Server = "irc6.foonetic.net",
				Channels = new[] {"#testgrounds2" },//, "#ministryofsillywalks" },
				Joined = false,
				Identified = false
			};

			using (IrcBot bot = new IrcBot(conf)) {
				bot.InitializeConnections();

				while (ShouldRun)
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
}