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
	}
}
