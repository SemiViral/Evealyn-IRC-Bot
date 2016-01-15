using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using Eve.Managers.Modules;
using Eve.Managers.Classes;
using System.Threading;

namespace Eve {
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
	
	public class IrcBot : IDisposable, IModule {
		private static IrcConfig _config;

		private TcpClient _connection;

		private bool _disposed;
		private StreamWriter _log;
		private NetworkStream _networkStream;
		private StreamReader _streamReader;
		private StreamWriter _streamWriter;

		public static Variables V { get; set; }

		public Dictionary<string, string> Def => null;

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

			ModuleManager moduleManager = new ModuleManager();
			V.Modules = moduleManager.LoadModules(V.Commands);
		}

		/// <summary>
		///     Dispose of all streams
		/// </summary>
		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		

		public ChannelMessage OnChannelMessage(ChannelMessage c, Variables v) {
			try {
				foreach (ChannelMessage cm in V.Modules.Values
					.Select(m => ((IModule)Activator.CreateInstance(m)).OnChannelMessage(c, V)).Where(cm => cm != null)) {

					bool stopLoop = false;
					switch (cm.ExitType) {
						case ExitType.Exit:
							stopLoop = true;
							break;
						case ExitType.MessageAndExit:
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
			} catch (MissingMemberException e) {
				Console.WriteLine(e);

				Console.ReadLine();
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

					Console.WriteLine(cmd.Equals("PONG") ? "Pong" : $"{cmd} {param}"); // Output PingPong in a more readable fashion
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

			using (SQLiteCommand com = new SQLiteCommand($"UPDATE users SET seen='{c.Time}' WHERE realname='{c.Realname}'", V.Db))
				com.ExecuteNonQuery();

			if (V.CurrentUser == null || V.CurrentUser.Nickname != c.Nickname)
				return;

			using (SQLiteCommand com = new SQLiteCommand($"UPDATE users SET nickname='{c.Nickname}' WHERE realname='{c.Realname}'", V.Db))
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

			using (SQLiteCommand com = new SQLiteCommand($"INSERT INTO users VALUES ({id}, '{u.Nickname}', '{u.Realname}', {u.Access}, '{u.Seen}')", V.Db))
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
					Name = s, UserList = new List<string>()
				});
			}

			_config.Joined = true;
			_config.Identified = true;
		}

		public void CheckChannelExistsAndAdd(Channel chan) {
			V.Channels.Add(chan);
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
		///     Recieves incoming data, parses it, and passes it to <see cref="OnChannelMessage(ChannelMessage, Variables)" />
		/// </summary>
		public void Runtime() {
			string data;

			try {
				data = _streamReader.ReadLine();
			} catch (NullReferenceException) {
				Console.WriteLine("||| Stream disconnected. Attempting to reconnect.");

				Dispose();
				InitializeConnections();
				return;
			}
	
			ChannelMessage c = new ChannelMessage(data);
			if (c.Type.Equals("PONG")) {
				SendData(c.Type, c.Message);
				return;
			}

			if (c.Nickname.Equals(_config.Nickname)) return;

			// Write data to console & log in a readable format
			Console.WriteLine(c.Type.Equals("PRIVMSG")
				? $"[{c.Recipient}]({c.Time.ToString("hh:mm:ss")}){c.Nickname}: {c.Args}"
				: data);

			WriteToLog($"({DateTime.Now}) {data}");

			CheckDoIdentifyAndJoin(c.Type);

			if (V.Channels.All(e => e.Name != c.Recipient) && c.Recipient.StartsWith("#"))
				CheckChannelExistsAndAdd(new Channel {
					Name = c.Recipient, UserList = new List<string>()
				});

			if (CheckUserExists(c.Realname)) {
				UpdateUserInfo(c);

				V.CurrentUser = V.Users.FirstOrDefault(e => e.Realname == c.Realname);
			} else if (!V.IgnoreList.Contains(c.Realname.ToLower()))
				CreateUserAndUpdateCollections(new User {
					Access = 3, Nickname = c.Nickname, Realname = c.Realname, Seen = DateTime.UtcNow, Attempts = 0
				});

			// queue OnChannelMessage into threadpool
			ThreadPool.QueueUserWorkItem(e => OnChannelMessage(c, V));
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
				Channels = new[] {"#testgrounds"}, //, "#ministryofsillywalks" },
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
}