using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using Eve.Ref.Irc;
using Eve.Types;

namespace Eve {
	public partial class IrcBot : Utilities, IDisposable {
		private bool _identified;
		private bool _disposed;

		private static IrcConfig _config;
		private TcpClient _connection;
		private StreamWriter _log;
		private NetworkStream _networkStream;
		private StreamReader _in;
		internal static StreamWriter Out;

		public static PropertyReference V { get; set; }

		/// <summary>
		///     initialises class
		/// </summary>
		/// <param name="config">configuration for object variables</param>
		public IrcBot(IrcConfig config) {
			_config = config;

			V = new PropertyReference(_config.Database) {
				IgnoreList = _config.IgnoreList
			};

			InitializeConnections();
		}

		/// <summary>
		///     Dispose of all streams and objects
		/// </summary>
		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool dispose) {
			if (!dispose || _disposed) return;
			_networkStream.Dispose();
			_in.Dispose();
			Out.Dispose();
			_log.Dispose();
			_connection.Close();

			_disposed = true;
		}

		/// <summary>
		///     Recieves incoming data, parses it, and passes it to <see cref="DoModuleIteration(ChannelMessage, PropertyReference)" />
		/// </summary>
		public void Runtime() {
			string data;

			try {
				data = _in.ReadLine();
			} catch (NullReferenceException) {
				Console.WriteLine("||| Stream disconnected. Attempting to reconnect.");

				InitializeConnections();
				return;
			}

			ChannelMessage c = new ChannelMessage(data);
			if (c.Type.Equals(Protocols.Pong)) {
				SendData(Out, c.Type, c.Message);
				return;
			}

			if (c.Nickname.Equals(_config.Nickname)) return;

			// Write data to console & log in a readable format
			Console.WriteLine(c.Type.Equals(Protocols.Privmsg)
				? $"[{c.Recipient}]({c.Time.ToString("hh:mm:ss")}){c.Nickname}: {c.Args}" : data);
			_log.WriteLine($"({DateTime.Now}) {data}");
			_log.Flush();

			Channel.CheckAdd(c.Recipient);

			if (User.CheckExists(c.Realname))
				User.UpdateCurrentUser(c.Realname, c.Nickname);
			else if (c.SenderIdentifiable)
				User.Create(new User {
					Access = 3,
					Nickname = c.Nickname,
					Realname = c.Realname,
					Seen = DateTime.UtcNow,
					Attempts = 0
				});

			if (PreprocessMessage(c)) return;
			ThreadPool.QueueUserWorkItem(e => DoModuleIteration(c, V));
		}

