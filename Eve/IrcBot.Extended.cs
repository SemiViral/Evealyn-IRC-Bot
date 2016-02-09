using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using Eve.Plugin;
using Eve.Ref;
using Eve.Utility;

namespace Eve {
	// Non-declarative or runtime methods
	internal partial class IrcBot {
		/// <summary>
		///     Dispose of all streams and objects
		/// </summary>
		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool dispose) {
			if (!dispose || _disposed) return;

			_networkStream?.Dispose();
			_in?.Dispose();
			_connection?.Close();

			_disposed = true;
		}

		/// <summary>
		///     Method for initialising all data streams
		/// </summary>
		public bool InitializeConnections() {
			int retries = 0;

			while (retries < 3) {
				try {
					_connection = new TcpClient(_config.Server, _config.Port);
					_networkStream = _connection.GetStream();
					_in = new StreamReader(_networkStream);
					break;
				} catch (SocketException) {
					Writer.Log("Communication error, attempting to connect again...", EventLogEntryType.Error);
					retries++;
				}
			}

			return retries != 4;
		}

		/// <summary>
		///     Preprocess ChannelMessage and determine whether to fire OnChannelMessage event
		/// </summary>
		/// <returns>true: loop should continue</returns>
		private bool PreprocessMessage(ChannelMessageEventArgs message) {
			// set the current user
			VarManagement.CurrentUser = VarManagement.GetUser(message.Realname);

			switch (message.Type) {
				case Protocols.MOTD_REPLY_END:
					if (_identified ||
						!message.Type.Equals(Protocols.MOTD_REPLY_END)) return false;

					Writer.SendData(Protocols.PRIVMSG, $"NICKSERV IDENTIFY {_config.Password}");
					Writer.SendData(Protocols.MODE, $"{_config.Nickname} +B");

					foreach (string s in _config.Channels) {
						Writer.SendData(Protocols.JOIN, s);
						VarManagement.AddChannel(s);
					}

					_identified = true;
					break;
				case Protocols.NICK:
					VarManagement.QueryDefaultDatabase(
						$"UPDATE users SET nickname='{message.Recipient}' WHERE realname='{message.Realname}'");
					break;
				case Protocols.JOIN:
					//if (VarManagement.GetUser(message.Realname) != null &&
					//	VarManagement.CurrentUser.Messages.Count > 0) {
					//	message.Target = message.Nickname;

					//	foreach (Message m in VarManagement.CurrentUser.Messages)
					//		message.MultiMessage.Add($"({m.Date}) {m.Sender}: {Regex.Unescape(m.Contents)}");

					//	PassableMutableObject.QueryDefaultDatabase($"DELETE FROM messages WHERE id={VarManagement.CurrentUser.Id}");
					//}

					VarManagement.AddUserToChannel(message.Recipient, message.Realname);
					break;
				case Protocols.PART:
					VarManagement.RemoveUserFromChannel(message.Recipient, message.Realname);
					break;
				case Protocols.NAME_REPLY:
					// splits the channel user list in half by the :, then splits each user into an array object to be iterated
					foreach (string s in message.Args.Split(':')[1].Split(' ')) {
						VarManagement.AddUserToChannel(message.Recipient, s);
					}
					break;
				default:
					if (!message.SplitArgs[0].Replace(",", string.Empty).CaseEquals(_config.Nickname) ||
						VarManagement.IgnoreList.Contains(message.Realname) ||
						VarManagement.CurrentUser.GetTimeout()) break;

					Wrapper.PluginHost.TriggerChannelMessageCallback(this, message);

					if (message.SplitArgs.Count < 2) {
						Writer.SendData(Protocols.PRIVMSG,
							$"{message.Recipient} Please provide a command. Type 'eve help' to view my command list.");
						break;
					}

					if (VarManagement.CommandList.Keys.Contains(message.SplitArgs[1].ToLower())) return false;

					if (message.SplitArgs[1].CaseEquals("help")) {
						Writer.SendData(Protocols.PRIVMSG,
							$"{message.Recipient} There appears to have been an issue loading my core plugin. Please notify my operator.");
						break;
					}

					Writer.SendData(Protocols.PRIVMSG, $"{message.Recipient} Invalid command. Type 'eve help' to view my command list.");
					break;
			}

			return true;
		}
	}
}