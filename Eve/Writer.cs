#region usings

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Timers;
using Eve.Plugin;

#endregion

namespace Eve {
    internal class Writer : IDisposable {
        /// <summary>
        ///     Initiailises the Writer object with an output stream
        /// </summary>
        /// <message name="stream">object to get stream from</message>
        protected internal Writer(Stream stream) {
            Output = new StreamWriter(stream);

            RecursiveBacklogTrigger = new Timer(5000);
            RecursiveBacklogTrigger.Elapsed += LogBacklog;
            RecursiveBacklogTrigger.Start();
        }

        private StringBuilder Backlog { get; } = new StringBuilder();
        private StreamWriter Output { get; }
        private Timer RecursiveBacklogTrigger { get; }

        public void Dispose() {
            Output?.Dispose();
            RecursiveBacklogTrigger?.Dispose();
        }

        private void LogBacklog(object source, ElapsedEventArgs e) {
            if (Backlog.Length.Equals(0))
                return;

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

        public void SendDataEvent(object source, PluginSimpleReturnMessage returnMessage) {
            SendData(returnMessage.Protocol, $"{returnMessage.Target} {returnMessage.Args}");
        }

        /// <summary>
        ///     Outputs text to the stream
        /// </summary>
        /// <message name="command">command to be sent, i.e. PONG or PRIVMSG</message>
        /// <message name="message">message for command</message>
        public void SendData(string command, string parameters = null) {
            if (Output.BaseStream.Equals(null)) {
                Log(IrcLogEntryType.Warning, "Output stream is not connected to any endpoint. Call method `Intiailise'.");
                return;
            }

            string stringToWrite = $"{command} {parameters}";

            try {
                Output.WriteLine(stringToWrite);
                Output.Flush();
            } catch (Exception ex) {
                Log(IrcLogEntryType.Error, $"Error occured writing to stream: {ex}");
                return;
            }

            Log(IrcLogEntryType.Message, $" >> {stringToWrite}");
        }

        public void LogEvent(object source, LogEntry logEntry) {
            Log(logEntry.EntryType, logEntry.Message, logEntry.MemberName, logEntry.LineNumber);
        }

        public void Log(IrcLogEntryType logType, string message, string memberName = "", int lineNumber = 0) {
            string timestamp = DateTime.Now.ToString("dd/MM hh:mm");

            string _out = $"[{timestamp} {Enum.GetName(typeof(IrcLogEntryType), logType)}]";

            switch (logType) {
                case IrcLogEntryType.System:
                    if (message.StartsWith("PONG"))
                        break;

                    _out += $" {message}";

                    Console.WriteLine(_out);
                    break;
                case IrcLogEntryType.Warning:
                    _out += $" {message}";

                    Console.WriteLine(_out);
                    break;
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
                    throw new ArgumentOutOfRangeException(nameof(logType), logType, "Use of undefined EventLogEntryType.");
            }

            if (!_out.EndsWith(Environment.NewLine))
                _out += Environment.NewLine;

            Backlog.Append(_out);
        }
    }

    [Serializable]
    public class LogEntry {
        public LogEntry(IrcLogEntryType entryType, string logMessage, string memberName = "", int lineNumber = 0) {
            EntryType = entryType;
            Message = logMessage;
            MemberName = memberName;
            LineNumber = lineNumber;
        }

        public IrcLogEntryType EntryType { get; }
        public string Message { get; }
        public string MemberName { get; }
        public int LineNumber { get; }
    }

    public enum IrcLogEntryType {
        Error = 0,
        System,
        Message,
        Warning
    }
}