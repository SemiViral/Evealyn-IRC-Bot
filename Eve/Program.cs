﻿#region usings

using System;
using System.ComponentModel;

#endregion

namespace Eve {
    public class Program {
        public static IrcBot Bot;

        private static void ParseAndDo(object sender, DoWorkEventArgs e) {
            while (Bot.CanExecute)
                Bot.ExecuteRuntime();
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
            while (Bot.CanExecute)
                Bot.ExecuteRuntime();
        }

        private static void ExecuteRuntime() {
            using (Bot = new IrcBot()) {
#if DEBUG
                DebugRun();
#else
				NonDebugRun();
#endif
            }
        }

        private static void Main() {
            ExecuteRuntime();
        }
    }
}