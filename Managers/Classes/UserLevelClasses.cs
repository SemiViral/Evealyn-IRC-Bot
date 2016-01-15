using System;
using System.Collections.Generic;
using Eve.Data.Modes;

namespace Eve.Managers.Classes {
	public class User {
		public User() {
			Messages = new List<Message>();
		}

		public int Id { get; set; }
		public string Nickname { get; set; }
		public string Realname { get; set; }
		public int Access { get; set; }
		public DateTime Seen { get; set; }
		public List<Message> Messages { get; set; }
		public int Attempts { get; set; }
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
	}
}
