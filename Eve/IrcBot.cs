using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using Eve.Ref.Irc;
using Eve.Types;

namespace Eve {
	public partial class IrcBot : Utilities, IDisposable {
		private readonly IrcConfig _config;
		private bool _disposed;

		private TcpClient _connection;
		private StreamWriter _log;
		private NetworkStream _networkStream;
		private StreamWriter _out;
		private StreamReader _streamReader;

		public PropertyReference V { get; set; }

		private bool _joined,
			_identified;

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
			if (_disposed) return;

			if (dispose) {
				_streamReader.Dispose();
				_out.Dispose();
				_networkStream.Dispose();
				_log.Dispose();
				V.Dispose();

				_connection.Close();
			}

			V = new PropertyReference(_config.Database);
			_joined = false;
			_identified = false;

			_disposed = true;
		}

		private bool PreprocessMessage(ChannelMessage c) {
			switch (c.Type) {
				case Protocols.MotdReplyEnd:
					if (_identified ||
						!c.Type.Equals(Protocols.MotdReplyEnd)) return false;

					SendData(_out, Protocols.Privmsg, $"NICKSERV IDENTIFY {_config.Password}");
					SendData(_out, Protocols.Mode, $"{_config.Nickname} +B");

					foreach (string s in _config.Channels) {
						SendData(_out, Protocols.Join, s);
						V.Channels.Add(new Channel {
							Name = s,
							UserList = new List<string>()
						});
					}

					_joined = true;
					_identified = true;
					break;
				case Protocols.Nick:
					using (
						SQLiteCommand com =
							new SQLiteCommand($"UPDATE users SET nickname='{c.Recipient}' WHERE realname='{c.Realname}'", V.Db))
						com.ExecuteNonQuery();
					break;
				case Protocols.Join:
					if (V.QueryName(c.Realname) != null &&
						V.CurrentUser.Messages != null) {
						c.Target = c.Nickname;

						foreach (Message m in V.CurrentUser.Messages)
							c.MultiMessage.Add($"({m.Date}) {m.Sender}: {Regex.Unescape(m.Contents)}");

						V.Users.First(e => e.Realname == c.Realname).Messages = null;

						using (SQLiteCommand x = new SQLiteCommand($"DELETE FROM messages WHERE id={V.CurrentUser.Id}", V.Db))
							x.ExecuteNonQuery();
					}

					AddUserToChannel(c.Recipient, c.Realname);
					break;
				case Protocols.Part:
					RemoveUserFromChannel(c.Recipient, c.Realname);
					break;
				case Protocols.NameReply:
					// splits the channel user list in half by the :, then splits each user into an array object to be iterated
					foreach (string s in c.Args.Split(':')[1].Split(' '))
						AddUserToChannel(c.Recipient, s);
					break;
				default:
					if (!c.MultiArgs[0].Replace(",", string.Empty).CaseEquals(_config.Nickname) ||
						V.IgnoreList.Contains(c.Realname) ||
						GetUserTimeout(c.Realname, V))
						break;

					if (c.MultiArgs.Count < 2) {
						SendData(_out, Protocols.Privmsg, $"{c.Recipient} Please provide a command. Type 'eve help' to view my command list.");
						break;
					}

					if (V.Modules.Select(e => e.Accessor).Contains(c.MultiArgs[1].ToLower())) return false;

					if (c.MultiArgs[1].CaseEquals("help")) {
						SendData(_out, Protocols.Privmsg, $"{c.Recipient} There appears to have been an issue loading my core module. Please notify my operator.");
						break;
					}

					SendData(_out, Protocols.Privmsg, $"{c.Recipient} Invalid command. Type 'eve help' to view my command list.");
					break;
			}

			return true;
		}

