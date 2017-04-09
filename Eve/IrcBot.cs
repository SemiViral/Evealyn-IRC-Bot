#region usings

using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using Eve.Classes;
using Eve.Plugin;
using Eve.References;

#endregion

namespace Eve {
    public class IrcBot : MarshalByRefObject, IDisposable {
        private readonly Dictionary<string, string> commands = new Dictionary<string, string>();
        private StreamReader _in;

        internal BotConfig Config;
        private TcpClient connection;
        private bool disposed;
        private NetworkStream networkStream;

        /// <summary>
        ///     Initialises class
        /// </summary>
        /// <param name="config">configuration for object variables</param>
        public IrcBot(BotConfig config) {
            Config = config;

            Users = new List<User>();
            Channels = new List<Channel>();
            Wrapper = new PluginWrapper();

            // check if connection is established, don't execute if not
            if (!(CanExecute = InitializeConnections())) return;

            Wrapper.Start(CommandRegistrarCallbackEvent);
            Wrapper.TerminateBotEvent += delegate { Dispose(); };

            try {
                MainDatabase = new Database(Config.DatabaseLocation);
                Database.InitialiseUsersIntoList(Users);
            } catch (Exception ex) {
                Writer.Log(ex.ToString(), IrcLogEntryType.Error);

                CanExecute = false;
                return;
            }

            Writer.Initialise(networkStream);
            Writer.SendData(Protocols.USER, $"{Config.Nickname} 0 * {Config.Realname}");
            Writer.SendData(Protocols.NICK, Config.Nickname);

            Initialised = true;
        }

        internal bool Initialised { get; }

        public static string Info => "Evealyn is an IRC bot created by SemiViral as a primary learning project for C#. Version 4.1.2";

        public List<string> IgnoreList { get; internal set; } = new List<string>();

        public List<User> Users { get; }

        public List<string> Inhabitants => Channels.SelectMany(e => e.Inhabitants).ToList();
        public List<Channel> Channels { get; }
        internal Database MainDatabase { get; set; }
        internal PluginWrapper Wrapper { get; }

        internal bool CanExecute { get; private set; }

        /// <summary>
        ///     Dispose of all streams and objects
        /// </summary>
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool dispose) {
            if (!dispose || disposed) return;

            networkStream?.Dispose();
            _in?.Dispose();
            connection?.Close();

            disposed = true;
            CanExecute = false;
        }

        public int GetLastDatabaseId() => MainDatabase.GetLastDatabaseId();
        public string Query(SQLiteCommand command) => MainDatabase.Query(command);

        private void Terminate(object sender, EventArgs e) {
            Dispose();
        }

        internal void CommandRegistrarCallbackEvent(object source, CommandRegistrarEventArgs e) {
            commands.Add(e.Key, e.Value);
        }

        /// <summary>
        ///     Initialises all data streams
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
                    Writer.Log(retries <= maxRetries ? "Communication error, attempting to connect again..." : "Communication could not be established with address.", IrcLogEntryType.Error);

