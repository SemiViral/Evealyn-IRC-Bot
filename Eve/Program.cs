using System;
using System.ComponentModel;
using System.Diagnostics;
using Eve.Types;

namespace Eve {
	internal class Program {
		private static IrcBot _bot;
		private static IrcConfig _config;

		private static void ParseAndDo(object sender, DoWorkEventArgs e) {
			while (_bot.CanExecute) {
				_bot.ExecuteRuntime();
			}
		}

		private static void Main() {
			string input = string.Empty;

			_config = IrcConfig.GetDefaultConfig();
			Writer.Log("Configuration file loaded.", EventLogEntryType.Information);

			using (_bot = new IrcBot(_config)) {
				#if DEBUG
					while (_bot.CanExecute) {
						_bot.ExecuteRuntime();
					}
				#else
					BackgroundWorker backgroundDataParser = new BackgroundWorker();
					backgroundDataParser.DoWork += ParseAndDo;
					backgroundDataParser.RunWorkerAsync();

					do {
						input = Console.ReadLine();
					} while (!input.CaseEquals("exit"));
				#endif
			}

			Writer.Log("Bot has shutdown. Press any key to exit program.", EventLogEntryType.Information);
			Console.ReadLine();
		}
	}
}