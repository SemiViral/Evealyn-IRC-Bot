using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace Eve.Utilities
{
    public class Utils
    {
		public static string HttpGET(string url) {
			var request = (HttpWebRequest)WebRequest.Create(url);
			request.Method = "GET";

			using (var httpr = (HttpWebResponse)request.GetResponse()) {
				return new StreamReader(httpr.GetResponseStream()).ReadToEnd();
			}
		}

		public static IEnumerable<string> SplitStr(string str, int maxLength) {
			for (int i = 0; i < str.Length; i += maxLength)
				yield return str.Substring(i, Math.Min(maxLength, str.Length - i));
		}

		public static bool GetUserTimeout(string who) {
			Variables v = Eve.IRCBot.v;
			bool doTimeout = false;

			if (v.QueryName(who) == null)
				return false;

			if (v.userAttempts[who] == 4)
				// Check if user's last message happened more than 1 minute ago
				if (v.QueryName(who).Seen.AddMinutes(1) < DateTime.UtcNow)
					v.userAttempts[who] = 0; // if so, reset their attempts to 0
				else doTimeout = true; // if not, timeout is true
			else if (v.QueryName(who).Access > 1)
				// if user isn't admin/op, increment their attempts
				v.userAttempts[who]++;

			Eve.IRCBot.v = v;
			return doTimeout;
		}
	}
}
