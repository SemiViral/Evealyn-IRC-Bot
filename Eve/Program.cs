using System;
using System.ComponentModel;
using Eve.Types;

namespace Eve {
	internal class Program {
		private static IrcBot _bot;
		public static IrcConfig Config;

		public static bool ShouldRun { get; set; } = true;

		private static void ParseAndDo(object sender, DoWorkEventArgs e) {
			while (ShouldRun)
				_bot.Runtime();
		}

		private static void Main() {
			Config = Utilities.CheckConfigExistsAndReturn();
			Console.WriteLine("||| Configuration file loaded.");

			try {
				_bot = new IrcBot(Config);
			} catch (TypeInitializationException e) {
				Console.WriteLine(e);
			}

			BackgroundWorker backgroundDataParser = new BackgroundWorker();
			backgroundDataParser.DoWork += ParseAndDo;
			backgroundDataParser.RunWorkerAsync();

			string command = Console.ReadLine();

			Console.WriteLine("||| Bot has shutdown.");
			Console.ReadLine();
		}
	}
}