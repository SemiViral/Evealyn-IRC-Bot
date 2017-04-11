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
using Newtonsoft.Json;

#endregion

namespace Eve {
    public class IrcBot : MarshalByRefObject, IDisposable {
        private readonly Writer logger;
        private StreamReader _in;

        internal BotConfig Config;
        private TcpClient connection;
        private bool disposed;
        private NetworkStream networkStream;

        /// <summary>
        ///     Initialises class
        /// </summary>
        public IrcBot() {
            InitialiseConfig();

            Users = new List<User>();
            Channels = new List<Channel>();
            Wrapper = new PluginWrapper();

            // check if connection is established, don't execute if not
            if (!(CanExecute = InitializeNetworkStream()))
                return;

            logger = new Writer(networkStream);

            InitailisePluginWrapper();
            InitializeDatabase();

            logger.SendData(Protocols.USER, $"{Config.Nickname} 0 * {Config.Realname}");
            logger.SendData(Protocols.NICK, Config.Nickname);

            RegisterMethods();

            Initialised = true;
        }


        #region Non-Critical Variables

        private Database MainDatabase { get; set; }
        private PluginWrapper Wrapper { get; }

        internal bool Initialised { get; }

        public static string Info => "Evealyn is an IRC bot created by SemiViral as a primary learning project for C#. Version 4.1.2";

        public List<string> IgnoreList { get; internal set; } = new List<string>();

        public List<User> Users { get; }

        public List<string> Inhabitants => Channels.SelectMany(e => e.Inhabitants).ToList();
        public List<Channel> Channels { get; }

        internal bool CanExecute { get; private set; }

        public string GetApiKey(string type) => Config.ApiKeys[type];

        #endregion


        #region End-process

        /// <summary>
        ///     Dispose of all streams and objects
        /// </summary>
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool dispose) {
            if (!dispose || disposed)
                return;

            networkStream?.Dispose();
            _in?.Dispose();
            logger?.Dispose();
            connection?.Dispose();

            disposed = true;
            CanExecute = false;
        }

        private void Terminate(object sender, EventArgs e) {
            Dispose();

            logger.Log(IrcLogEntryType.System, "Bot has shutdown. Press any key to exit program.");
            Console.ReadKey();
        }

        #endregion


        #region Initializations

        private void InitialiseConfig() {
            Config = new BotConfig();

            if (!File.Exists("config.json")) {
                Console.WriteLine("Configuration file not found, creating.");

                Config.CreateDefaultConfig();
            }

            Config = JsonConvert.DeserializeObject<BotConfig>(File.ReadAllText("config.json"));
            Console.WriteLine("Configuration file loaded.");
        }

        /// <summary>
        ///     Initialises all data streams
        /// </summary>
        private bool InitializeNetworkStream(int maxRetries = 3) {
            int retries = 0;

            while (retries <= maxRetries)
                try {
                    connection = new TcpClient(Config.Server, Config.Port);
                    networkStream = connection.GetStream();
                    _in = new StreamReader(networkStream);
                    break;
                } catch (SocketException) {
                    logger.Log(IrcLogEntryType.Error, retries <= maxRetries
                        ? "Communication error, attempting to connect again..."
                        : "Communication could not be established with address.");

                    retries++;
                }

            return retries <= maxRetries;
        }

        private void InitailisePluginWrapper() {
            Wrapper.TerminateBotEvent += delegate { Dispose(); };
            Wrapper.LogEntryEventHandler += logger.LogEvent;
            Wrapper.SimpleMessageEventHandler += logger.SendDataEvent;
            Wrapper.Start();
        }

        private void InitializeDatabase() {
            try {
                MainDatabase = new Database();
                MainDatabase.LogEntryEventHandler += logger.LogEvent;
                MainDatabase.Initialise(Config.DatabaseLocation);
                MainDatabase.InitialiseUsersIntoList(Users);
            } catch (Exception ex) {
                logger.Log(IrcLogEntryType.Error, ex.ToString());

                CanExecute = false;
            }
        }

