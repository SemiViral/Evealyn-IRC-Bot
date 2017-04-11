#region usings

using System.Collections.Generic;
using System.IO;

#endregion

namespace Eve.Classes {
    public class BotConfig {
        public const string BASE_CONFIG = "{ \"Nickname\": \"TestyBot\", \"Realname\": \"SemiViral\", \"Password\": \"testypass\", \"Server\": \"irc.foonetic.net\", \"Port\": 6667, \"Channels\": [\"#testgrounds\"],  \"IgnoreList\": [], \"DatabaseLocation\": \"users.sqlite\", \"ApiKeys\": [{\"Youtube\": \"\", \"Dictionary\": \"\"}]}";

        public List<string> IgnoreList { get; set; } = new List<string>();
        public Dictionary<string, string> ApiKeys { get; set; } = new Dictionary<string, string>();

        public bool Identified { get; set; }

        public string Server { get; set; }
        public string[] Channels { get; set; }
        public string Realname { get; set; }
        public string Nickname { get; set; }
        public string Password { get; set; }
        public string DatabaseLocation { get; set; }
        public int Port { get; set; }

        public void CreateDefaultConfig() {
            using (FileStream stream = new FileStream(@"config.json", FileMode.OpenOrCreate, FileAccess.ReadWrite)) {
                StreamWriter writer = new StreamWriter(stream);
                writer.Write(BASE_CONFIG);
                writer.Flush();
            }
        }
    }
}