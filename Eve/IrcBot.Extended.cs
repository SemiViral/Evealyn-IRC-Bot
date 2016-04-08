#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using Eve.Plugin;

#endregion

namespace Eve {
    // Non-declarative or generic methods
    public partial class IrcBot {
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
        public bool InitializeConnections(int maxRetries) {
            int retries = 0;

            while (retries <= maxRetries) {
                try {
                    _connection = new TcpClient(Config.Server, Config.Port);
                    _networkStream = _connection.GetStream();
                    _in = new StreamReader(_networkStream);
                    break;
                } catch (SocketException) {
                    Writer.Log(
                        retries < maxRetries
                            ? "Communication error, attempting to connect again..."
                            : "Communication could not be established with address.",
                        EventLogEntryType.Error);

                    retries++;
                }
            }

            return retries != 4;
        }

        public void UpdateCurrentUser(string realname) {
            if (Users.Get(realname) != null)
                Users.LastSeen = Users.Get(realname);
        }

        public void CheckCreateUser(ChannelMessageEventArgs message) {
            if (Users.Get(message.Realname) != null ||
                !message.IsRealUser) return;

            Users.Create(3, message.Nickname, message.Realname, message.Timestamp, true);
        }

        /// <summary>
        ///     Return list of commands or a single command, or null if the command is unmatched
        /// </summary>
        /// <param name="command">Command to be checked and returned, if specified</param>
        /// <returns></returns>
        public List<string> GetCommands(string command = null) {
            return command == null ?
                new List<string>(PluginWrapper.Commands.Keys) :
                new List<string> {
                    PluginWrapper.Commands[command]
                };
        }

        /// <summary>
        ///     Checks whether specified comamnd exists
        /// </summary>
        /// <param name="command">comamnd name to be checked</param>
        /// <returns>True: exists; false: does not exist</returns>
        public bool HasCommand(string command) {
            return PluginWrapper.Commands.Keys.Contains(command);
        }
    }
}