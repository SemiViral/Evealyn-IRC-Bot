using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using Eve.Plugin;
using Eve.Ref;
using Eve.Types;
using Eve.Utility;

namespace Eve {
	internal partial class IrcBot : IDisposable {
		private static IrcConfig _config;
		private TcpClient _connection;
		private bool _disposed;
		private bool _identified;
		private StreamReader _in;
		private NetworkStream _networkStream;

		/// <summary>
		///     initialises class
		/// </summary>
		/// <param name="config">configuration for object variables</param>
		public IrcBot(IrcConfig config) {
			_config = config;

			Wrapper.Start();
			if (!(CanExecute = InitializeConnections())) return;

			Writer.Initialise(_networkStream);
			VarManagement = new VariablesManagement(_config.Database) {
				IgnoreList = _config.IgnoreList
			};

			Writer.SendData(Protocols.USER, $"{_config.Nickname} 0 * {_config.Realname}");
			Writer.SendData(Protocols.NICK, _config.Nickname);
		}

		public static VariablesManagement VarManagement { get; set; }
		internal static PluginWrapper Wrapper { get; } = new PluginWrapper();

		public bool CanExecute { get; }

		/// <summary>
		///     Handles the data I/O and plugin firing
		/// </summary>
		public void Runtime() {
			string data;

			try {
				data = _in.ReadLine();
			} catch (NullReferenceException) {
				Writer.Log("Stream disconnected. Attempting to reconnect.", EventLogEntryType.Error);

				InitializeConnections();
				return;
			}

			if (data.StartsWith(Protocols.PING)) {
				// cut 'PING ' from data and send it back
				Writer.SendData($"{Protocols.PONG} {data.Remove(0, 5)}");
				return;
			}

			ChannelMessageEventArgs message = new ChannelMessageEventArgs(data);

			if (message.Nickname.Equals(_config.Nickname)) return;

			Writer.Log(message.Type.Equals(Protocols.PRIVMSG) ?
				$"<{message.Recipient} {message.Nickname}> {message.Args}" :
				data, EventLogEntryType.Information);

			VarManagement.AddChannel(message.Recipient);

			if (VarManagement.GetUser(message.Realname) == null &&
				message.IsRealUser) {
				VarManagement.CreateUser(new User {
					Access = 3,
					Nickname = message.Nickname,
					Realname = message.Realname,
					Seen = DateTime.UtcNow,
					Attempts = 0
				});
			}

			if (PreprocessMessage(message)) return;

			VarManagement.CurrentUser.UpdateUser(message.Nickname);

			Wrapper.PluginHost.TriggerChannelMessageCallback(this, message);
		}
	}
}