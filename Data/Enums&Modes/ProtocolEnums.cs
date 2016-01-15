using System;

namespace Eve.Data.Protocols {
	public enum IrcResponse {
		/* Language Responses */
		Default,
		Nick,
		Quit,
		Join,
		Part,
		Mode,
		Topic,
		Kick,
		Notice,
		Names,
		List,
		Motd,
		Lusers,
		Version,
		Stats,
		Links,
		Time,
		Connect,
		Admin,
		Info,
		Servlist,
		Who,
		Whois,
		Whowas,
		Kill,
		Ping,
		Pong,
		Error,
		Away,
		Rehash,
		Users,
		Userhost,
		Ison,

		/* Numeric responses */
		Welcome = 001, // <nick>!<user>@<host>
		YourHost = 002, // Host is <servername>, version <ver>
		CreationDate = 003, // Created <date>
		YourInfo = 004, // <servername> <version> <available user modes> <available channel modes>
		AltServer = 005, // Try server <server name>, port <port number>
		UserhostReply = 302, // :*1<reply> *( " " <reply> )
		IsonReply = 303, // :*1<nick> *( " " <nick> )
		NickAway = 301, // <nick> :<away message>
		NotAway = 305, // :No longer marked away
		SelfAway = 306, // :You have been marked as away
		WhoisReply = 311, // <nick> <user> <host> * :<real name>
		WhoisServerReply = 312, // <nick> <server> :<server info>
		WhoisOperatorReply = 313, // <nick> :is an IRC operator
		WhoisIdleReply = 317, // <nick> <integer> :seconds idle
		WhoisEnd = 318, // <nick> :End of WHOIS list
		WhoisChannelsReply = 319, // <nick> :*( ( "@" / "+" ) <channel> " " )
		WhowasUserReply = 314, // <nick> <user> <host> * :<real name>
		WhowasUserEnd = 315, // <nick> :End of WHOWAS
		ListReply = 322, // <channel> <# visible> :<topic>
		ListEnd = 323, // :End of LIST
		UniqueOpIs = 325, // <channel> <nickname>
		ChannelModeIs = 324, // <channel> <mode> <mode params>
		NoChannelTopic = 331, // <channel> :No topic is set
		ChannelTopic = 332, // <channel> :<topic>
		VersionReply = 351, // <version>.<debuglevel> <server> :<comments>
		WhoReply = 352, // <channel> <user> <host> <server> <nick> ( "H" / "G" > ["*"] [ ("@" / "+")] :<hopcount> <real name>
		WhoReplyEnd = 315, // <name> :End of WHO list
		NameReply = 353, // ( "=" / "*" / "@" ) <channel> :[ "@" / "+" ] <nick> *( " " [ "@" / "+" ] <nick> )
		NamesReplyEnd = 366, // <channel> :End of NAMES list
		LinksReply = 364, // <mask> <server> :<hopcount> <server info>
		LinksReplyEnd = 365, // <mask> :End of LINKS list
		BanListReply = 367, // <channel> <banmask>
		BanListReplyEnd = 368, // <channel> :End of channel ban list
		InfoReply = 371, // :<string>
		InfoReplyEnd = 374, // :End of INFO list
		MotdStart = 375, // :- <server> Message of the day -
		MotdReply = 372, // :- <text>
		MotdReplyEnd = 376, // :End of MOTD command
		UsersReply = 393, // :<username> <ttyline> <hostname>
		UsersReplyEnd = 394, // :End of users
		NoUsersReply = 395, // :Nobody logged in
		StatsLinkInfoReply = 211, // <linkname> <sendq> <sent messages> <sent Kbytes> <received messages> <received Kbytes> <time open>
		StatsCommandsReply = 212, // <command> <count> <byte count> <remote count>
		StatsReplyEnd = 219, // <stats letter> :End of STATS report
		StatsUptimeReply = 242, // :Server Up %d days %d:%02d:%02d
		StatsOnline = 243, // O <hostmask> * <name>
		UserModeIsReply = 221, // <user mode string>
		ServerListReply = 234, // <name> <server> <mask> <type> <hopcount> <info>
		ServerListReplyEnd = 235, // <mask> <type> :End of service listing
		TryAgainReply = 263, // <command> :Please wait a while and try again.

