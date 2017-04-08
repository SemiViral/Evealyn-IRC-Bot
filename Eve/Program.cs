#region usings

using System;
using System.ComponentModel;
using System.Diagnostics;
using Eve.Classes;

#endregion

namespace Eve {
    public class Program {
        public static IrcBot Bot;
        private static BotConfig config;

        private static void ParseAndDo(object sender, DoWorkEventArgs e) {
            while (Bot.CanExecute) Bot.ExecuteRuntime();
        }

        private static void NonDebugRun() {
            string input = string.Empty;

            BackgroundWorker backgroundWorker = new BackgroundWorker();
            backgroundWorker.DoWork += ParseAndDo;
            backgroundWorker.RunWorkerAsync();

            do {
                input = Console.ReadLine();
            } while (!string.IsNullOrEmpty(input) &&
                     !input.ToLower().Equals("exit"));
        }

        private static void DebugRun() {
            while (Bot.CanExecute) Bot.ExecuteRuntime();
        }

        private static void ExecuteRuntime() {
            using (Bot = new IrcBot(config)) {
#if DEBUG
                DebugRun();
#else
				NonDebugRun();
#endif
            }
        }

        private static void Main() {
            config = BotConfig.GetDefaultConfig();
            Writer.Log("Configuration file loaded.", IrcLogEntryType.System);

            ExecuteRuntime();

            Writer.Log("Bot has shutdown. Press any key to exit program.", IrcLogEntryType.System);
            Console.ReadKey();
        }
    }
}