		private void DoModuleIteration(ChannelMessage c, PropertyReference v) {
			c.Target = c.Recipient;

			//foreach (Module m in v.Modules) {
			//	var ac = ((IModule) Activator.CreateInstance(m.Assembly));
			//	ChannelMessage cm = ac.OnChannelMessage(c, v);

			//foreach (ChannelMessage cm in v.Modules.Select(e => ((IModule)Activator.CreateInstance(e.Assembly)).OnChannelMessage(c, v))) {
			//	if (cm == null) {
			//		c.Reset();
			//		continue;
			//	}

			//	bool stopLoop = false;

			//	switch (cm.ExitType) {
			//		case ExitType.Exit:
			//			stopLoop = true;
			//			break;
			//		case ExitType.MessageAndExit:
			//			SendData(Out, Protocols.Privmsg, $"{cm.Target} {cm.Message}");
			//			stopLoop = true;
			//			break;
			//		case ExitType.DoNotExit:
			//			break;
			//		default:
			//			throw new ArgumentOutOfRangeException();
			//	}

			//	if (stopLoop) {
			//		c.Reset();
			//		break;
			//	}

			//	if (cm.MultiMessage.Any())
			//		foreach (string s in cm.MultiMessage)
			//			SendData(Out, cm.Type, $"{cm.Target} {s}");
			//	else if (!string.IsNullOrEmpty(cm.Message))
			//		SendData(Out, cm.Type, $"{cm.Target} {cm.Message}");

			//	c.Reset();
			//}
		}
	}

	// Utilities and general methods
	public partial class IrcBot {
		/// <summary>
		///     Method for initialising all data streams
		/// </summary>
		public void InitializeConnections() {
			int retries = 0;

			while (retries < 4) {
				try {
					_connection = new TcpClient(_config.Server, _config.Port);
					_networkStream = _connection.GetStream();
					_in = new StreamReader(_networkStream);
					Out = new StreamWriter(_networkStream);
					_log = File.AppendText("logs.txt");

					SendData(Out, Protocols.User, $"{_config.Nickname} 0 * {_config.Realname}");
					SendData(Out, Protocols.Nick, _config.Nickname);
					break;
				} catch (SocketException) {
					Console.WriteLine($"||| Communication error, attempting to connect again...");
					retries++;
				}
			}
		}

		/// <summary>
		/// Preprocess ChannelMessage and determine whether to send it to modules
		/// </summary>
		/// <param name="c"><see cref="ChannelMessage"/> to be processed</param>
		/// <returns>true: loop should continue</returns>
		private bool PreprocessMessage(ChannelMessage c) {
			switch (c.Type) {
				case Protocols.MotdReplyEnd:
					if (_identified ||
						!c.Type.Equals(Protocols.MotdReplyEnd)) return false;

					SendData(Out, Protocols.Privmsg, $"NICKSERV IDENTIFY {_config.Password}");
					SendData(Out, Protocols.Mode, $"{_config.Nickname} +B");

					foreach (string s in _config.Channels) {
						SendData(Out, Protocols.Join, s);
						V.Channels.Add(new Channel {
							Name = s,
							UserList = new List<string>()
						});
					}

					_identified = true;
					break;
				case Protocols.Nick:
					QueryDefaultDatabase($"UPDATE users SET nickname='{c.Recipient}' WHERE realname='{c.Realname}'");
					break;
				case Protocols.Join:
					if (V.QueryName(c.Realname) != null &&
						V.CurrentUser.Messages != null) {
						c.Target = c.Nickname;

						foreach (Message m in V.CurrentUser.Messages)
							c.MultiMessage.Add($"({m.Date}) {m.Sender}: {Regex.Unescape(m.Contents)}");

						V.Users.First(e => e.Realname == c.Realname).Messages = new List<Message>();

						QueryDefaultDatabase($"DELETE FROM messages WHERE id={V.CurrentUser.Id}");
					}

					Channel.AddUserToChannel(c.Recipient, c.Realname);
					break;
				case Protocols.Part:
					Channel.RemoveUserFromChannel(c.Recipient, c.Realname);
					break;
				case Protocols.NameReply:
					// splits the channel user list in half by the :, then splits each user into an array object to be iterated
					foreach (string s in c.Args.Split(':')[1].Split(' '))
						Channel.AddUserToChannel(c.Recipient, s);
					break;
				default:
					if (!c.MultiArgs[0].Replace(",", string.Empty).CaseEquals(_config.Nickname) ||
						V.IgnoreList.Contains(c.Realname) ||
						User.GetTimeout(c.Realname))
						break;

					if (c.MultiArgs.Count < 2) {
						SendData(Out, Protocols.Privmsg, $"{c.Recipient} Please provide a command. Type 'eve help' to view my command list.");
						break;
					}

					if (V.CommandList.Keys.Contains(c.MultiArgs[1].ToLower())) return false;

					if (c.MultiArgs[1].CaseEquals("help")) {
						SendData(Out, Protocols.Privmsg, $"{c.Recipient} There appears to have been an issue loading my core module. Please notify my operator.");
						break;
					}

					SendData(Out, Protocols.Privmsg, $"{c.Recipient} Invalid command. Type 'eve help' to view my command list.");
					break;
			}

			return true;
		}



		/// <summary>
		///     Send raw data to server
		/// </summary>
		/// <param name="streamWriter"></param>
		/// <param name="cmd">command operation; i.e. PRIVMSG, JOIN, or PART</param>
		/// <param name="param">plain arguments to send</param>
		public static void SendData(StreamWriter streamWriter, string cmd, string param = null) {
			if (param == null) {
				streamWriter.WriteLine(cmd);
				streamWriter.Flush();

				Console.WriteLine(cmd);
			} else {
				streamWriter.WriteLine($"{cmd} {param}");
				streamWriter.Flush();

				if (cmd.Equals(Protocols.Ping) ||
					cmd.Equals(Protocols.Pong))
					return;

				Console.WriteLine($"{cmd} {param}");
			}
		}



		/// <summary>
		/// Execute a query on the default IrcBot database
		/// </summary>
		/// <param name="query"></param>
		public static void QueryDefaultDatabase(string query) {
			using (SQLiteConnection db = new SQLiteConnection($"Data Source={_config.Database};Version=3;"))
			using (SQLiteCommand com = new SQLiteCommand(query, db)) {
				db.Open();
				com.ExecuteNonQuery();
			}
		}

		/// <summary>
		/// Returns int value of last ID in database
		/// </summary>
		/// <returns><see cref="Int32"/></returns>
		public static int GetLastDatabaseId() {
			int id = -1;

			using (SQLiteConnection db = new SQLiteConnection($"Data Source={_config.Database};Version=3;")) {
				db.Open();

				using (SQLiteDataReader r = new SQLiteCommand("SELECT MAX(id) FROM users", db).ExecuteReader())
					while (r.Read())
						id = Convert.ToInt32(r.GetValue(0));
			}
			return id;
		}
	}
}