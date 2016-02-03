using System;
using System.ComponentModel;
using Eve.Types;

namespace Eve {
	internal class Program {
		private static IrcBot _bot;
		private static IrcConfig _config;

		public static bool ShouldRun { get; set; } = true;

		private static void ParseAndDo(object sender, DoWorkEventArgs e) {
			while (ShouldRun)
				_bot.Runtime();
		}

		private static void Main() {
			string input = string.Empty;

			_config = IrcConfig.GetDefaultConfig();
			Console.WriteLine("||| Configuration file loaded.");

			using (_bot = new IrcBot(_config)) {
				BackgroundWorker backgroundDataParser = new BackgroundWorker();
				backgroundDataParser.DoWork += ParseAndDo;
				backgroundDataParser.RunWorkerAsync();

				do {
					input = Console.ReadLine();
				} while (!input.CaseEquals("exit"));
			}

			Console.WriteLine("||| Bot has shutdown.");
			Console.ReadLine();
		}
	}

	public static class Extentions {
		/// <summary>
		///     Compares the object to a string with default ignorance of casing
		/// </summary>
		/// <param name="obj">inherent object</param>
		/// <param name="query">string to compare</param>
		/// <param name="ignoreCase">whether or not to ignore case</param>
		/// <returns>true: strings equal; false: strings unequal</returns>
		public static bool CaseEquals(this string obj, string query, bool ignoreCase = true) {
			return obj.Equals(query, ignoreCase ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture);
		}
	}
}