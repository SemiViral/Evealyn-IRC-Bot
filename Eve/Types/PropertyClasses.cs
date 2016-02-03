using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Eve.Ref.Irc;
using Newtonsoft.Json.Linq;

namespace Eve.Types {
	public class Module {
		public Module(string name, AppDomain domain, AssemblyName assemblyName) {
			Name = name;
			Domain = domain;
			ReferenceAssembly = domain.Load(assemblyName);

			foreach (Type type in ReferenceAssembly.GetTypes().Where(type => type.GetInterface("IModule") != null)) {
				Types.Add(new TypeAssembly(type));
			}
		}

		public string Name { get; }
		public AppDomain Domain { get; }
		public List<TypeAssembly> Types { get; } = new List<TypeAssembly>();

		private Assembly ReferenceAssembly { get; }
	}

	public class TypeAssembly {
		public string Name { get; set; }
		public string Accessor { get; set; }
		public string Descriptor { get; set; }

		private Type Assembly { get; }
		private MethodInfo Method { get; }
		private object Instance { get; }

		public TypeAssembly(Type type) {
			Assembly = type;
			Method = type.GetMethod("OnChannelMessage");
			Instance = Activator.CreateInstance(Assembly);
		}

		public object OnChannelMessage(ChannelMessage c, PropertyReference p) {
			return Method.Invoke(Instance, new object[] {c, p});
		}
	}

	public class User {
		public int Id { get; set; }
		public string Nickname { get; set; }
		public string Realname { get; set; }
		public int Access { get; set; }
		public DateTime Seen { get; set; }
		public int Attempts { get; set; }

		public List<Message> Messages { get; set; } = new List<Message>();

		/// <summary>
		///     Creates a new user and updates the users & userTimeouts collections
		/// </summary>
		/// <param name="u"><see cref="User" /> object to surmise information from</param>
		public static void Create(User u) {
			Console.WriteLine($"||| Creating database entry for {u.Realname}.");

			u.Id = IrcBot.GetLastDatabaseId() + 1;

			IrcBot.QueryDefaultDatabase($"INSERT INTO users VALUES ({u.Id}, '{u.Nickname}', '{u.Realname}', {u.Access}, '{u.Seen}')");

			IrcBot.V.Users.Add(u);
		}

		/// <summary>
		///     Checks whether or not a specified user exists in database
		/// </summary>
		/// <param name="realname">name to check</param>
		/// <returns>true: user exists; false: user does not exist</returns>
		public static bool CheckExists(string realname) {
			return IrcBot.V.QueryName(realname) != null;
		}

		/// <summary>
		///     Updates specified user's `seen` data and sets user to CurrentUser
		/// </summary>
		/// <param name="realname"></param>
		/// <param name="nickname"></param>
		public static void UpdateCurrentUser(string realname, string nickname) {
			IrcBot.V.Users.First(e => e.Realname == realname).Seen = DateTime.UtcNow;
			IrcBot.V.CurrentUser = IrcBot.V.Users.FirstOrDefault(e => e.Realname == realname);

			IrcBot.QueryDefaultDatabase($"UPDATE users SET seen='{DateTime.UtcNow}' WHERE realname='{realname}'");

			if (IrcBot.V.CurrentUser.Nickname != IrcBot.V.QueryName(IrcBot.V.CurrentUser.Realname).Nickname)
				IrcBot.QueryDefaultDatabase($"UPDATE users SET nickname='{nickname}' WHERE realname='{realname}'");
		}

		/// <summary>
		/// Adds a Message object to list
		/// </summary>
		/// <param name="realname">Name of user to be modified</param>
		/// <param name="m"><see cref="Message"/> to be added</param>
		public static void AddMessage(string realname, Message m) {
			IrcBot.V.Users.First(e => e.Realname == realname).Messages.Add(m);
		}

		public static void SetAccess(string realname, int access) {
			IrcBot.V.Users.First(e => e.Realname == realname).Access = access;
		}
		/// <summary>
		/// Discern whether a user has exceeded command-querying limit
		/// </summary>
		/// <param name="who">user to check</param>
		/// <returns></returns>
		public static bool GetTimeout(string who) {
			bool doTimeout = false;

			if (IrcBot.V.QueryName(who) == null)
				return false;

			if (IrcBot.V.CurrentUser.Attempts == 4)
				// Check if user's last message happened more than 1 minute ago
				if (IrcBot.V.QueryName(who).Seen.AddMinutes(1) < DateTime.UtcNow)
					IrcBot.V.CurrentUser.Attempts = 0; // if so, reset their attempts to 0
				else doTimeout = true; // if not, timeout is true
			else if (IrcBot.V.QueryName(who).Access > 1)
				// if user isn't admin/op, increment their attempts
				IrcBot.V.CurrentUser.Attempts++;

			return doTimeout;
		}
	}

