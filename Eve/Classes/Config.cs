using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Eve.Classes {
	public class BotConfig {
		public List<string> IgnoreList { get; set; } = new List<string>();

		public bool Identified { get; set; }

		public string Server { get; set; }
		public string[] Channels { get; set; }
		public string Realname { get; set; }
		public string Nickname { get; set; }
		public string Password { get; set; }
		public string Database { get; set; }

		public int Port { get; set; }

		/// <summary>
		///     Check that the default config file exists, then return an object of it
		/// </summary>
		/// <returns>
		///     <see cref="BotConfig" />
		/// </returns>
		public static BotConfig GetDefaultConfig() {
			const string baseConfig =
				"{ \"Nickname\": \"TestyBot\", \"Realname\": \"SemiViral\", \"Password\": \"testypass\", \"Server\": \"irc.foonetic.net\", \"Port\": 6667, \"Channels\": [\"#testgrounds\"],  \"IgnoreList\": [], \"DatabaseLocation\": \"users.sqlite\" }";

			if (!File.Exists("config.json")) {
				using (FileStream stream = new FileStream(@"config.json", FileMode.OpenOrCreate, FileAccess.ReadWrite)) {
					Writer.Log("Configuration file not found, creating.", EventLogEntryType.Information);

					StreamWriter writer = new StreamWriter(stream);
					writer.Write(baseConfig);
					writer.Flush();
				}
			}

			JObject config = JObject.Parse(File.ReadAllText("config.json"));

			return new BotConfig {
				Nickname = (string)config.SelectToken("Nickname"),
				Realname = (string)config.SelectToken("Realname"),
				Password = (string)config.SelectToken("Password"),
				Port = (int)config.SelectToken("Port"),
				Server = (string)config.SelectToken("Server"),
				Channels = config.SelectToken("Channels")
					.Select(e => e.ToString())
					.ToArray(),
				Database = (string)config.SelectToken("DatabaseLocation")
			};
		}
	}
}