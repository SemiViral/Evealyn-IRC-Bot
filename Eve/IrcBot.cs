#region usings

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using Eve.ComponentModel;
using Eve.Plugin;
using Eve.Types;
using Eve.Types.Irc;
using Eve.Types.References;
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

            users = new ObservableCollection<User>();
            users.CollectionChanged += UserAdded;

            channels = new List<Channel>();
            Wrapper = new PluginWrapper();

            // check if connection is established, don't execute if not
            if (!(CanExecute = InitializeNetworkStream()))
                return;

            logger = new Writer(networkStream);

            InitializeDatabase();
            InitailisePluginWrapper();

            logger.SendData(Protocols.USER, $"{Config.Nickname} 0 * {Config.Realname}");
            logger.SendData(Protocols.NICK, Config.Nickname);

            RegisterMethods();

            Initialised = true;
        }

        #region non-critical variables

        private Database MainDatabase { get; set; }
        private PluginWrapper Wrapper { get; }

        private readonly ObservableCollection<User> users;
        private readonly List<Channel> channels;

        internal bool Initialised { get; }

        public static string Info => "Evealyn is an IRC bot created by SemiViral as a primary learning project for C#. Version 4.1.2";

        public List<string> IgnoreList { get; internal set; } = new List<string>();
        public List<string> Inhabitants => channels.SelectMany(e => e.Inhabitants).ToList();

        internal bool CanExecute { get; private set; }

        public string GetApiKey(string type) => Config.ApiKeys[type];

        #endregion

        #region disposing

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

        #region initializations

        private void InitialiseConfig() {
            Config = new BotConfig();

            if (!File.Exists("config.json")) {
                Console.WriteLine("Configuration file not found, creating.");

                Config.CreateDefaultConfig();
            }

            Config = JsonConvert.DeserializeObject<BotConfig>(File.ReadAllText("config.json"));
            Console.WriteLine("Configuration file loaded.\n");
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
                } catch (Exception) {
                    Console.WriteLine(retries <= maxRetries
                        ? "Communication error, attempting to connect again..."
                        : "Communication could not be established with address.\n");

                    retries++;
                }

            return retries <= maxRetries;
        }

        private void InitializeDatabase() {
            try {
                MainDatabase = new Database();
                MainDatabase.LogEntryEventHandler += logger.LogEvent;
                MainDatabase.Initialise(Config.DatabaseLocation);
                MainDatabase.InitialiseUsersIntoList(users);
            } catch (Exception ex) {
                logger.Log(IrcLogEntryType.Error, ex.ToString());

                CanExecute = false;
            }
        }

        private void InitailisePluginWrapper() {
            Wrapper.TerminateBotEvent += delegate { Dispose(); };
            Wrapper.LogEntryEventHandler += logger.LogEvent;
            Wrapper.SimpleMessageEventHandler += logger.SendDataEvent;
            Wrapper.Start();
        }

        /// <summary>
        ///     Register all methods
        /// </summary>
        private void RegisterMethods() {
            Wrapper.PluginHost.RegisterMethod(new PluginRegistrar(Protocols.MOTD_REPLY_END, MotdReplyEnd));
            Wrapper.PluginHost.RegisterMethod(new PluginRegistrar(Protocols.NICK, Nick));
            Wrapper.PluginHost.RegisterMethod(new PluginRegistrar(Protocols.JOIN, Join));
            Wrapper.PluginHost.RegisterMethod(new PluginRegistrar(Protocols.PART, Part));
            Wrapper.PluginHost.RegisterMethod(new PluginRegistrar(Protocols.NAMES_REPLY, NamesReply));
            Wrapper.PluginHost.RegisterMethod(new PluginRegistrar(Protocols.PRIVMSG, Privmsg));
        }

        #endregion

        #region runtime

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

                InitializeNetworkStream();
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

            if (string.IsNullOrEmpty(channelMessage.Type) ||
                channelMessage.Type.Equals(Protocols.ABORT))
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

            try {
                Wrapper.PluginHost.InvokeMethods(channelMessage);
            } catch (Exception ex) {
                logger.Log(IrcLogEntryType.Error, ex.ToString());
            }
        }

        #endregion

        #region general methods

        public int RemoveChannels(string channelName) {
            int count = 0;

            foreach (Channel channel in channels) {
                if (!channel.Name.Equals(channelName))
                    continue;

                channels.Remove(channel);
                count++;
            }

            return count;
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

        #endregion

        #region channel methods

        /// <summary>
        ///     Check if message's channel origin should be added to channel list
        /// </summary>
        /// <param name="channelMessage"></param>
        private void CheckAddChannel(ChannelMessage channelMessage) {
            if (channelMessage.Recipient.StartsWith("#") &&
                !channels.Any(e => e.Name.Equals(channelMessage.Recipient)))
                channels.Add(new Channel(channelMessage.Recipient));
        }

        public bool ChannelExists(string channelName) => channels.Any(channel => channel.Name.Equals(channelName));

        public List<string> GetAllChannels() => channels.Select(channel => channel.Name).ToList();

        #endregion

        #region user methods

        protected virtual void UserAdded(object source, NotifyCollectionChangedEventArgs e) {
            if (!e.Action.Equals(NotifyCollectionChangedAction.Add))
                return;

            foreach (object item in e.NewItems) {
                if (!(item is User))
                    continue;

                ((User)item).PropertyChanged += AutoUpdateUsers;
            }
        }

        private void AutoUpdateUsers(object source, PropertyChangedEventArgs e) {
            if (!(e is SpecialPropertyChangedEventArgs))
                return;

            SpecialPropertyChangedEventArgs castedArgs = (SpecialPropertyChangedEventArgs)e;

            MainDatabase.Query($"UPDATE users SET {castedArgs.PropertyName}='{castedArgs.NewValue}' WHERE realname='{castedArgs.Name}'");
        }

        public void CheckAddUser(ChannelMessage channelMessage) {
            User user = users.SingleOrDefault(e => e.Realname.Equals(channelMessage.Realname));

            if (user == null)
                return;

            CreateUser(3, channelMessage.Nickname, channelMessage.Realname, channelMessage.Timestamp);
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
            if (users.Any(e => e.Realname.Equals(realname)))
                return;

            users.Add(new User(access, nickname, realname, seen, id));

            logger.Log(IrcLogEntryType.System, $"Creating database entry for {realname}.");

            id = MainDatabase.GetLastDatabaseId() + 1;

            MainDatabase.Query($"INSERT INTO users VALUES ({id}, '{nickname}', '{realname}', {access}, '{seen}')");
        }

        public List<string> GetAllUsernames() => users.Select(user => user.Realname).ToList();
        public User GetUser(string userName) => users.SingleOrDefault(user => user.Realname.Equals(userName));
        public bool UserExists(string userName) => users.Any(user => user.Realname.Equals(userName));

        /// <summary>
        ///     Adds a Args object to list
        /// </summary>
        /// <param name="user">user object</param>
        /// <param name="m"><see cref="Message" /> to be added</param>
        public void AddMessage(User user, Message m) {
            if (!users.Contains(user))
                return;

            MainDatabase.Query($"INSERT INTO messages VALUES ({user.Id}, '{m.Sender}', '{m.Contents}', '{m.Date}')");
            user.Messages.Add(m);
        }

        #endregion

        #region register methods

        private void MotdReplyEnd(object source, ChannelMessage channelMessage) {
            if (Config.Identified)
                return;

            logger.SendData(Protocols.PRIVMSG, $"NICKSERV IDENTIFY {Config.Password}");
            logger.SendData(Protocols.MODE, $"{Config.Nickname} +B");

            foreach (string channel in Config.Channels) {
                logger.SendData(Protocols.JOIN, channel);
                channels.Add(new Channel(channel));
            }

            Config.Identified = true;
        }

        private void Nick(object source, ChannelMessage channelMessage) {
            MainDatabase.Query($"UPDATE users SET nickname='{channelMessage.Recipient}' WHERE realname='{channelMessage.Realname}'");
        }

        private void Join(object source, ChannelMessage channelMessage) {
            channels.SingleOrDefault(e => !e.Name.Equals(channelMessage.Recipient))?.AddUser(channelMessage.Recipient);
        }

        private void Part(object source, ChannelMessage channelMessage) {
            channels.SingleOrDefault(e => e.Name.Equals(channelMessage.Recipient))?.Inhabitants.RemoveAll(x => x.Equals(channelMessage.Nickname));
        }

        private void NamesReply(object source, ChannelMessage channelMessage) {
            string channelName = channelMessage.SplitArgs[1];

            // * SplitArgs [2] is always your nickname

            // in this case, Eve is the only one in the channel
            if (channelMessage.SplitArgs.Count < 4)
                return;

            foreach (string s in channelMessage.SplitArgs[3].Split(' ')) {
                Channel currentChannel = channels.SingleOrDefault(e => e.Name.Equals(channelName));

                if (currentChannel == null ||
                    currentChannel.Inhabitants.Contains(s))
                    continue;

                channels.Single(e => e.Name.Equals(channelName)).Inhabitants.Add(s);
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
                    logger.SendData(Protocols.PRIVMSG, Wrapper.PluginHost.GetCommands().Count == 0
                        ? $"{channelMessage.Recipient} No commands currently active."
                        : $"{channelMessage.Recipient} Active commands: {string.Join(", ", Wrapper.PluginHost.GetCommands().Keys)}");
                    return;
                }

                KeyValuePair<string, string> queriedCommand = GetCommand(channelMessage.SplitArgs[2]);

                string valueToSend = queriedCommand.Equals(default(KeyValuePair<string, string>))
                    ? "Command not found."
                    : $"{queriedCommand.Key}: {queriedCommand.Value}";

                logger.SendData(Protocols.PRIVMSG, $"{channelMessage.Recipient} {valueToSend}");

                return;
            }

            if (HasCommand(channelMessage.SplitArgs[1].ToLower()))
                return;
            logger.SendData($"{channelMessage.Recipient} Invalid command. Type 'eve help' to view my command list.");
        }

        #endregion
    }
}