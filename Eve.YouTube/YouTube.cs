using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Eve.Utilities;
using Newtonsoft.Json.Linq;


namespace Eve.YouTube {
	public class YouTube : Utils, IModule {
		public Dictionary<String, String> def {
			get {
				return new Dictionary<string, string> {
					{ "youtube", "returns video information for any given YouTube link in messages." }
				};
			}
		}

		public ChannelMessage OnChannelMessage(ChannelMessage c) {
			var youtubeRegex = new Regex(@"http(?:s?)://(?:www\.)?youtu(?:be\.com/watch\?v=|\.be/)(?<ID>[\w\-]+)(&(amp;)?[\w\?=‌​]*)?", RegexOptions.Compiled);
			ChannelMessage o = new ChannelMessage {
				Type = "PRIVMSG",
				Nickname = c.Recipient,
				Args = null
			};

			if (youtubeRegex.IsMatch(c.Args)) {
				string get = HttpGET($"https://www.googleapis.com/youtube/v3/videos?part=snippet&id={youtubeRegex.Match(c.Args).Groups["ID"]}&key=AIzaSyDnKtEZGuv3PgmePOSe6xBvoXKbrEMVxx8");

				JToken video = JObject.Parse(get)["items"][0]["snippet"];
				string channel = (string)video["channelTitle"];
				string title = (string)video["title"];
				string description = video["description"].ToString().Split('\n')[0];
				string[] descArray = description.Split(' ');

				if (description.Length > 200) {
					description = "";

					for (int i = 0; description.Length < 200; i++)
						description += $" {descArray[i]}";

					description += "....";
				}

				o.Args = $"{title} (by {channel}) — {description}";
				return o; 
			}

			return null;
		}
	}
}
