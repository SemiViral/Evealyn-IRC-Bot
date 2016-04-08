#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using Eve.Classes;
using Eve.Plugin;
using Eve.Ref;

#endregion

namespace Eve {
    public partial class IrcBot : IDisposable {
        private TcpClient _connection;
        private bool _disposed;
        private StreamReader _in;
        private NetworkStream _networkStream;
        internal BotConfig Config;

        /// <summary>
        ///     initialises class
        /// </summary>
        /// <param name="config">configuration for object variables</param>
        public IrcBot(BotConfig config) {
            Config = config;

            if (!(CanExecute = InitializeConnections(3))) return;

            Wrapper.Start();

            Database = new Database(Config.Database, Users);

            Writer.Initialise(_networkStream);
            Writer.SendData(Protocols.USER, $"{Config.Nickname} 0 * {Config.Realname}");
            Writer.SendData(Protocols.NICK, Config.Nickname);

            Initialised = true;
        }

        internal bool Initialised { get; }

        public static string Info
            => "Evealyn is an IRC bot created by SemiViral as a primary learning project for C#. Version 4.1.2";

        public List<string> IgnoreList { get; internal set; } = new List<string>();

        public UserOverlord Users { get; } = new UserOverlord();
        public ChannelOverlord Channels { get; } = new ChannelOverlord();
        public Database Database { get; set; }
        internal PluginWrapper Wrapper { get; } = new PluginWrapper();

        public bool CanExecute { get; }

        public void ExecuteRuntime() {
            string data = ListenToStream();

            if (string.IsNullOrEmpty(data)) return;

            ChannelMessageEventArgs message = new ChannelMessageEventArgs(data);

            Writer.Log(message.Type.Equals(Protocols.PRIVMSG) ?
                $"<{message.Recipient} {message.Nickname}> {message.Args}" :
                data, EventLogEntryType.Information);

            if (message.Type == Protocols.ABORT) return;

            Channels.Add(message.Recipient);

            CheckCreateUser(message);
            UpdateCurrentUser(message.Realname);

            Users.LastSeen.UpdateUser(message.Nickname);

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
                Writer.Log("Stream disconnected. Attempting to reconnect...", EventLogEntryType.Error);

                InitializeConnections(4);
            } catch (Exception ex) {
                Writer.Log(ex.ToString(), EventLogEntryType.Error);
            }

            // true means ping check and do succeeded
            return Writer.Ping(data) ? string.Empty : data;
        }
    }
}