#region usings

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

#endregion

namespace Eve.Classes {
    public class BotConfig {
        private const string BASE_CONFIG =
            "{ \"Nickname\": \"TestyBot\", \"Realname\": \"SemiViral\", \"Password\": \"testypass\", \"Server\": \"irc.foonetic.net\", \"Port\": 6667, \"Channels\": [\"#testgrounds\"],  \"IgnoreList\": [], \"DatabaseLocation\": \"users.sqlite\" }";

        public List<string> IgnoreList { get; set; } = new List<string>();

        public bool Identified { get; set; }

        public string Server { get; private set; }
        public string[] Channels { get; private set; }
        public string Realname { get; private set; }
        public string Nickname { get; private set; }
        public string Password { get; private set; }
        public string DatabaseLocation { get; private set; }
        public string YouTubeAPIKey { get; private set; }

        public int Port { get; set; }

        private static void CreateDefaultConfig() {
            using (FileStream stream = new FileStream(@"config.json", FileMode.OpenOrCreate, FileAccess.ReadWrite)) {
                Writer.Log("Configuration file not found, creating.", IrcLogEntryType.System);

                StreamWriter writer = new StreamWriter(stream);
                writer.Write(BASE_CONFIG);
                writer.Flush();
            }
        }

        /// <summary>
        ///     Check that the default config file exists, then return an object of it
        /// </summary>
        /// <returns>
        ///     <see cref="BotConfig" />
        /// </returns>
        public static BotConfig GetDefaultConfig() {
            if (!File.Exists("config.json")) CreateDefaultConfig();

            JObject config = JObject.Parse(File.ReadAllText("config.json"));

            return new BotConfig {
                Nickname = (string)config.SelectToken(nameof(Nickname)),
                Realname = (string)config.SelectToken(nameof(Realname)),
                Password = (string)config.SelectToken(nameof(Password)),
                Port = (int)config.SelectToken(nameof(Port)),
                Server = (string)config.SelectToken(nameof(Server)),
                Channels = config.SelectToken(nameof(Channels))
                    .Select(e => e.ToString())
                    .ToArray(),
                DatabaseLocation = (string)config.SelectToken(nameof(DatabaseLocation)),
                YouTubeAPIKey = (string)config.SelectToken(nameof(YouTubeAPIKey))
            };
        }
    }
}