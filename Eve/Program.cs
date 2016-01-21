using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using Eve.Types.Classes;
using Eve.Types.Irc;

namespace Eve {
	internal class IrcBot : Utilities, IDisposable, IModule {
		private IrcConfig _config;
		private bool _disposed;

		private TcpClient _connection;
		private StreamWriter _log;
		private NetworkStream _networkStream;
		private StreamWriter _streamWriter;
		private StreamReader _streamReader;

		public PropertyReference V { get; set; }

		public Dictionary<string, string> Def => null;

		/// <summary>
		///     initialises class
		/// </summary>
		/// <param name="config">configuration for object variables</param>
		public IrcBot(IrcConfig config) {
			_config = config;

			V = new PropertyReference(_config.Database);
			V.Modules = ModuleManager.LoadModules(V.Commands);
			V.IgnoreList = _config.IgnoreList;
		}

		/// <summary>
		///     Dispose of all streams and objects
		/// </summary>
		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		
		protected virtual void Dispose(bool dispose) {
			if (_disposed) return;

			if (dispose) {
				_streamReader.Dispose();
				_streamWriter.Dispose();
				_networkStream.Dispose();
				_log.Dispose();

				_connection.Close();
			}

			V = new PropertyReference(_config.Database);
			_config.Joined = false;
			_config.Identified = false;

			_disposed = true;
		}

		public ChannelMessage OnChannelMessage(ChannelMessage c, PropertyReference v) {
			c.Target = c.Recipient;
	
			foreach (ChannelMessage cm in v.Modules.Values
				.Select(m => ((IModule) Activator.CreateInstance(m)).OnChannelMessage(c, v))) {
				bool stopLoop = false;

				switch (cm.ExitType) {
					case ExitType.Exit:
						stopLoop = true;
						break;
					case ExitType.MessageAndExit:
						SendData(_streamWriter, IrcProtocol.Privmsg, $"{cm.Target} {cm.Message}");
						stopLoop = true;
						break;
					case ExitType.DoNotExit:
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}

				if (stopLoop) break;

				if (cm.MultiMessage.Any())
					foreach (string s in cm.MultiMessage)
						SendData(_streamWriter, cm.Type, $"{cm.Target} {s}");
				else if (!string.IsNullOrEmpty(cm.Message))
					SendData(_streamWriter, cm.Type, $"{cm.Target} {cm.Message}");

				c.Reset();
			}

			return null;
		}

		/// <summary>
		///     Message NickServ to identify bot and set MODE +B
		/// </summary>
		/// <param name="type">Type to be checked against</param>
		public void CheckDoIdentifyAndJoin(string type) {
			// 376 is end of MOTD command
			if (_config.Identified ||
				!type.Equals(IrcProtocol.MotdReplyEnd)) return;

			SendData(_streamWriter, IrcProtocol.Privmsg, "NICKSERV IDENTIFY evepass");
			SendData(_streamWriter, IrcProtocol.Mode, "Eve +B");

			foreach (string s in _config.Channels) {
				SendData(_streamWriter, IrcProtocol.Join, s);
				V.Channels.Add(new Channel {
					Name = s,
					UserList = new List<string>()
				});
			}

			_config.Joined = true;
			_config.Identified = true;
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

				if (!File.Exists("logs.txt")) {
					Console.WriteLine("||| Log file not found, creating.");

					File.Create("logs.txt").Close();
				}

				_log = new StreamWriter("logs.txt", true);

				SendData(_streamWriter, IrcProtocol.User, $"{_config.Nickname} 0 * {_config.Realname}");
				SendData(_streamWriter, IrcProtocol.Nick, _config.Nickname);
			} catch (Exception e) {
				Console.WriteLine($"||| Communication error: {e}");
			}
		}

		/// <summary>
		///     Recieves incoming data, parses it, and passes it to <see cref="OnChannelMessage(ChannelMessage, PropertyReference)" />
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
			if (c.Type.Equals(IrcProtocol.Pong)) {
				SendData(_streamWriter, c.Type, c.Message);
				return;
			}

			if (c.Nickname.Equals(_config.Nickname)) return;

			// Write data to console & log in a readable format
			Console.WriteLine(c.Type.Equals(IrcProtocol.Privmsg) ? $"[{c.Recipient}]({c.Time.ToString("hh:mm:ss")}){c.Nickname}: {c.Args}" : data);
			_log.WriteLine($"({DateTime.Now}) {data}");
			_log.Flush();

			CheckDoIdentifyAndJoin(c.Type);
			CheckValidChannelAndAdd(V, c.Recipient);

			if (CheckUserExists(V, c.Realname))
				UpdateCurrentUserAndInfo(V, c);
			else if (c.SenderIdentifiable)
				CreateUserAndUpdateCollections(V, new User {
					Access = 3,
					Nickname = c.Nickname,
					Realname = c.Realname,
					Seen = DateTime.UtcNow,
					Attempts = 0
				});

			OnChannelMessage(c, V);
		}
	}

	internal class Program {
		private static IrcBot _bot;
		public static IrcConfig Config;

		public static bool ShouldRun { get; set; } = true;

		private static void ParseAndDo(object sender, DoWorkEventArgs e) {
			while (ShouldRun)
				_bot.Runtime();
		}

		private static void Main() {
			Config = Utilities.CheckConfigExistsAndReturn();
			Console.WriteLine("||| Configuration file loaded.");

			try {
				_bot = new IrcBot(Config);
			} catch (TypeInitializationException e) {
				Console.WriteLine(e);
			}
			_bot.InitializeConnections();

			//BackgroundWorker backgroundDataParser = new BackgroundWorker();
			//backgroundDataParser.DoWork += ParseAndDo;
			//backgroundDataParser.RunWorkerAsync();

			while (ShouldRun) {
				_bot.Runtime();

				//string command = Console.ReadLine();
			}
			Console.WriteLine("||| Bot has shutdown.");
			Console.ReadLine();
		}
	}
}