using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using Eve.Ref;

namespace Eve {
	public class Writer : MarshalByRefObject {
		private static StreamWriter Output { get; set; }
		private static string ToWrite { get; set; }

		internal static bool Ping(string data) {
			if (!data.StartsWith(Protocols.PING) ||
				string.IsNullOrEmpty(data)) return false;

			// cut 'PING ' from data and send it back as PONG
			SendData(Protocols.PONG, data.Remove(0, 5));
			return true;
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
			if (Output == null) {
				Log("Output stream is not connected to any endpoint. Call method `Intiailise'.", EventLogEntryType.Warning);
				return;
			}

			ToWrite = parameters == null ? command : $"{command} {parameters}";

			try {
				Output.WriteLine(ToWrite);
				Output.Flush();
			} catch (Exception ex) {
				Log($"Error occured writing to stream: {ex}", EventLogEntryType.Error);
				return;
			}

			if (command.Equals(Protocols.PING) ||
				command.Equals(Protocols.PONG)) return;

			Log($" >> {ToWrite}", EventLogEntryType.Information);
		}

		/// <summary>
		///     Shorthand method for sending PRIVMSGs
		/// </summary>
		/// <param name="recipient">who to send to </param>
		/// <param name="message">the message to send</param>
		public static void Privmsg(string recipient, string message) {
			SendData("PRIVMSG", string.Concat(recipient, ' ', message));
		}

		public static void Log(string message, EventLogEntryType logType, [CallerMemberName] string memberName = "",
			[CallerLineNumber] int lineNumber = 0) {
			try {
				using (StreamWriter log = new StreamWriter("logs.txt", true)) {
					string _out = $"[{DateTime.Now.ToString("dd/MM hh:mm")} {Enum.GetName(typeof(EventLogEntryType), logType)}] ";

					switch (logType) {
						case EventLogEntryType.SuccessAudit:
						case EventLogEntryType.FailureAudit:
						case EventLogEntryType.Warning:
						case EventLogEntryType.Error:
							_out += $"from `{memberName}' at {lineNumber}: {message}";

							Console.WriteLine(_out);
							log.WriteLine(_out);
							log.Flush();
							break;
						case EventLogEntryType.Information:
							_out += message;

							Console.WriteLine(_out);
							log.WriteLine(_out);
							log.Flush();
							break;
						default:
							throw new ArgumentOutOfRangeException(nameof(logType), logType, null);
					}
				}
			} catch (Exception ex) {
				Console.WriteLine($"||| Logging error occured: {ex}", EventLogEntryType.Error);
			}
		}
	}
}