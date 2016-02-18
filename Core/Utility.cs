using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace Eve.Core {
	public static class Utility {
		/// <summary>
		///     Obtain HTTP response from a GET request
		/// </summary>
		/// <param name="url">url to request</param>
		/// <returns>GET response</returns>
		public static string HttpGet(string url) {
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
			request.Method = "GET";

			using (HttpWebResponse httpr = (HttpWebResponse)request.GetResponse())
				return new StreamReader(httpr.GetResponseStream()).ReadToEnd();
		}

		/// <summary>
		///     Splits a string into seperate parts
		/// </summary>
		/// <param name="str">string to be split</param>
		/// <param name="maxLength">max length of individual strings to split</param>
		/// <returns>an enumerable object of strings</returns>
		public static IEnumerable<string> SplitStr(string str, int maxLength) {
			for (int i = 0; i < str.Length; i += maxLength) {
				yield return str.Substring(i, Math.Min(maxLength, str.Length - i));
			}
		}
	}
}