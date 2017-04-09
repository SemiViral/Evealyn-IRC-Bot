//using System.Collections.Generic;
//using System.Linq;
//using System.Text.RegularExpressions;
//using Eve.ModuleControl;
//using Eve.Types;
//using Newtonsoft.Json.Linq;

//namespace Eve.Core {
//	public class Define : Utils, IPlugin {
//		public Dictionary<string, string> Def => new Dictionary<string, string> {
//			["define"] = "(<word> *<part of speech>) — returns definition for given word."
//		};

//		public ChannelMessage OnChannelMessage(ChannelMessage c, PassableMutableObject v) {
//			if (!c.SplitArgs[1].CaseEquals(Def.Keys.First()))
//				return null;

//			if (c.SplitArgs.Count < 3) {
//				c.Message = "Insufficient parameters. Type 'eve help lookup' to view correct usage.";
//				return c;
//			}

//			JObject entry =
//				JObject.Parse(
//					HttpGet(
//						$"https://api.pearson.com:443/v2/dictionaries/lasde/entries?headword={c.SplitArgs[2]}&limit=1&part_of_speech={(c.SplitArgs.Count > 3 ? c.SplitArgs[3] : null)}"));
//			var _out = new Dictionary<string, string>();

//			if ((int) entry.SelectToken("count") < 1) {
//				c.Message = "Query returned no results.";
//				return c;
//			}

//			_out.Add("word", (string) entry.SelectToken("results[0].headword"));
//			_out.Add("pos", (string) entry.SelectToken("results[0].part_of_speech"));
//			_out.Add("def", (string) entry.SelectToken("results[0].senses[0].definition[0]"));
//			_out.Add("ex", (string) entry.SelectToken("results[0].senses[0].examples[0].text"));

//			string sOut = $"{_out["word"]} [{_out["pos"]}] — {_out["def"]}";
//			if (string.IsNullOrEmpty(_out["ex"]))
//				sOut += $" (ex. {_out["ex"]})";

//			c.Message = sOut;
//			return c;
//		}
//	}

//	public class Lookup : Utils, IPlugin {
//		public Dictionary<string, string> Def => new Dictionary<string, string> {
//			["lookup"] = "(<term/phrase>) — returns the wikipedia summary of given term or phrase."
//		};

//		public ChannelMessage OnChannelMessage(ChannelMessage c, PassableMutableObject v) {
//			if (!c.SplitArgs[1].CaseEquals(Def.Keys.First()))
//				return null;

//			c.Target = c.Nickname;
//			string query = c.SplitArgs.Count < 4 ? c.SplitArgs[2] : $"{c.SplitArgs[2]}%20{c.SplitArgs[3]}".Replace(" ", "%20"),
//				response =
//					HttpGet("https://en.wikipedia.org/w/api.php?format=json&action=query&prop=extracts&exintro=&explaintext=&titles=" +
//							query);

//			if (c.SplitArgs.Count < 3) {
//				c.Message = "Insufficient parameters. Type 'eve help lookup' to view correct usage.";
//				return c;
//			}

//			JToken pages = JObject.Parse(response)["query"]["pages"].Values().First();
//			if (string.IsNullOrEmpty((string) pages["extract"])) {
//				c.Message = "Query failed to return results. Perhaps try a different term?";
//				return c;
//			}

//			c.MultiMessage = new List<string> {
//				$"\x02{(string) pages["title"]}\x0F — "
//			};
//			c.MultiMessage.AddRange(SplitStr(Regex.Replace((string) pages["extract"], @"\n\n?|\n", " "), 440));

//			return c;
//		}
//	}
//}