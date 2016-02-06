using System.Collections.Generic;
using System.Text.RegularExpressions;
using Eve.Types;
using Newtonsoft.Json.Linq;

namespace Eve.Core {
	public class YouTube : Utils, IModule {
		public Dictionary<string, string> Def => new Dictionary<string, string> {
			["youtube"] = "outputs video information for any given YouTube link in messages."
		};

		public ChannelMessage OnChannelMessage(ChannelMessage c, PassableMutableObject v) {
			Regex youtubeRegex =
				new Regex(@"(?i)http(?:s?)://(?:www\.)?youtu(?:be\.com/watch\?v=|\.be/)(?<ID>[\w\-]+)(&(amp;)?[\w\?=‌​]*)?",
					RegexOptions.Compiled);

			if (!youtubeRegex.IsMatch(c.Args)) return null;

			string get =
				HttpGet(
					$"https://www.googleapis.com/youtube/v3/videos?part=snippet&id={youtubeRegex.Match(c.Args).Groups["ID"]}&key=AIzaSyDnKtEZGuv3PgmePOSe6xBvoXKbrEMVxx8");

			JToken video = JObject.Parse(get)["items"][0]["snippet"];
			string channel = (string) video["channelTitle"];
			string title = (string) video["title"];
			string description = video["description"].ToString().Split('\n')[0];
			string[] descArray = description.Split(' ');

			if (description.Length > 200) {
				description = "";

				for (int i = 0; description.Length < 200; i++)
					description += $" {descArray[i]}";

				description += "....";
			}

			c.Message = $"{title} (by {channel}) — {description}";
			return c;
		}
	}
}