	public class Message {
		public string Sender { get; set; }
		public string Contents { get; set; }
		public DateTime Date { get; set; }
	}

	public class Channel {
		public string Name { get; set; }
		public string Topic { get; set; }
		public List<string> UserList { get; set; }
		public List<IrcMode> Modes { get; set; }

		/// <summary>
		///     Checks if a channel exists within V.Channels
		/// </summary>
		/// <param name="channel">channel to be checked against list</param>
		/// <returns>true: channel is in list; false: channel is not in list</returns>
		public static bool CheckExists(string channel) {
			return IrcBot.V.Channels.Any(e => e.Name == channel);
		}

		/// <summary>
		///     Adds channel to list of currently connected channels
		/// </summary>
		/// <param name="channel">Channel name to be checked against and added</param>
		public static bool CheckAdd(string channel) {
			if (IrcBot.V.Channels.All(e => e.Name != channel) &&
				channel.StartsWith("#"))
				IrcBot.V.Channels.Add(new Channel {
					Name = channel,
					UserList = new List<string>()
				});
			else return false;

			return true;
		}

		/// <summary>
		/// Adds a user to a channel in list
		/// </summary>
		/// <param name="channel">channel for user to be added to</param>
		/// <param name="realname">user to be added</param>
		public static bool AddUserToChannel(string channel, string realname) {
			if (!CheckExists(channel)) return false;

			IrcBot.V.Channels.FirstOrDefault(e => e.Name == channel)?.UserList.Add(realname);
			return true;
		}

		/// <summary>
		/// Removes a channel from list
		/// </summary>
		/// <param name="channel">channel to be removed</param>
		/// <returns>True: removed successfully</returns>
		public static bool Remove(string channel) {
			if (!CheckExists(channel))
				IrcBot.V.Channels.RemoveAll(e => e.Name == channel);
			else return false;

			return true;
		}

		/// <summary>
		/// Remove a user from a channel's user list
		/// </summary>
		/// <param name="channel">channel's UserList to remove from</param>
		/// <param name="realname">user to remove</param>
		public static bool RemoveUserFromChannel(string channel, string realname) {
			if (!CheckExists(channel)) return false;

			Channel firstOrDefault = IrcBot.V.Channels.FirstOrDefault(e => e.Name == channel);
			if (firstOrDefault != null &&
				!firstOrDefault.UserList.Contains(realname)) {
				return false;
			}

			IrcBot.V.Channels.FirstOrDefault(e => e.Name == channel)?.UserList.Remove(realname);
			return true;
		}

		public static string[] List() {
			return IrcBot.V.Channels.Select(e => e.Name).ToArray();
		}
	}

	public class IrcConfig {
		public List<string> IgnoreList { get; set; } = new List<string>();

		public bool Identified { get; set; }

		public string Server { get; set; }
		public string[] Channels { get; set; }
		public string Realname { get; set; }
		public string Nickname { get; set; }
		public string Password { get; set; }
		public string Database { get; set; }

		public int Port { get; set; }

		/// <summary>
		///     Check that the default config file exists, then return an object of it
		/// </summary>
		/// <returns>
		///     <see cref="IrcConfig" />
		/// </returns>
		public static IrcConfig GetDefaultConfig() {
			const string baseConfig =
				"{ \"Nickname\": \"TestyBot\", \"Realname\": \"SemiViral\", \"Password\": \"testypass\", \"Server\": \"irc.foonetic.net\", \"Port\": 6667, \"Channels\": [\"#testgrounds\"],  \"IgnoreList\": [], \"Database\": \"users.sqlite\" }";

			if (!File.Exists("config.json"))
				using (FileStream stream = new FileStream(@"config.json", FileMode.OpenOrCreate, FileAccess.ReadWrite)) {
					Console.WriteLine("||| Configuration file not found, creating.");

					StreamWriter writer = new StreamWriter(stream);
					writer.Write(baseConfig);
					writer.Flush();
				}

			JObject config = JObject.Parse(File.ReadAllText("config.json"));

			return new IrcConfig {
				Nickname = (string)config.SelectToken("Nickname"),
				Realname = (string)config.SelectToken("Realname"),
				Password = (string)config.SelectToken("Password"),
				Port = (int)config.SelectToken("Port"),
				Server = (string)config.SelectToken("Server"),
				Channels = config.SelectToken("Channels")
					.Select(e => e.ToString())
					.ToArray(),
				Database = (string)config.SelectToken("Database")
			};
		}
	}
}