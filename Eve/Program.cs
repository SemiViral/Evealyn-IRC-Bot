#region

using System;
using System.ComponentModel;
using System.Diagnostics;
using Eve.Classes;

#endregion

namespace Eve {
    public class Program {
        public static IrcBot Bot;
        private static BotConfig _config;

        private static void ParseAndDo(object sender, DoWorkEventArgs e) {
            while (Bot.CanExecute) {
                Bot.ExecuteRuntime();
            }
        }

        private static void NonDebugRun() {
            string input = string.Empty;

            BackgroundWorker backgroundDataParser = new BackgroundWorker();
            backgroundDataParser.DoWork += ParseAndDo;
            backgroundDataParser.RunWorkerAsync();

            do {
                input = Console.ReadLine();
            } while (!string.IsNullOrEmpty(input) &&
                     !input.ToLower().Equals("exit"));
        }

        private static void DebugRun() {
            while (Bot.CanExecute) {
                Bot.ExecuteRuntime();
            }
        }

        private static void RunOverlay() {
            using (Bot = new IrcBot(_config)) {
#if DEBUG
                DebugRun();
#else
				NonDebugRun();
#endif
            }
        }

        private static void Main() {
            _config = BotConfig.GetDefaultConfig();
            Writer.Log("Configuration file loaded.", EventLogEntryType.Information);

            RunOverlay();

            Writer.Log("Bot has shutdown. Press any key to exit program.", EventLogEntryType.Information);
            Console.ReadLine();
        }
    }
}