                    retries++;
                }

            return retries <= maxRetries;
        }

        /// <summary>
        ///     Recieves input from open stream
        /// </summary>
        private string ListenToStream() {
            string data = string.Empty;
            try {
                data = _in.ReadLine();
            } catch (NullReferenceException) {
                Writer.Log("Stream disconnected. Attempting to reconnect...", IrcLogEntryType.Error);

                InitializeConnections();
            } catch (Exception ex) {
                Writer.Log(ex.ToString(), IrcLogEntryType.Error);
            }

            return data;
        }

        /// <summary>
        ///     Default method to execute bot functions
        /// </summary>
        public void ExecuteRuntime() {
            string rawData = ListenToStream();

            if (string.IsNullOrEmpty(rawData) ||
                CheckIfPing(rawData)) return;

            ChannelMessage channelMessage = new ChannelMessage(this, rawData);

            if (channelMessage.Type.Equals(Protocols.ABORT)) return;

            CheckAddChannel(channelMessage);
            CheckAddUser(channelMessage);

            // PRIVMSG messages are displayed differently to other message types
            Writer.Log(channelMessage.Type.Equals(Protocols.PRIVMSG) ? $"<{channelMessage.Recipient} {channelMessage.Nickname}> {channelMessage.Args}" : rawData, IrcLogEntryType.Message);

            if (PreprocessAndCheckAbort(channelMessage)) return;

            Wrapper.PluginHost.ChannelMessageCallback(this, channelMessage);
        }

        /// <summary>
        ///     Check whether the data recieved is a ping message and reply
        /// </summary>
        /// <param name="rawData"></param>
        /// <returns></returns>
        private static bool CheckIfPing(string rawData) {
            if (!rawData.StartsWith(Protocols.PING)) return false;

            Writer.SendData(Protocols.PONG, rawData.Remove(0, 5)); // removes 'PING ' from string
            return true;
        }

        /// <summary>
        ///     Check if message's channel origin should be added to channel list
        /// </summary>
        /// <param name="channelMessage"></param>
        private void CheckAddChannel(ChannelMessage channelMessage) {
            if (channelMessage.Recipient.StartsWith("#") &&
                !Channels.Any(e => e.Name.Equals(channelMessage.Recipient)))
                Channels.Add(new Channel(channelMessage.Recipient));
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

            Writer.Log($"Creating database entry for {realname}.", IrcLogEntryType.System);

            id = MainDatabase.GetLastDatabaseId() + 1;

            MainDatabase.Query($"INSERT INTO users VALUES ({id}, '{nickname}', '{realname}', {access}, '{seen}')");
        }

        public void CheckAddUser(ChannelMessage channelMessage) {
            User user = Users.SingleOrDefault(e => e.Realname.Equals(channelMessage.Realname));

            if (user == null) return;

            CreateUser(3, channelMessage.Nickname, channelMessage.Realname, channelMessage.Timestamp);
        }

        /// <summary>
        ///     Preprocess ChannelMessage and determine whether to fire OnChannelMessage event
        /// </summary>
        private bool PreprocessAndCheckAbort(ChannelMessage channelMessage) {
            if (channelMessage.Nickname.Equals(Config.Nickname) &&
                channelMessage.Realname.Equals(Config.Realname)) return true;

            switch (channelMessage.Type) {
                case Protocols.MOTD_REPLY_END:
                    if (Config.Identified) return true;

                    Writer.SendData(Protocols.PRIVMSG, $"NICKSERV IDENTIFY {Config.Password}");
                    Writer.SendData(Protocols.MODE, $"{Config.Nickname} +B");

                    foreach (string channel in Config.Channels) {
                        Writer.SendData(Protocols.JOIN, channel);
                        Channels.Add(new Channel(channel));
                    }

                    Config.Identified = true;
                    break;
                case Protocols.NICK:
                    MainDatabase.Query($"UPDATE users SET nickname='{channelMessage.Recipient}' WHERE realname='{channelMessage.Realname}'");
                    break;
                case Protocols.JOIN:
                    // todo write code to send messages inside user object to channel

                    Channels.SingleOrDefault(e => !e.Name.Equals(channelMessage.Recipient))?.AddUser(channelMessage.Recipient);
                    break;
                case Protocols.PART:
                    Channels.SingleOrDefault(e => e.Name.Equals(channelMessage.Recipient))?.Inhabitants.RemoveAll(x => x.Equals(channelMessage.Nickname));
                    break;
                case Protocols.NAME_REPLY:
                    string channelName = channelMessage.SplitArgs[1];

                    // * SplitArgs [2] is always your nickname

                    // in this case, Eve is the only one in the channel
                    if (channelMessage.SplitArgs.Count < 4) break;

                    foreach (string s in channelMessage.SplitArgs[3].Split(' ')) {
                        Channel currentChannel = Channels.SingleOrDefault(e => e.Name.Equals(channelName));

                        if (currentChannel == null ||
                            currentChannel.Inhabitants.Contains(s))
                            continue;

                        Channels.Single(e => e.Name.Equals(channelName)).Inhabitants.Add(s);
                    }
                    break;
                default:
                    if (IgnoreList.Contains(channelMessage.Realname)) return true;

                    if (channelMessage.SplitArgs[0].Replace(",", string.Empty).Equals(Config.Nickname.ToLower())) {
                        channelMessage.SplitArgs.RemoveAt(0); // removes 'eve'/'eve,' from the SplitArgs, it should never be necessary

                        if (channelMessage.SplitArgs.Count < 2) {
                            Writer.Privmsg(channelMessage.Recipient, "Type 'eve help' to view my command list.");
                            return true;
                        }

                        // built-in 'help' command
                        if (channelMessage.SplitArgs[1].ToLower().Equals("help")) {
                            if (channelMessage.SplitArgs.Count.Equals(2)) { // in this case, 'help' is the only text in the string.
                                Writer.Privmsg(channelMessage.Recipient, $"Active commands: {string.Join(", ", commands.Keys)}");
                                return true;
                            }

                            KeyValuePair<string, string> queriedCommand = GetCommand(channelMessage.SplitArgs[2]);

                            Writer.Privmsg(channelMessage.Recipient, queriedCommand.Equals(default(KeyValuePair<string, string>)) ? "Command not found." : $"{queriedCommand.Key}: {queriedCommand.Value}");

                            return true;
                        }

                        if (!HasCommand(channelMessage.SplitArgs[1].ToLower())) {
                            Writer.Privmsg(channelMessage.Recipient, "Invalid command. Type 'eve help' to view my command list.");
                            return true;
                        }
                    }
                    break;
            }

            return false;
        }

        /// <summary>
        ///     Updates specified user's `seen` data and sets user to LastSeen
        /// </summary>
        /// <param name="user">user object</param>
        /// <param name="nickname">name to be checked</param>
        public void UpdateUser(User user, string nickname) {
            user.Seen = DateTime.UtcNow;

            MainDatabase.Query($"UPDATE users SET seen='{DateTime.UtcNow}' WHERE realname='{user.Realname}'");

            if (nickname != user.Nickname) // checks if nickname has changed
                MainDatabase.Query($"UPDATE users SET nickname='{nickname}' WHERE realname='{user.Realname}'");
        }

        /// <summary>
        ///     Adds a Args object to list
        /// </summary>
        /// <param name="user">user object</param>
        /// <param name="m"><see cref="Message" /> to be added</param>
        public bool AddMessage(User user, Message m) {
            if (!string.IsNullOrEmpty(MainDatabase.Query($"INSERT INTO messages VALUES ({user.Id}, '{m.Sender}', '{m.Contents}', '{m.Date}')"))) return false;
            user.Messages.Add(m);
            return true;
        }

        /// <summary>
        ///     Set new access level for user
        /// </summary>
        /// <param name="user">user object</param>
        /// <param name="access">new access level</param>
        public bool SetAccess(User user, int access) {
            if (!string.IsNullOrEmpty(MainDatabase.Query($"UPDATE users SET access={access} WHERE realname='{user.Realname}'")))
                return false;

            user.Access = access;
            return true;
        }

        public string YouTubeAPIKey => Config.YouTubeAPIKey;

        /// <summary>
        ///     Returns a specified command from commands list
        /// </summary>
        /// <param name="command">Command to be returned</param>
        /// <returns></returns>
        public KeyValuePair<string, string> GetCommand(string command) {
            return commands.SingleOrDefault(x => x.Key.Equals(command));
        }

        /// <summary>
        ///     Checks whether specified comamnd exists
        /// </summary>
        /// <param name="command">comamnd name to be checked</param>
        /// <returns>True: exists; false: does not exist</returns>
        public bool HasCommand(string command) {
            return commands.Keys.Contains(command);
        }
    }
}