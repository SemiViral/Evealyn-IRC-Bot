#region usings

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Eve.References;

#endregion

namespace Eve {
    public class Writer : MarshalByRefObject {
        private static readonly string[] _resitrictedLoggingList = {Protocols.PING, Protocols.PONG};
        private static StreamWriter Output { get; set; }

        /// <summary>
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        private static bool CheckSkipLogging(string command) {
            string[] fullCommand = command.Split(' ');

            if (fullCommand.Length > 1)
                Log(
                    "Pre-logging check input string exceeded normal paramater count. Please review input strings to ensure code quality.",
                    EventLogEntryType.Warning);

            return _resitrictedLoggingList.Contains(fullCommand[0]); // 0th index should be the IRC protocol
        }

        /// <summary>
        ///     Initiailises the Writer object with an output stream
        /// </summary>
        /// <message name="stream">object to get stream from</message>
        public static void Initialise(Stream stream) {
            Output = new StreamWriter(stream);
        }

        /// <summary>
        ///     Outputs text to the stream
        /// </summary>
        /// <message name="command">command to be sent, i.e. PONG or PRIVMSG</message>
        /// <message name="message">message for command</message>
        public static void SendData(string command, string parameters = null) {
            if (Output?.BaseStream == null) {
                Log("Output stream is not connected to any endpoint. Call method `Intiailise'.", EventLogEntryType.Warning);
                return;
            }

            string stringToWrite = $"{command} {parameters}";

            try {
                Output.WriteLine(stringToWrite);
                Output.Flush();
            } catch (Exception ex) {
                Log($"Error occured writing to stream: {ex}", EventLogEntryType.Error);
                return;
            }

            if (!CheckSkipLogging(command))
                Log($" >> {stringToWrite}", EventLogEntryType.Information);
        }

        /// <summary>
        ///     Shorthand method for sending PRIVMSGs
        /// </summary>
        /// <param name="recipient">who to send to </param>
        /// <param name="message">the message to send</param>
        public static void Privmsg(string recipient, string message) {
            SendData("PRIVMSG", $"{recipient} {message}");
        }

        public static void Log(string message, EventLogEntryType logType, [CallerMemberName] string memberName = "",
            [CallerLineNumber] int lineNumber = 0) {
            string timestamp = DateTime.Now.ToString("dd/MM hh:mm");

            try {
                using (StreamWriter log = new StreamWriter("Log.txt", true)) {
                    string _out =
                        $"[{timestamp} {Enum.GetName(typeof(EventLogEntryType), logType)}] ";

                    switch (logType) {
                        case EventLogEntryType.SuccessAudit:
                        case EventLogEntryType.FailureAudit:
                        case EventLogEntryType.Warning:
                        case EventLogEntryType.Error:
                            _out += $"from `{memberName}' at line {lineNumber}";

                            Console.WriteLine(_out);

                            _out = $"\n{_out}\n{message}\n";

                            break;
                        case EventLogEntryType.Information:
                            _out += message;

                            Console.WriteLine(_out);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(logType), logType,
                                "Use of undefined EventLogEntryType.");
                    }

                    log.WriteLine(_out);
                    log.Flush();
                }
            } catch (Exception ex) {
                Console.WriteLine($"||| Logging error occured: {ex}", EventLogEntryType.Error);
            }
        }
    }
}