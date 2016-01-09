using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Eve.Utilities;
using Newtonsoft.Json.Linq;

namespace Eve.Reference {
	public class Define : Utils, IModule {
		private ChannelMessage o = new ChannelMessage {
			Type = "PRIVMSG",
			Args = null
		};

		public Dictionary<String, String> def {
			get {
				return new Dictionary<string, string> {
					{ "define", "(<word> *<part of speech>) — returns definition for given word." }
				};
			}
		}

		public ChannelMessage OnChannelMessage(ChannelMessage c) {
			if (c._Args[0].Replace(",", string.Empty) != "eve"
				|| c._Args.Count < 2
				|| c._Args[1] != "define")
				return null;

			o.Nickname = c.Recipient;
			string pos = c._Args.Count < 4 ? null : c._Args[3],
				url;
			
			if (c._Args.Count < 3) {
				o.Args = "Insufficient parameters. Type 'eve help lookup' to view correct usage.";
				return o;
			}

			url = $"https://api.pearson.com:443/v2/dictionaries/lasde/entries?headword={c._Args[2]}&limit=1";

			if (!String.IsNullOrEmpty(pos))
				url += "&part_of_speech={pos}";

			JObject entry = JObject.Parse(HttpGET(url));
			var _out = new Dictionary<string, string>();

			if ((int)entry.SelectToken("count") < 1) {
				o.Args = "Query returned no results.";
				return o;
			}

			_out.Add("word", (string)entry.SelectToken("results[0].headword"));
			_out.Add("pos", (string)entry.SelectToken("results[0].part_of_speech"));
			_out.Add("def", (string)entry.SelectToken("results[0].senses[0].definition[0]"));
			_out.Add("ex", (string)entry.SelectToken("results[0].senses[0].examples[0].text"));

			string sOut = $"{_out["word"]} [{_out["pos"]}] — {_out["def"]}";
			if (String.IsNullOrEmpty(_out["ex"]))
				sOut += $" (ex. {_out["ex"]})";

			o.Args = sOut;
			return o;
		}
	}

	public class Lookup : Utils, IModule {
		private ChannelMessage o = new ChannelMessage {
			Type = "PRIVMSG",
			Args = null
		};

		public Dictionary<String, String> def {
			get {
				return new Dictionary<string, string> {
					{ "lookup", "(<term/phrase>) — returns the wikipedia summary of given term or phrase." }
				};
			}
		}

		public ChannelMessage OnChannelMessage(ChannelMessage c) {
			if (c._Args[0].Replace(",", string.Empty) != "eve"
				|| c._Args.Count < 2
				|| c._Args[1] != "lookup")
				return null;

			o.Nickname = c.Nickname;
			string query = c._Args.Count < 4 ? c._Args[2] : $"{c._Args[2]}%20{c._Args[3]}".Replace(" ", "%20"),
				response = HttpGET("https://en.wikipedia.org/w/api.php?format=json&action=query&prop=extracts&exintro=&explaintext=&titles=" +
						query);

			if (c._Args.Count < 3) {
				o.Args = "Insufficient parameters. Type 'eve help lookup' to view correct usage.";
				return o;
			}

			JToken pages = JObject.Parse(response)["query"]["pages"].Values().First();
			if (String.IsNullOrEmpty((string)pages["extract"])) {
				o.Args = "Query failed to return results. Perhaps try a different term?";
				return o;
			}

			o._Args = new List<string>() { $"\x02{(string)pages["title"]}\x0F — " };
			o._Args.AddRange(SplitStr(Regex.Replace((string)pages["extract"], @"\n\n?|\n", " "), 440));

			o._Args.ForEach(Console.WriteLine);
			return o;
		}
	}
}
