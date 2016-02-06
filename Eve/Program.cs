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
			Utils.Output("Configuration file loaded.");

			using (_bot = new IrcBot(_config)) {
				while (ShouldRun) _bot.Runtime();

				//	BackgroundWorker backgroundDataParser = new BackgroundWorker();
				//	backgroundDataParser.DoWork += ParseAndDo;
				//	backgroundDataParser.RunWorkerAsync();

				//	do {
				//		input = Console.ReadLine();
				//	} while (!input.CaseEquals("exit"));
			}

			Utils.Output("Bot has shutdown.");
			Console.ReadLine();
		}
	}
}