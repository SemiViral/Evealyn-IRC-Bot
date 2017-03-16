#region usings

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using Eve.Classes;
using Eve.Plugin;
using Eve.References;

#endregion

namespace Eve {
    public class IrcBot : MarshalByRefObject, IDisposable {
        private StreamReader _in;

        private ChannelMessageEventArgs channelMessage;
        internal BotConfig Config;
        private TcpClient connection;
        private bool disposed;
        private NetworkStream networkStream;

        private string rawData;

        /// <summary>
        ///     initialises class
        /// </summary>
        /// <param name="config">configuration for object variables</param>
        public IrcBot(BotConfig config) {
            Config = config;

            Users = new List<User>();
            Channels = new List<Channel>();
            Wrapper = new PluginWrapper();

            // check if connection is established, don't execute if not
            if (!(CanExecute = InitializeConnections())) return;

            Wrapper.Start();

            Database = new Database(Config.Database);
            Database.Initialise(Users);

            Writer.Initialise(networkStream);
            Writer.SendData(Protocols.USER, $"{Config.Nickname} 0 * {Config.Realname}");
            Writer.SendData(Protocols.NICK, Config.Nickname);

            Initialised = true;
        }

        internal bool Initialised { get; }

        public static string Info
            => "Evealyn is an IRC bot created by SemiViral as a primary learning project for C#. Version 4.1.2";

        public List<string> IgnoreList { get; internal set; } = new List<string>();

        public List<User> Users { get; }

        public List<string> Inhabitants => Channels.SelectMany(e => e.Inhabitants).ToList();
        public List<Channel> Channels { get; }
        public Database Database { get; set; }
        internal PluginWrapper Wrapper { get; }

        public bool CanExecute { get; }

        /// <summary>
        ///     Dispose of all streams and objects
        /// </summary>
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void ExecuteRuntime() {
            ListenToStream();

            if (string.IsNullOrEmpty(rawData)) return;

            if (rawData.StartsWith(Protocols.PING)) {
                Writer.SendData(Protocols.PONG, rawData.Remove(0, 5)); // removes 'PING ' from string
                return;
            }

            if (!RefreshCurrentMessage()) return; // end if message refreshing failed

            if (channelMessage.Recipient.StartsWith("#") &&
                !Channels.Any(e => e.Name.Equals(channelMessage.Recipient)))
                Channels.Add(new Channel(channelMessage.Recipient));

            MessageRecievedOperations();

            PreprocessAndAbort();

            Wrapper.PluginHost.TriggerChannelMessageCallback(this, channelMessage);
        }

        /// <summary>
        ///     Reset the currently active message in memeory
        /// </summary>
        /// <returns>true: </returns>
        private bool RefreshCurrentMessage() {
            channelMessage = new ChannelMessageEventArgs(this, rawData);

            if (channelMessage.Type == Protocols.ABORT) return false;

            Writer.Log(channelMessage.Type.Equals(Protocols.PRIVMSG) ?
                    $"<{channelMessage.Recipient} {channelMessage.Nickname}> {channelMessage.Args}" : rawData,
                EventLogEntryType.Information);

            return true;
        }

        /// <summary>
        ///     Recieves input from open stream
        /// </summary>
        public void ListenToStream() {
            try {
                rawData = _in.ReadLine();
            } catch (NullReferenceException) {
                Writer.Log("Stream disconnected. Attempting to reconnect...", EventLogEntryType.Error);

                InitializeConnections();
            } catch (Exception ex) {
                Writer.Log(ex.ToString(), EventLogEntryType.Error);
            }
        }

        /// <summary>
        ///     Creates a new user and updates the users & userTimeouts collections
        /// </summary>
        /// <param name="access">access level of user</param>
        /// <param name="nickname">nickname of user</param>
        /// <param name="realname">realname of user</param>
        /// <param name="seen">last time user was seen</param>
        /// <param name="id">id of user</param>
        public void CreateUser(int access, string nickname, string realname, DateTime seen, int id = -1) {
            if (Users.Any(e => e.Realname.Equals(realname))) return;

            Users.Add(new User(access, nickname, realname, seen, id));

            Writer.Log($"Creating database entry for {realname}.", EventLogEntryType.Information);

            id = Database.GetLastDatabaseId() + 1;

            Database.QueryDefaultDatabase(
                $"INSERT INTO users VALUES ({id}, '{nickname}', '{realname}', {access}, '{seen}')");
        }

        public void MessageRecievedOperations() {
            User user = Users.SingleOrDefault(e => e.Realname.Equals(channelMessage.Realname));

            if (user == null) return;

            CreateUser(3, channelMessage.Nickname, channelMessage.Realname, channelMessage.Timestamp);
        }