        /// <summary>
        ///     Register all methods
        /// </summary>
        private void RegisterMethods() {
            Wrapper.PluginHost.RegisterMethod(new PluginRegistrarEventArgs(Protocols.MOTD_REPLY_END, MotdReplyEnd));
            Wrapper.PluginHost.RegisterMethod(new PluginRegistrarEventArgs(Protocols.NICK, Nick));
            Wrapper.PluginHost.RegisterMethod(new PluginRegistrarEventArgs(Protocols.JOIN, Join));
            Wrapper.PluginHost.RegisterMethod(new PluginRegistrarEventArgs(Protocols.PART, Part));
            Wrapper.PluginHost.RegisterMethod(new PluginRegistrarEventArgs(Protocols.NAMES_REPLY, NamesReply));
            Wrapper.PluginHost.RegisterMethod(new PluginRegistrarEventArgs(Protocols.PRIVMSG, Privmsg));
        }

        #endregion


        #region Runtime

        /// <summary>
        ///     Recieves input from open stream
        /// </summary>
        private string ListenToStream() {
            string data = string.Empty;
            try {
                data = _in.ReadLine();
            } catch (NullReferenceException) {
                logger.Log(IrcLogEntryType.Error, "Stream disconnected. Attempting to reconnect...");

                InitializeNetworkStream();
            } catch (Exception ex) {
                logger.Log(IrcLogEntryType.Error, ex.ToString());
            }

            return data;
        }

        /// <summary>
        ///     Default method to execute bot functions
        /// </summary>
        public void ExecuteRuntime() {
            string rawData = ListenToStream();

            if (string.IsNullOrEmpty(rawData) ||
                CheckIfIsPing(rawData))
                return;

            ChannelMessage channelMessage = new ChannelMessage(this, rawData);

            if (channelMessage.Type.Equals(Protocols.ABORT))
                return;

            CheckAddChannel(channelMessage);
            CheckAddUser(channelMessage);

            // PRIVMSG messages are displayed differently to other message types
            string toLog = channelMessage.Type.Equals(Protocols.PRIVMSG)
                ? $"<{channelMessage.Recipient} {channelMessage.Nickname}> {channelMessage.Args}"
                : rawData;

            logger.Log(IrcLogEntryType.Message, toLog);

            if (channelMessage.Nickname.Equals(Config.Nickname) &&
                channelMessage.Realname.Equals(Config.Realname))
                return;

            Wrapper.PluginHost.InvokeMethods(channelMessage);
        }

        #endregion


        #region Checks, Creates, Adds

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
        public void AddMessage(User user, Message m) {
            if (!Users.Contains(user))
                return;

            MainDatabase.Query($"INSERT INTO messages VALUES ({user.Id}, '{m.Sender}', '{m.Contents}', '{m.Date}')");
            user.Messages.Add(m);
        }

        /// <summary>
        ///     Set new access level for user
        /// </summary>
        /// <param name="user">user object</param>
        /// <param name="access">new access level</param>
        public void SetAccess(User user, int access) {
            if (!Users.Contains(user))
                return;

            MainDatabase.Query($"UPDATE users SET access={access} WHERE realname='{user.Realname}'");
            user.Access = access;
        }

        /// <summary>
        ///     Returns a specified command from commands list
        /// </summary>
        /// <param name="command">Command to be returned</param>
        /// <returns></returns>
        public KeyValuePair<string, string> GetCommand(string command) {
            return Wrapper.PluginHost.GetCommands().SingleOrDefault(x => x.Key.Equals(command));
        }

        /// <summary>
        ///     Checks whether specified comamnd exists
        /// </summary>
        /// <param name="command">comamnd name to be checked</param>
        /// <returns>True: exists; false: does not exist</returns>
        public bool HasCommand(string command) {
            return Wrapper.PluginHost.GetCommands().Keys.Contains(command);
        }

        /// <summary>
        ///     Check whether the data recieved is a ping message and reply
        /// </summary>
        /// <param name="rawData"></param>
        /// <returns></returns>
        private bool CheckIfIsPing(string rawData) {
            if (!rawData.StartsWith(Protocols.PING))
                return false;

            logger.SendData(Protocols.PONG, rawData.Remove(0, 5)); // removes 'PING ' from string
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
            if (Users.Any(e => e.Realname.Equals(realname)))
                return;

            Users.Add(new User(access, nickname, realname, seen, id));

            logger.Log(IrcLogEntryType.System, $"Creating database entry for {realname}.");

            id = MainDatabase.GetLastDatabaseId() + 1;

            MainDatabase.Query($"INSERT INTO users VALUES ({id}, '{nickname}', '{realname}', {access}, '{seen}')");
        }