		/* Error responses */
		ErrorNoSuchNick = 401, // <nickname> :No such nick/channel
		ErrorNoSuchServer = 402, // <server name> :No such server
		ErrorNoSuchChannel = 403, // <channel name> :No such channel
		ErrorCannotSendToChan = 404, // <channel name> :Cannot send to channel
		ErrorTooManyChannels = 405, // <channel name> :You have joined too many channels
		ErrorWasNoSuchNick = 406, // <nickname> :There was no such nickname
		ErrorTooManyTargets = 407, // <target> :<error code> recipients. <abort message>
		ErrorNoSuchService = 408, // <service name> :No such service
		ErrorNoOrigin = 409, // :No origin specified - PING or PONG message missing originator parameter
		ErrorNoRecipient = 411, // :No recipient given (<command>)
		ErrorNoTextToSend = 412, // :No text to send
		ErrorNoTopLevel = 413, // <mask> :No toplevel domain specified
		ErrorWildTopLevel = 414, // <mask> :Wildcard in toplevel domain
		ErrorBadMask = 415, // <mask> :Bad Server/host mask
		ErrorUnknownCommand = 421, // <command> :Unknown command
		ErrorNoMotd = 422, // :MOTD File is missing
		ErrorNoAdminInfo = 423, // <server> :No administrative info available
		ErrorFileError = 424, // :File error doing <file op> on <file>
		ErrorNoNicknameGiven = 431, // :No nickname given
		ErrorErreoneousNickname = 432, // <nick> :Erroneous nickname
		ErrorNickNameInUse = 433, // <nick> :Nickname is already in use
		ErrorNickCollision = 436, // <nick> :Nickname collision KILL from <user>@<host>
		ErrorUnavailableResource = 437, // <nick/channel> :Nick/channel is temporarily unavailable
		ErrorUserNotInChannel = 437, // <nick> <channel> :They aren't on that channel
		ErrorNotOnChannel = 442, // <channel> :You're not on that channel
		ErrorUserOnChannel = 443, // <user> <channel> :is already on channel
		ErrorNoLogin = 444, // <user> :User not logged in
		ErrorUsersDisabled = 446, // :USERS has been disabled
		ErrorNotRegistered = 451, // :You have not registered
		ErrorNeedMoreParams = 461, // <command> :Not enough parameters
		ErrorAlreadyRegistered = 462, // :Unauthorized command (already registered)
		ErrorNoPermForHost = 463, // :Your host isn't among the privileged
		ErrorPasswordMismatch = 464, // :Password incorrect
		ErrorYouAreBanned = 465, // :You are banned from this server
		ErrorYouWillBeBanned = 466, // - Sent by a server to a user to inform that access to the server will soon be denied.
		ErrorKeyset = 467, // <channel> :Channel key already set
		ErrorChannelIsFull = 471, // <channel> :Cannot join channel (+l)
		ErrorUnknownMode = 472, // <char> :is unknown mode char to me for <channel>
		ErrorInviteOnlyChan = 473, // <channel> :Cannot join channel (+i)
		ErrorBannedFromChan = 747, // <channel> :Cannot join channel (+b)
		ErrorBadChannelKey = 475, // <channel> :Cannot join channel (+k)
		ErrorBadChanMask = 476, // <channel> :Bad Channel Mask
		ErrorNoChanModes = 477, // <channel> :Channel doesn't support modes
		ErrorBanListFull = 478, // <channel> <char> :Channel list is full
		ErrorNoPrivegedes = 481, // :Permission Denied- You're not an IRC operator
		ErrorChannelOpPrivsNeeded = 482, // <channel> :You're not channel operator
		ErrorCantKillServer = 483, // :You can't kill a server
		ErrorRestricted = 484, // :Your connection is restricted
		ErrorUniqueOpPrivsNeeded = 485, // :You're not the original channel operator
		ErrorNoOperHost = 491, // :No O-lines for your host
		ErrorUnknownModeFlag = 501, // :Unknown MODE flag
		ErrorUsersDontMatch = 502, // :Cannot change mode for other user
	}

	public class ProtocolTranslateException : Exception {
		public ProtocolTranslateException(string originalProtocol)
			: base($"ProtocolTranslateException: failed to convert string '{originalProtocol}' to any IrcResponse enum.") {
			OrginalProtocol = originalProtocol;
		}

		public string OrginalProtocol { get; }
	}
}