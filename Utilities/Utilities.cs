using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Eve.Managers.Classes;

namespace Eve.Utilities {
	public class Utils {
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

		public static bool GetUserTimeout(string who, Variables v) {
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
		/// Checks if a channel exists within V.Channels
		/// </summary>
		/// <param name="channel">channel to be checked against list</param>
		/// <returns>true: channel is in list; false: channel is not in list</returns>
		public static bool CheckChannelExists(string channel, Variables v) {
			return v.Channels.Any(e => e.Name == channel);
		}

		public static void AddUserToChannel(string channel, string realname, Variables v) {
			if (!CheckChannelExists(channel, v)) return;

			v.Channels.FirstOrDefault(e => e.Name == channel)?.UserList.Add(realname);
		}

		public static void RemoveUserFromChannel(string channel, string realname, Variables v) {
			if (!CheckChannelExists(channel, v)) return;

			Channel firstOrDefault = v.Channels.FirstOrDefault(e => e.Name == channel);
			if (firstOrDefault != null && !firstOrDefault.UserList.Contains(realname)) {
				Console.WriteLine($"||| '{realname}' does not exist in that channel.");
				return;
			}

			v.Channels.FirstOrDefault(e => e.Name == channel)?.UserList.Remove(realname);
		}
	}
}