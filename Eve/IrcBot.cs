using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using Eve.Ref.Irc;
using Eve.Types;

namespace Eve {
	public partial class IrcBot : IDisposable {
		private bool _identified;
		private bool _disposed;

		private static IrcConfig _config;
		private TcpClient _connection;
		private StreamWriter _log;
		private NetworkStream _networkStream;
		private StreamReader _in;
		internal static StreamWriter Out;

		public static PassableMutableObject V { get; private set; }

		/// <summary>
		///     initialises class
		/// </summary>
		/// <param name="config">configuration for object variables</param>
		public IrcBot(IrcConfig config) {
			_config = config;

			V = new PassableMutableObject(_config.Database) {
				IgnoreList = _config.IgnoreList,
			};

			V.ModuleControl = new ModuleManager(ref V.CommandList);

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
		///     Recieves incoming data, parses it, and passes it to <see cref="DoModuleIteration(ChannelMessage)" />
		/// </summary>
		public void Runtime() {
			string data;

			try {
				data = _in.ReadLine();
			} catch (NullReferenceException) {
				Utils.Output("Stream disconnected. Attempting to reconnect.");

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

			V.AddChannel(c.Recipient);

			if (V.GetUser(c.Realname) == null &&
				c.SenderIdentifiable)
				V.CreateUser(new User {
					Access = 3,
					Nickname = c.Nickname,
					Realname = c.Realname,
					Seen = DateTime.UtcNow,
					Attempts = 0
				});

			V.CurrentUser = V.GetUser(c.Realname);
			if (PreprocessMessage(c)) return;
			V.CurrentUser.UpdateUser(c.Nickname);

			DoModuleIteration(c);
		}

		private static void DoModuleIteration(ChannelMessage c) {
			c.Target = c.Recipient;
			
			foreach (Module m in V.ModuleControl.Modules)
			foreach (ChannelMessage cm in m.OnChannelMessageIterate(c, V)) {
				Console.WriteLine("FUCK OFF");

				if (cm == null) {
					Console.WriteLine("FUCK OFF 4, FUCK OFF HARDER");
					c.Reset();
					continue;
				}

				bool stopLoop = false;

				switch (cm.ExitType) {
					case ExitType.Exit:
						stopLoop = true;
						break;
					case ExitType.MessageAndExit:
						SendData(Out, Protocols.Privmsg, $"{cm.Target} {cm.Message}");
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
						SendData(Out, cm.Type, $"{cm.Target} {s}");
				else if (!string.IsNullOrEmpty(cm.Message))
					SendData(Out, cm.Type, $"{cm.Target} {cm.Message}");

				c.Reset();
			}
		}
	}

	// Utilities and general methods
	public partial class IrcBot {
		/// <summary>
		///     Method for initialising all data streams
		/// </summary>
		public void InitializeConnections() {
			int retries = 0;

			while (retries < 3) {
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
					Utils.Output("Communication error, attempting to connect again...");
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
						V.AddChannel(s);
					}

					_identified = true;
					break;
				case Protocols.Nick:
					PassableMutableObject.QueryDefaultDatabase($"UPDATE users SET nickname='{c.Recipient}' WHERE realname='{c.Realname}'");
					break;
				case Protocols.Join:
					if (V.GetUser(c.Realname) != null &&
						V.CurrentUser.Messages.Count > 0) {
						c.Target = c.Nickname;

						foreach (Message m in V.CurrentUser.Messages)
							c.MultiMessage.Add($"({m.Date}) {m.Sender}: {Regex.Unescape(m.Contents)}");

						PassableMutableObject.QueryDefaultDatabase($"DELETE FROM messages WHERE id={V.CurrentUser.Id}");
					}

					V.AddUserToChannel(c.Recipient, c.Realname);
					break;
				case Protocols.Part:
					V.RemoveUserFromChannel(c.Recipient, c.Realname);
					break;
				case Protocols.NameReply:
					// splits the channel user list in half by the :, then splits each user into an array object to be iterated
					foreach (string s in c.Args.Split(':')[1].Split(' '))
						V.AddUserToChannel(c.Recipient, s);
					break;
				default:
					if (!c.MultiArgs[0].Replace(",", string.Empty).CaseEquals(_config.Nickname) ||
						V.IgnoreList.Contains(c.Realname) ||
						V.CurrentUser.GetTimeout())
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
	}
}