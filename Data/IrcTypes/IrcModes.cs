using System.Collections.Generic;

// ReSharper disable InconsistentNaming

namespace Eve.Data.Modes {
	public static class IrcModes {
		// Several modes mean different things on
		// seperate servers, so this may require some
		// tinkering when connecting to a different server.
		//
		// You can probably find your server's MODE specs by
		// Googling them or asking the admin for documentation.
		//
		// Eve will probably only use general mode specs,
		// +v, +o, etc. that are the same across most servers.
		// ====================================
		// Notes on mode
		// Mode syntax follows the SyntaxParser specification
		public static List<IrcMode> modes = new List<IrcMode>();

		static IrcModes() {
			modes.Add(new IrcMode {
				Mode = 'b',
				Translation = "BAN",
				Syntax = "MODE %c +b %n!%i@%h"
			});

			modes.Add(new IrcMode {
				Mode = 'e',
				Translation = "BAN_EXCEPTION",
				Syntax = "MODE %c +e %n!%i@%h"
			});

			modes.Add(new IrcMode {
				Mode = 'c',
				Translation = "NOCOLOR",
				Syntax = "MODE %c +e"
			});

			modes.Add(new IrcMode {
				Mode = 'f',
				Translation = "FLOOD_LIMIT",
				Syntax = "MODE %c +e %p"
			});

			modes.Add(new IrcMode {
				Mode = 'j',
				Translation = "JOIN_THROTTLE",
				Syntax = "MODE %c +e %p:%p2"
			});

			modes.Add(new IrcMode {
				Mode = 'l',
				Translation = "LIMIT",
				Syntax = "MODE %c +e %p"
			}); // Sets limit to number of users in a channel

			modes.Add(new IrcMode {
				Mode = 'm',
				Translation = "MODERATRED_CHANNEL",
				Syntax = "MODE %c +e"
			});

			modes.Add(new IrcMode {
				Mode = 'p',
				Translation = "PRIVATE",
				Syntax = "MODE %c +p"
			}); // Considered obsolete, alt: SECRET

			modes.Add(new IrcMode {
				Mode = 's',
				Translation = "SECRET",
				Syntax = "MODE %c +s",
			});

			modes.Add(new IrcMode {
				Mode = 'z',
				Translation = "SECURED_ONLY",
				Syntax = "MODE %channel +z"
			});

			modes.Add(new IrcMode {
				Mode = 'c',
				Translation = "NO_CTCP",
				Syntax = "MODE %channel +C"
			});

			modes.Add(new IrcMode {
				Mode = 'G',
				Translation = "STRIP_BAD_WORDS",
				Syntax = "MODE %c +G"
			});

			modes.Add(new IrcMode {
				Mode = 'M',
				Translation = "REGONLY",
				Syntax = "MODE %c +M"
			}); // Only registered nicknames can talk

			modes.Add(new IrcMode {
				Mode = 'v',
				Translation = "VOICE",
				Syntax = "MODE %c +v %n"
			}); // Allows user to talk in +M channels

			modes.Add(new IrcMode {
				Mode = 'K',
				Translation = "NOKNOCK",
				Syntax = "MODE %c +K"
			}); // Blocks users from using /KNOCK to 
			// to try and acces a keyword locked channel

			modes.Add(new IrcMode {
				Mode = 'N',
				Translation = "NO_NICK_CHANGE",
				Syntax = "mode %c +N"
			});

			modes.Add(new IrcMode {
				Mode = 'Q',
				Translation = "NO_KICKS",
				Syntax = "MODE %c +Q"
			});

			modes.Add(new IrcMode {
				Mode = 'R',
				Translation = "REGONLY",
				Syntax = "MODE %c +R"
			}); // Only registered users may join channel

			modes.Add(new IrcMode {
				Mode = 'S',
				Translation = "STRIP",
				Syntax = "MODE %c +S"
			}); // This channel mode will remove all client
			// color codes from messages in your channel.

			modes.Add(new IrcMode {
				Mode = 'T',
				Translation = "NO_NOTICE",
				Syntax = "MODE %c +T"
			}); // This blocks users from sending NOTICE's
			// to the channel

			modes.Add(new IrcMode {
				Mode = 'V',
				Translation = "NO_INVITES",
				Syntax = "MODE %c +V"
			}); // This mode prevents users from sending channel
			// invites to users outside the channel.
		}
	}

	public class IrcMode {
		public char Mode { get; set; }
		public string Translation { get; set; }
		public string Syntax { get; set; }
	}
}
