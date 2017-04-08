#region usings

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Timers;

#endregion

namespace Eve {
    public class Writer : MarshalByRefObject {
        private static StringBuilder Backlog { get; } = new StringBuilder();
        private static StreamWriter Output { get; set; }
        private static Timer RecursiveBacklogTrigger { get; set; }

        /// <summary>
        ///     Initiailises the Writer object with an output stream
        /// </summary>
        /// <message name="stream">object to get stream from</message>
        protected internal static void Initialise(Stream stream) {
            Output = new StreamWriter(stream);

            RecursiveBacklogTrigger = new Timer(5000);
            RecursiveBacklogTrigger.Elapsed += LogBacklog;
            RecursiveBacklogTrigger.Start();
        }

        private static void LogBacklog(object source, ElapsedEventArgs e) {
            if (Backlog.Length.Equals(0)) return;

            try {
                using (StreamWriter log = new StreamWriter("Log.txt", true)) {
                    log.WriteLine(Backlog.ToString().Trim());
                    Backlog.Clear();
                    log.Flush();
                }
            } catch (Exception ex) {
                Console.WriteLine($"||| Logging error occured: {ex}", EventLogEntryType.Error);
            }
        }

        /// <summary>
        ///     Outputs text to the stream
        /// </summary>
        /// <message name="command">command to be sent, i.e. PONG or PRIVMSG</message>
        /// <message name="message">message for command</message>
        public static void SendData(string command, string parameters = null) {
            if (Output.BaseStream.Equals(null)) {
                Log("Output stream is not connected to any endpoint. Call method `Intiailise'.",
                    IrcLogEntryType.Warning);
                return;
            }

            string stringToWrite = $"{command} {parameters}";

            try {
                Output.WriteLine(stringToWrite);
                Output.Flush();
            } catch (Exception ex) {
                Log($"Error occured writing to stream: {ex}", IrcLogEntryType.Error);
                return;
            }

            Log($" >> {stringToWrite}", IrcLogEntryType.Message);
        }

        /// <summary>
        ///     Shorthand method for sending PRIVMSGs
        /// </summary>
        /// <param name="recipient">who to send to </param>
        /// <param name="message">the message to send</param>
        public static void Privmsg(string recipient, string message) {
            SendData("PRIVMSG", $"{recipient} {message}");
        }

        public static void Log(string message, IrcLogEntryType logType, [CallerMemberName] string memberName = "",
            [CallerLineNumber] int lineNumber = 0) {
            string timestamp = DateTime.Now.ToString("dd/MM hh:mm");

            string _out =
                $"[{timestamp} {Enum.GetName(typeof(IrcLogEntryType), logType)}]";

            switch (logType) {
                case IrcLogEntryType.System:
                    if (message.StartsWith("PONG")) break;

                    _out += $" {message}";

                    Console.WriteLine(_out);
                    break;
                case IrcLogEntryType.Warning:
                case IrcLogEntryType.Error:
                    _out += $" from `{memberName}' at line {lineNumber}";

                    Console.WriteLine(_out);

                    _out = $"\n{_out}\n{message}\n";

                    break;
                case IrcLogEntryType.Message:
                    _out += $" {message}";

                    Console.WriteLine(_out);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(logType), logType,
                        "Use of undefined EventLogEntryType.");
            }

            if (!_out.EndsWith(Environment.NewLine))
                _out += Environment.NewLine;

            Backlog.Append(_out);
        }
    }

    public enum IrcLogEntryType {
        Error = 0,
        System,
        Message,
        Warning
    }
}