        /// <summary>
        ///     Preprocess ChannelMessage and determine whether to fire OnChannelMessage event
        /// </summary>
        private void PreprocessAndAbort() {
            switch (channelMessage.Type) {
                case Protocols.MOTD_REPLY_END:
                    if (Config.Identified) break;

                    Writer.SendData(Protocols.PRIVMSG, $"NICKSERV IDENTIFY {Config.Password}");
                    Writer.SendData(Protocols.MODE, $"{Config.Nickname} +B");

                    foreach (string channel in Config.Channels) {
                        Writer.SendData(Protocols.JOIN, channel);
                        Channels.Add(new Channel(channel));
                    }

                    Config.Identified = true;
                    break;
                case Protocols.NICK:
                    Database.QueryDefaultDatabase(
                        $"UPDATE users SET nickname='{channelMessage.Recipient}' WHERE realname='{channelMessage.Realname}'");
                    break;
                case Protocols.JOIN:
                    // todo write code to send messages inside user object to channel

                    Channels.SingleOrDefault(e => !e.Name.Equals(channelMessage.Recipient))?
                        .AddUser(channelMessage.Recipient);
                    break;
                case Protocols.PART:
                    Channels.SingleOrDefault(e => e.Name.Equals(channelMessage.Recipient))?
                        .RemoveUser(channelMessage.Realname);
                    break;
                case Protocols.NAME_REPLY:
                    string channelName = channelMessage.SplitArgs[1];

                    // SplitArgs [2] is always your nickname

                    foreach (string s in channelMessage.SplitArgs[3].Split(' ')) {
                        Channel currentChannel = Channels.SingleOrDefault(e => e.Name.Equals(channelName));

                        if (currentChannel == null ||
                            currentChannel.Inhabitants.Contains(s))
                            continue;

                        Channels.Single(e => e.Name.Equals(channelName)).Inhabitants.Add(s);
                    }
                    break;
                default:
                    if (!channelMessage.SplitArgs[0].Replace(",", string.Empty).Equals(Config.Nickname.ToLower()) ||
                        IgnoreList.Contains(channelMessage.Realname)) break;

                    if (channelMessage.SplitArgs.Count < 2) {
                        Writer.Privmsg(channelMessage.Recipient,
                            "Please provide a command. Type 'eve help' to view my command list.");
                        break;
                    }

                    // built-in `help' command
                    if (channelMessage.SplitArgs[1].ToLower().Equals("help"))
                        if (channelMessage.SplitArgs.Count == 2) {
                            Writer.SendData(Protocols.PRIVMSG,
                                $"{channelMessage.Recipient} Active commands: {string.Join(", ", GetCommands().Keys)}");
                            break;
                        } else {
                            if (channelMessage.SplitArgs.Count < 3) {
                                Writer.Privmsg(channelMessage.Recipient, "Insufficient parameters.");
                                break;
                            }

                            if (!PluginWrapper.Commands.ContainsKey(channelMessage.SplitArgs[2])) {
                                Writer.Privmsg(channelMessage.Recipient, "Command not found.");
                                break;
                            }

                            //Writer.Privmsg(Recipient, $"{SplitArgs[2]} {MainBot.GetCommands(SplitArgs[1])}");
                        }

                    Writer.Privmsg(channelMessage.Recipient, "Invalid command. Type 'eve help' to view my command list.");
                    break;
            }
        }

        protected virtual void Dispose(bool dispose) {
            if (!dispose || disposed) return;

            networkStream?.Dispose();
            _in?.Dispose();
            connection?.Close();

            disposed = true;
        }

        /// <summary>
        ///     Method for initialising all data streams
        /// </summary>
        public bool InitializeConnections(int maxRetries = 3) {
            int retries = 0;

            while (retries <= maxRetries)
                try {
                    connection = new TcpClient(Config.Server, Config.Port);
                    networkStream = connection.GetStream();
                    _in = new StreamReader(networkStream);
                    break;
                } catch (SocketException) {
                    Writer.Log(
                        retries <= maxRetries
                            ? "Communication error, attempting to connect again..."
                            : "Communication could not be established with address.",
                        EventLogEntryType.Error);

                    retries++;
                }

            return retries <= maxRetries;
        }

        /// <summary>
        ///     Return list of commands or a single command, or null if the command is unmatched
        /// </summary>
        /// <param name="commands">Command list to be checked and returned, if specified</param>
        /// <returns></returns>
        public Dictionary<string, string> GetCommands(List<string> commands = null) {
            return commands == null ?
                PluginWrapper.Commands :
                new Dictionary<string, string>(
                    PluginWrapper.Commands.Where(e => commands.Contains(e.Key))
                        .ToDictionary(dict => dict.Key, dict => dict.Value));
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