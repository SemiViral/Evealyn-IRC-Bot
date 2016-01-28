using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Eve.Types.Classes;
using Newtonsoft.Json.Linq;

namespace Eve {
	public class Utilities {
		public static string HttpGet(string url) {
			HttpWebRequest request = (HttpWebRequest) WebRequest.Create(url);
			request.Method = "GET";

			using (HttpWebResponse httpr = (HttpWebResponse) request.GetResponse())
				return new StreamReader(httpr.GetResponseStream()).ReadToEnd();
		}

		public static IEnumerable<string> SplitStr(string str, int maxLength) {
			for (int i = 0; i < str.Length; i += maxLength)
				yield return str.Substring(i, Math.Min(maxLength, str.Length - i));
		}

		public static bool GetUserTimeout(string who, PropertyReference v) {
			bool doTimeout = false;

			if (v.QueryName(who) == null)
				return false;

			if (v.CurrentUser.Attempts == 4)
				// Check if user's last message happened more than 1 minute ago
				if (v.QueryName(who).Seen.AddMinutes(1) < DateTime.UtcNow)
					v.CurrentUser.Attempts = 0; // if so, reset their attempts to 0
				else doTimeout = true; // if not, timeout is true
			else if (v.QueryName(who).Access > 1)
				// if user isn't admin/op, increment their attempts
				v.CurrentUser.Attempts++;

			return doTimeout;
		}
	
		/// <summary>
		///     Checks if a channel exists within V.Channels
		/// </summary>
		/// <param name="channel">channel to be checked against list</param>
		/// <returns>true: channel is in list; false: channel is not in list</returns>
		public static bool CheckChannelExists(string channel, PropertyReference v) {
			return v.Channels.Any(e => e.Name == channel);
		}

		/// <summary>
		///     Check that the default config file exists, then return an object of it
		/// </summary>
		/// <returns>
		///     <see cref="IrcConfig" />
		/// </returns>
		public static IrcConfig CheckConfigExistsAndReturn() {
			const string baseConfig =
				"{ \"Nickname\": \"Eve\", \"Realname\": \"SemiViral\", \"Password\": \"evepass\", \"Server\": \"irc.foonetic.net\", \"Port\": 6667, \"Channels\": [\"#testgrounds\"],  \"IgnoreList\": [], \"Database\": \"users.sqlite\" }";

			if (!File.Exists("config.json"))
				using (FileStream stream = new FileStream(@"config.json", FileMode.OpenOrCreate, FileAccess.ReadWrite)) {
					Console.WriteLine("||| Configuration file not found, creating.");

					StreamWriter writer = new StreamWriter(stream);
					writer.Write(baseConfig);
					writer.Flush();
				}

			JObject config = JObject.Parse(File.ReadAllText("config.json"));

			return new IrcConfig {
				Nickname = (string)config.SelectToken("Nickname"),
				Realname = (string)config.SelectToken("Realname"),
				Password = (string)config.SelectToken("Password"),
				Port = (int)config.SelectToken("Port"),
				Server = (string)config.SelectToken("Server"),
				Channels = config.SelectToken("Channels")
					.Select(e => e.ToString())
					.ToArray(),
				Database = (string)config.SelectToken("Database"),
				Identified = false,
				Joined = false
			};
		}
	}

	public static class Extentions {
		/// <summary>
		///     Compares the object to a string with default ignorance of casing
		/// </summary>
		/// <param name="query">string to compare</param>
		/// <param name="ignoreCase">whether or not to ignore case</param>
		/// <returns>true: strings equal; false: strings unequal</returns>
		public static bool CaseEquals(this string obj, string query, bool ignoreCase = true) {
			return obj.Equals(query, ignoreCase ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture);
		}
	}
}