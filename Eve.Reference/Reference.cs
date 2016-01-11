using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Eve.Utilities;
using Newtonsoft.Json.Linq;

namespace Eve.Reference {
	public class Define : Utils, IModule {
		private readonly ChannelMessage _o = new ChannelMessage {
			Type = "PRIVMSG",
			Args = String.Empty
		};

		public Dictionary<String, String> Def => new Dictionary<string, string> {
			{ "define", "(<word> *<part of speech>) — returns definition for given word." }
		};

		public ChannelMessage OnChannelMessage(ChannelMessage c) {
			if (!c._Args[0].Replace(",", String.Empty).CaseEquals("eve")
				|| c._Args.Count < 2
				|| !c._Args[1].CaseEquals("define"))
				return null;

			_o.Nickname = c.Recipient;
			
			if (c._Args.Count < 3) {
				_o.Args = "Insufficient parameters. Type 'eve help lookup' to view correct usage.";
				return _o;
			}

			JObject entry = JObject.Parse(HttpGet($"https://api.pearson.com:443/v2/dictionaries/lasde/entries?headword={c._Args[2]}&limit=1&part_of_speech={(c._Args.Count > 3 ? c._Args[3] : null)}"));
			Dictionary<String, String> _out = new Dictionary<string, string>();

			if ((int)entry.SelectToken("count") < 1) {
				_o.Args = "Query returned no results.";
				return _o;
			}

			_out.Add("word", (string)entry.SelectToken("results[0].headword"));
			_out.Add("pos", (string)entry.SelectToken("results[0].part_of_speech"));
			_out.Add("def", (string)entry.SelectToken("results[0].senses[0].definition[0]"));
			_out.Add("ex", (string)entry.SelectToken("results[0].senses[0].examples[0].text"));

			string sOut = $"{_out["word"]} [{_out["pos"]}] — {_out["def"]}";
			if (String.IsNullOrEmpty(_out["ex"]))
				sOut += $" (ex. {_out["ex"]})";

			_o.Args = sOut;
			return _o;
		}
	}

	public class Lookup : Utils, IModule {
		private readonly ChannelMessage _o = new ChannelMessage {
			Type = "PRIVMSG",
			Args = String.Empty
		};

		public Dictionary<String, String> Def => new Dictionary<string, string> {
			{ "lookup", "(<term/phrase>) — returns the wikipedia summary of given term or phrase." }
		};

		public ChannelMessage OnChannelMessage(ChannelMessage c) {
			if (!c._Args[0].Replace(",", String.Empty).CaseEquals("eve")
				|| c._Args.Count < 2
				|| !c._Args[1].CaseEquals("lookup"))
				return null;

			_o.Nickname = c.Nickname;
			string query = c._Args.Count < 4 ? c._Args[2] : $"{c._Args[2]}%20{c._Args[3]}".Replace(" ", "%20"),
				response = HttpGet("https://en.wikipedia.org/w/api.php?format=json&action=query&prop=extracts&exintro=&explaintext=&titles=" +
						query);

			if (c._Args.Count < 3) {
				_o.Args = "Insufficient parameters. Type 'eve help lookup' to view correct usage.";
				return _o;
			}

			JToken pages = JObject.Parse(response)["query"]["pages"].Values().First();
			if (String.IsNullOrEmpty((string)pages["extract"])) {
				_o.Args = "Query failed to return results. Perhaps try a different term?";
				return _o;
			}

			_o._Args = new List<string>() { $"\x02{(string)pages["title"]}\x0F — " };
			_o._Args.AddRange(SplitStr(Regex.Replace((string)pages["extract"], @"\n\n?|\n", " "), 440));
			
			return _o;
		}
	}
}
