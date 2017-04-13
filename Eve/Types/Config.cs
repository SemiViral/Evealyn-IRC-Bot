#region usings

using System.Collections.Generic;
using System.IO;

#endregion

namespace Eve.Types {
    public class BotConfig {
        public const string BASE_CONFIG = "{\r\n  \"Nickname\": \"Eve\",\r\n  \"Realname\": \"Evealyn\",\r\n  \"Password\": \"evepass\",\r\n  \"Server\": \"irc.foonetic.net\",\r\n  \"Port\": 6667,\r\n  \"Channels\": [ \"#testgrounds\" ],\r\n  \"IgnoreList\": [],\r\n  \"DatabaseLocation\": \"users.sqlite\",\r\n  \"ApiKeys\": {\r\n    \"YouTube\": \"\",\r\n    \"Dictionary\": \"\"\r\n  }\r\n}";

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