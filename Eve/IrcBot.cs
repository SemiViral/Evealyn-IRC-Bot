using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using Eve.Plugin;
using Eve.Ref;
using Eve.Types;

namespace Eve {
	internal partial class IrcBot : IDisposable {
		internal static IrcConfig Config;
		private TcpClient _connection;
		private bool _disposed;
		private StreamReader _in;
		private NetworkStream _networkStream;

		/// <summary>
		///     initialises class
		/// </summary>
		/// <param name="config">configuration for object variables</param>
		public IrcBot(IrcConfig config) {
			Config = config;

			if (!(CanExecute = InitializeConnections())) return;

			Wrapper.Start();

			Database = new Database(Config.Database);

			Writer.Initialise(_networkStream);
			Writer.SendData(Protocols.USER, $"{Config.Nickname} 0 * {Config.Realname}");
			Writer.SendData(Protocols.NICK, Config.Nickname);
		}

		public static string Info
			=> "Evealyn is an IRC bot created by SemiViral as a primary learning project for C#. Version 4.1.2";

		public static List<string> IgnoreList { get; internal set; } = new List<string>();

		public static Database Database { get; set; }
		internal static PluginWrapper Wrapper { get; } = new PluginWrapper();

		public bool CanExecute { get; }

		public void ExecuteRuntime() {
			string data = ListenToStream();

			if (string.IsNullOrEmpty(data)) return;

			ChannelMessageEventArgs message = new ChannelMessageEventArgs(data);

			Writer.Log(message.Type.Equals(Protocols.PRIVMSG) ?
				$"<{message.Recipient} {message.Nickname}> {message.Args}" :
				data, EventLogEntryType.Information);

			if (message.Type == Protocols.ABORT) return;

			Channel.Add(message.Recipient);

			if (User.Get(message.Realname) == null &&
				message.IsRealUser) {
				User.Create(3, message.Nickname, message.Realname, DateTime.UtcNow, true);
				User.Current = User.Get(message.Realname);
			}

			User.Current.UpdateUser(message.Nickname);

			Wrapper.PluginHost.TriggerChannelMessageCallback(this, message);
		}

		/// <summary>
		///     Handles the data I/O and plugin firing
		/// </summary>
		public string ListenToStream() {
			string data = string.Empty;

			try {
				data = _in.ReadLine();
			} catch (NullReferenceException) {
				Writer.Log("Stream disconnected. Attempting to reconnect.", EventLogEntryType.Error);

				InitializeConnections();
			} catch (Exception ex) {
				Writer.Log(ex.ToString(), EventLogEntryType.Error);
			}

			// true means ping check and do succeeded
			return Writer.Ping(data) ? string.Empty : data;
		}
	}
}