        public void CheckAddUser(ChannelMessage channelMessage) {
            User user = Users.SingleOrDefault(e => e.Realname.Equals(channelMessage.Realname));

            if (user == null)
                return;

            CreateUser(3, channelMessage.Nickname, channelMessage.Realname, channelMessage.Timestamp);
        }

        #endregion
 


        #region RegisterMethods

        private void MotdReplyEnd(object source, ChannelMessage channelMessage) {
            if (Config.Identified)
                return;

            logger.SendData(Protocols.PRIVMSG, $"NICKSERV IDENTIFY {Config.Password}");
            logger.SendData(Protocols.MODE, $"{Config.Nickname} +B");

            foreach (string channel in Config.Channels) {
                logger.SendData(Protocols.JOIN, channel);
                Channels.Add(new Channel(channel));
            }

            Config.Identified = true;
        }

        private void Nick(object source, ChannelMessage channelMessage) {
            MainDatabase.Query($"UPDATE users SET nickname='{channelMessage.Recipient}' WHERE realname='{channelMessage.Realname}'");
        }

        private void Join(object source, ChannelMessage channelMessage) {
            Channels.SingleOrDefault(e => !e.Name.Equals(channelMessage.Recipient))?.AddUser(channelMessage.Recipient);
        }

        private void Part(object source, ChannelMessage channelMessage) {
            Channels.SingleOrDefault(e => e.Name.Equals(channelMessage.Recipient))?.Inhabitants.RemoveAll(x => x.Equals(channelMessage.Nickname));
        }

        private void NamesReply(object source, ChannelMessage channelMessage) {
            string channelName = channelMessage.SplitArgs[1];

            // * SplitArgs [2] is always your nickname

            // in this case, Eve is the only one in the channel
            if (channelMessage.SplitArgs.Count < 4)
                return;

            foreach (string s in channelMessage.SplitArgs[3].Split(' ')) {
                Channel currentChannel = Channels.SingleOrDefault(e => e.Name.Equals(channelName));

                if (currentChannel == null ||
                    currentChannel.Inhabitants.Contains(s))
                    continue;

                Channels.Single(e => e.Name.Equals(channelName)).Inhabitants.Add(s);
            }
        }

        private void Privmsg(object source, ChannelMessage channelMessage) {
            if (IgnoreList.Contains(channelMessage.Realname))
                return;

            if (!channelMessage.SplitArgs[0].Replace(",", string.Empty).Equals(Config.Nickname.ToLower()))
                return;

            if (channelMessage.SplitArgs.Count < 2) {
                logger.SendData(Protocols.PRIVMSG, $"{channelMessage.Recipient} Type 'eve help' to view my command list.");
                return;
            }

            // built-in 'help' command
            if (channelMessage.SplitArgs[1].ToLower().Equals("help")) {
                if (channelMessage.SplitArgs.Count.Equals(2)) { // in this case, 'help' is the only text in the string.
                    logger.SendData(Protocols.PRIVMSG, $"{channelMessage.Recipient} Active commands: {string.Join(", ", Wrapper.PluginHost.GetCommands().Keys)}");
                    return;
                }

                KeyValuePair<string, string> queriedCommand = GetCommand(channelMessage.SplitArgs[2]);

                string valueToSend = queriedCommand.Equals(default(KeyValuePair<string, string>))
                    ? "Command not found."
                    : $"{queriedCommand.Key}: {queriedCommand.Value}";

                logger.SendData($"{channelMessage.Recipient} {valueToSend}");

                return;
            }

            if (HasCommand(channelMessage.SplitArgs[1].ToLower()))
                return;
            logger.SendData($"{channelMessage.Recipient} Invalid command. Type 'eve help' to view my command list.");
        }

        #endregion
    }
}