		public void DoModuleIteration(ChannelMessage c, PropertyReference v) {
			c.Target = c.Recipient;
			int count = 0;

			//foreach (Module m in v.Modules) {
			//	var ac = ((IModule) Activator.CreateInstance(m.Assembly));
			//	ChannelMessage cm = ac.OnChannelMessage(c, v);

			foreach (ChannelMessage cm in v.Modules.Select(e => ((IModule)Activator.CreateInstance(e.Assembly))
				.OnChannelMessage(c, v))) {
				Console.WriteLine($"-={count}=-");
				count++;

				Console.WriteLine((v.Db.State == ConnectionState.Open) ? "Open" : "Closed");

				if (cm == null) {
					c.Reset();
					continue;
				}

				bool stopLoop = false;

				switch (cm.ExitType) {
					case ExitType.Exit:
						stopLoop = true;
						break;
					case ExitType.MessageAndExit:
						SendData(_out, Protocols.Privmsg, $"{cm.Target} {cm.Message}");
						stopLoop = true;
						break;
					case ExitType.DoNotExit:
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}

				if (stopLoop) {
					c.Reset();
					break;
				}

				if (cm.MultiMessage.Any())
					foreach (string s in cm.MultiMessage)
						SendData(_out, cm.Type, $"{cm.Target} {s}");
				else if (!string.IsNullOrEmpty(cm.Message))
					SendData(_out, cm.Type, $"{cm.Target} {cm.Message}");

				c.Reset();
			}
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
				_out = new StreamWriter(_networkStream);

				if (!File.Exists("logs.txt")) {
					Console.WriteLine("||| Log file not found, creating.");

					File.Create("logs.txt").Close();
				}

				_log = new StreamWriter("logs.txt", true);

				SendData(_out, Protocols.User, $"{_config.Nickname} 0 * {_config.Realname}");
				SendData(_out, Protocols.Nick, _config.Nickname);
			} catch (Exception e) {
				Console.WriteLine($"||| Communication error: {e}");
			}
		}

		/// <summary>
		///     Recieves incoming data, parses it, and passes it to <see cref="DoModuleIteration(ChannelMessage, PropertyReference)" />
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
			if (c.Type.Equals(Protocols.Pong)) {
				SendData(_out, c.Type, c.Message);
				return;
			}

			if (c.Nickname.Equals(_config.Nickname)) return;

			// Write data to console & log in a readable format
			Console.WriteLine(c.Type.Equals(Protocols.Privmsg)
				? $"[{c.Recipient}]({c.Time.ToString("hh:mm:ss")}){c.Nickname}: {c.Args}" : data);
			_log.WriteLine($"({DateTime.Now}) {data}");
			_log.Flush();

			if (PreprocessMessage(c)) return;

			CheckValidChannelAndAdd(c.Recipient);

			if (CheckUserExists(c.Realname))
				UpdateCurrentUserAndInfo(c);
			else if (c.SenderIdentifiable)
				CreateUserAndUpdateCollections(new User {
					Access = 3,
					Nickname = c.Nickname,
					Realname = c.Realname,
					Seen = DateTime.UtcNow,
					Attempts = 0
				});

			DoModuleIteration(c, V);
		}
	}

	// Checks, dos, and adds
	public partial class IrcBot {
		/// <summary>
		/// </summary>
		/// <param name="channel">channel for user to be added to</param>
		/// <param name="realname">user to be added</param>
		public void AddUserToChannel(string channel, string realname) {
			if (!CheckChannelExists(channel, V)) return;

			V.Channels.FirstOrDefault(e => e.Name == channel)?.UserList.Add(realname);
		}

		/// <summary>
		/// Remove a user from a channel's user list
		/// </summary>
		/// <param name="channel">channel's UserList to remove from</param>
		/// <param name="realname">user to remove</param>
		public void RemoveUserFromChannel(string channel, string realname) {
			if (!CheckChannelExists(channel, V)) return;

			Channel firstOrDefault = V.Channels.FirstOrDefault(e => e.Name == channel);
			if (firstOrDefault != null &&
				!firstOrDefault.UserList.Contains(realname)) {
				Console.WriteLine($"||| '{realname}' does not exist in that channel.");
				return;
			}

			V.Channels.FirstOrDefault(e => e.Name == channel)?.UserList.Remove(realname);
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

				if (cmd.Equals(Protocols.Ping)
					||
					cmd.Equals(Protocols.Pong))
					return;

				Console.WriteLine($"{cmd} {param}");
			}
		}

		/// <summary>
		///     Checks whether or not a specified user exists in database
		/// </summary>
		/// <param name="realname">name to check</param>
		/// <returns>true: user exists; false: user does not exist</returns>
		public bool CheckUserExists(string realname) {
			return V.QueryName(realname) != null;
		}

		/// <summary>
		///     Updates specified user's `seen` data
		/// </summary>
		/// <param name="c">ChannelMessage for information to be surmised</param>
		public void UpdateCurrentUserAndInfo(ChannelMessage c) {
			V.Users.First(e => e.Realname == c.Realname).Seen = c.Time;

			using (SQLiteCommand com = new SQLiteCommand($"UPDATE users SET seen='{c.Time}' WHERE realname='{c.Realname}'", V.Db)
				)
				com.ExecuteNonQuery();

			using (
				SQLiteCommand com = new SQLiteCommand($"UPDATE users SET nickname='{c.Nickname}' WHERE realname='{c.Realname}'",
					V.Db))
				com.ExecuteNonQuery();

			V.CurrentUser = V.Users.FirstOrDefault(e => e.Realname == c.Realname);
		}

		/// <summary>
		///     Creates a new user and updates the users & userTimeouts collections
		/// </summary>
		/// <param name="u"><see cref="User" /> object to surmise information from</param>
		public void CreateUserAndUpdateCollections(User u) {
			Console.WriteLine($"||| Creating database entry for {u.Realname}.");

			int id = -1;

			// create data adapter to obtain all id's from users table, for setting new id
			using (SQLiteDataReader x = new SQLiteCommand("SELECT MAX(id) FROM users", V.Db).ExecuteReader())
				while (x.Read())
					id = Convert.ToInt32(x.GetValue(0)) + 1;

			using (
				SQLiteCommand com =
					new SQLiteCommand($"INSERT INTO users VALUES ({id}, '{u.Nickname}', '{u.Realname}', {u.Access}, '{u.Seen}')", V.Db)
				)
				com.ExecuteNonQuery();

			V.Users.Add(u);
		}

		/// <summary>
		///     Adds channel to list of currently connected channels
		/// </summary>
		/// <param name="channel">Channel name to be checked against and added</param>
		public void CheckValidChannelAndAdd(string channel) {
			if (V.Channels.All(e => e.Name != channel) &&
				channel.StartsWith("#"))
				V.Channels.Add(new Channel {
					Name = channel,
					UserList = new List<string>()
				});
		}
	}
}