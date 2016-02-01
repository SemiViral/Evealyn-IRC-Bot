using System;

namespace Eve.Ref.Irc {
	public static class Protocols {
		public const string Default = "",
			User = "USER",
			Nick = "NICK",
			Quit = "QUIT",
			Join = "JOIN",
			Part = "PART",
			Privmsg = "PRIVMSG",
			Mode = "MODE",
			Topic = "TOPIC",
			Kick = "KICK",
			Notice = "NOTICE",
			Names = "NAMES",
			List = "LIST",
			Motd = "MOTD",
			Version = "VERSION",
			Stats = "STATS",
			Links = "LINKS",
			Time = "TIME",
			Connect = "CONNECT",
			Admin = "ADMIN",
			Info = "INFO",
			Servlist = "SERVERLIST",
			Who = "WHO",
			Whois = "WHOIS",
			Whowas = "WHOWAS",
			Kill = "KILL",
			Ping = "PING",
			Pong = "PONG",
			Error = "ERROR",
			Away = "AWAY",
			Rehash = "REHASH",
			Users = "USERS",
			Userhost = "USERHOST",
			Ison = "ISON",
			
			/// <summary>
			///     [nick]![user]@[host]
			/// </summary>
			Welcome = "001",

			/// <summary>
			///     Host is [servername], version [ver]
			/// </summary>
			YourHost = "002",

			/// <summary>
			///     Created [date]
			/// </summary>
			CreationDate = "003",

			/// <summary>
			///     [servername] [version] [available user modes] [available channel modes]
			/// </summary>
			YourInfo = "004",

			/// <summary>
			///     Try server [server name], port [port number]
			/// </summary>
			AltServer = "005",

			/// <summary>
			///     :*1[reply] *( " " [reply] )
			/// </summary>
			UserhostReply = "302",

			/// <summary>
			///     :*1[nick] *( " " [nick] )
			/// </summary>
			IsonReply = "303",

			/// <summary>
			///     [nick] :[away message]
			/// </summary>
			NickAway = "301",

			/// <summary>
			///     :No longer marked away
			/// </summary>
			NotAway = "305",

			/// <summary>
			///     :You have been marked as away
			/// </summary>
			SelfAway = "306",

			/// <summary>
			///     [nick] [user] [host] * :[real name]
			/// </summary>
			WhoisReply = "311",

			/// <summary>
			///     [nick] [server] :[server info]
			/// </summary>
			WhoisServerReply = "312",

			/// <summary>
			///     [nick] :is an IRC operator
			/// </summary>
			WhoisOperatorReply = "313",

			/// <summary>
			///     [nick] [integer] :seconds idle
			/// </summary>
			WhoisIdleReply = "317",

			/// <summary>
			///     [nick] :End of WHOIS list
			/// </summary>
			WhoisEnd = "318",

			/// <summary>
			///     [nick] :*( ( "@" / "+" ) [channel] " " )
			/// </summary>
			WhoisChannelsReply = "319",

			/// <summary>
			///     [nick] [user] [host] * :[real name]
			/// </summary>
			WhowasUserReply = "314",

			/// <summary>
			///     [nick] :End of WHOWAS
			/// </summary>
			WhowasUserEnd = "315",

			/// <summary>
			///     [channel] [# visible] :[topic]
			/// </summary>
			ListReply = "322",

			/// <summary>
			///     :End of LIST
			/// </summary>
			ListEnd = "323",

			/// <summary>
			///     [channel] [nickname]
			/// </summary>
			UniqueOpIs = "325",

			/// <summary>
			///     [channel] [mode] [mode params]
			/// </summary>
			ChannelModeIs = "324",

			/// <summary>
			///     [channel] :No topic is set
			/// </summary>
			NoChannelTopic = "331",

			/// <summary>
			///     [channel] :[topic]
			/// </summary>
			ChannelTopic = "332",

			/// <summary>
			///     [version].[debuglevel] [server] :[comments]
			/// </summary>
			VersionReply = "351",

			/// <summary>
			///     [channel] [user] [host] [server] [nick] ( "H" / "G" ] ["*"] [ ("@" / "+")] :[hopcount] [real name]
			/// </summary>
			WhoReply = "352",

			/// <summary>
			///     [name] :End of WHO list
			/// </summary>
			WhoReplyEnd = "315",

			/// <summary>
			///     ( "=" / "*" / "@" ) [channel] :[ "@" / "+" ] [nick] *( " " [ "@" / "+" ] [nick] )
			/// </summary>
			NameReply = "353",

			/// <summary>
			///     [channel] :End of NAMES list
			/// </summary>
			NamesReplyEnd = "366",

			/// <summary>
			///     [mask] [server] :[hopcount] [server info]
			/// </summary>
			LinksReply = "364",

			/// <summary>
			///     [mask] :End of LINKS list
			/// </summary>
			LinksReplyEnd = "365",

			/// <summary>
			///     [channel] [banmask]
			/// </summary>
			BanListReply = "367",

			/// <summary>
			///     [channel] :End of channel ban list
			/// </summary>
			BanListReplyEnd = "368",

			/// <summary>
			///     :[string]
			/// </summary>
			InfoReply = "371",

			/// <summary>
			///     :End of INFO list
			/// </summary>
			InfoReplyEnd = "374",

			/// <summary>
			///     :- [server] Message of the day -
			/// </summary>
			MotdStart = "375",

			/// <summary>
			///     :- [text]
			/// </summary>
			MotdReply = "372",

			/// <summary>
			///     :End of MOTD command
			/// </summary>
			MotdReplyEnd = "376",

			/// <summary>
			///     :[username] [ttyline] [hostname]
			/// </summary>
			UsersReply = "393",

			/// <summary>
			///     :End of users
			/// </summary>
			UsersReplyEnd = "394",

			/// <summary>
			///     :Nobody logged in
			/// </summary>
			NoUsersReply = "395",

			/// <summary>
			///     [linkname] [sendq] [sent messages] [sent Kbytes] [received messages] [received Kbytes] [time open]
			/// </summary>
			StatsLinkInfoReply = "211",

			/// <summary>
			///     [command] [count] [byte count] [remote count]
			/// </summary>
			StatsCommandsReply = "212",

			/// <summary>
			///     [stats letter] :End of STATS report
			/// </summary>
			StatsReplyEnd = "219",

			/// <summary>
			///     :Server Up %d days %d:%02d:%02d
			/// </summary>
			StatsUptimeReply = "242",

			/// <summary>
			///     O [hostmask] * [name]
			/// </summary>
			StatsOnline = "243",

			/// <summary>
			///     [user mode string]
			/// </summary>
			UserModeIsReply = "221",

			/// <summary>
			///     [name] [server] [mask] [type] [hopcount] [info]
			/// </summary>
			ServerListReply = "234",

			/// <summary>
			///     [mask] [type] :End of service listing
			/// </summary>
			ServerListReplyEnd = "235",

			/// <summary>
			///     [command] :Please wait a while and try again.
			/// </summary>
			TryAgainReply = "263",

			/* Error responses */

			/// <summary>
			///     [nickname] :No such nick/channel
			/// </summary>
			ErrorNoSuchNick = "401",

			/// <summary>
			///     [server name] :No such server
			/// </summary>
			ErrorNoSuchServer = "402",

			/// <summary>
			///     [channel name] :No such channel
			/// </summary>
			ErrorNoSuchChannel = "403",

			/// <summary>
			///     [channel name] :Cannot send to channel
			/// </summary>
			ErrorCannotSendToChan = "404",

			/// <summary>
			///     [channel name] :You have joined too many channels
			/// </summary>
			ErrorTooManyChannels = "405",

			/// <summary>
			///     [nickname] :There was no such nickname
			/// </summary>
			ErrorWasNoSuchNick = "406",

			/// <summary>
			///     [target] :[error code] recipients. [abort message]
			/// </summary>
			ErrorTooManyTargets = "407",

			/// <summary>
			///     [service name] :No such service
			/// </summary>
			ErrorNoSuchService = "408",

			/// <summary>
			///     :No origin specified - PING or PONG message missing originator parameter
			/// </summary>
			ErrorNoOrigin = "409",

			/// <summary>
			///     :No recipient given ([command])
			/// </summary>
			ErrorNoRecipient = "411",

			/// <summary>
			///     :No text to send
			/// </summary>
			ErrorNoTextToSend = "412",

			/// <summary>
			///     [mask] :No toplevel domain specified
			/// </summary>
			ErrorNoTopLevel = "413",

			/// <summary>
			///     [mask] :Wildcard in toplevel domain
			/// </summary>
			ErrorWildTopLevel = "414",

			/// <summary>
			///     [mask] :Bad Server/host mask
			/// </summary>
			ErrorBadMask = "415",

			/// <summary>
			///     [command] :Unknown command
			/// </summary>
			ErrorUnknownCommand = "421",

			/// <summary>
			///     :MOTD File is missing
			/// </summary>
			ErrorNoMotd = "422",

			/// <summary>
			///     [server] :No administrative info available
			/// </summary>
			ErrorNoAdminInfo = "423",

			/// <summary>
			///     :File error doing [file op] on [file]
			/// </summary>
			ErrorFileError = "424",

			/// <summary>
			///     :No nickname given
			/// </summary>
			ErrorNoNicknameGiven = "431",

			/// <summary>
			///     [nick] :Erroneous nickname
			/// </summary>
			ErrorErreoneousNickname = "432",

			/// <summary>
			///     [nick] :Nickname is already in use
			/// </summary>
			ErrorNickNameInUse = "433",

			/// <summary>
			///     [nick] :Nickname collision KILL from [user]@[host]
			/// </summary>
			ErrorNickCollision = "436",

			/// <summary>
			///     [nick/channel] :Nick/channel is temporarily unavailable
			/// </summary>
			ErrorUnavailableResource = "437",

			/// <summary>
			///     [nick] [channel] :They aren't on that channel
			/// </summary>
			ErrorUserNotInChannel = "437",

			/// <summary>
			///     [channel] :You're not on that channel
			/// </summary>
			ErrorNotOnChannel = "442",

			/// <summary>
			///     [user] [channel] :is already on channel
			/// </summary>
			ErrorUserOnChannel = "443",

			/// <summary>
			///     [user] :User not logged in
			/// </summary>
			ErrorNoLogin = "444",

			/// <summary>
			///     :USERS has been disabled
			/// </summary>
			ErrorUsersDisabled = "446",

			/// <summary>
			///     :You have not registered
			/// </summary>
			ErrorNotRegistered = "451",

			/// <summary>
			///     [command] :Not enough parameters
			/// </summary>
			ErrorNeedMoreParams = "461",

			/// <summary>
			///     :Unauthorized command (already registered)
			/// </summary>
			ErrorAlreadyRegistered = "462",

			/// <summary>
			///     :Your host isn't among the privileged
			/// </summary>
			ErrorNoPermForHost = "463",

			/// <summary>
			///     :Password incorrect
			/// </summary>
			ErrorPasswordMismatch = "464",

			/// <summary>
			///     :You are banned from this server
			/// </summary>
			ErrorYouAreBanned = "465",

			/// <summary>
			///     - Sent by a server to a user to inform that access to the server will soon be denied.
			/// </summary>
			ErrorYouWillBeBanned = "466",

			/// <summary>
			///     [channel] :Channel key already set
			/// </summary>
			ErrorKeyset = "467",

			/// <summary>
			///     [channel] :Cannot join channel (+l)
			/// </summary>
			ErrorChannelIsFull = "471",

			/// <summary>
			///     [char] :is unknown mode char to me for [channel]
			/// </summary>
			ErrorUnknownMode = "472",

			/// <summary>
			///     [channel] :Cannot join channel (+i)
			/// </summary>
			ErrorInviteOnlyChan = "473",

			/// <summary>
			///     [channel] :Cannot join channel (+b)
			/// </summary>
			ErrorBannedFromChan = "747",

			/// <summary>
			///     [channel] :Cannot join channel (+k)
			/// </summary>
			ErrorBadChannelKey = "475",

			/// <summary>
			///     [channel] :Bad Channel Mask
			/// </summary>
			ErrorBadChanMask = "476",

			/// <summary>
			///     [channel] :Channel doesn't support modes
			/// </summary>
			ErrorNoChanModes = "477",

			/// <summary>
			///     [channel] [char] :Channel list is full
			/// </summary>
			ErrorBanListFull = "478",

			/// <summary>
			///     :Permission Denied- You're not an IRC operator
			/// </summary>
			ErrorNoPrivegedes = "481",

			/// <summary>
			///     [channel] :You're not channel operator
			/// </summary>
			ErrorChanopPrivsNeeded = "482",

			/// <summary>
			///     :You can't kill a server
			/// </summary>
			ErrorCantKillServer = "483",

			/// <summary>
			///     :Your connection is restricted
			/// </summary>
			ErrorRestricted = "484",

			/// <summary>
			///     :You're not the original channel operator
			/// </summary>
			ErrorUniqopPrivsNeeded = "485",

			/// <summary>
			///     :No O-lines for your host
			/// </summary>
			ErrorNoOperHost = "491",

			/// <summary>
			///     :Unknown MODE flag
			/// </summary>
			ErrorUnknownModeFlag = "501",

			/// <summary>
			///     :Cannot change mode for other user
			/// </summary>
			ErrorUsersDontMatch = "502";
	}

	[Serializable]
	public class ProtocolTranslateException : Exception {
		public ProtocolTranslateException(string originalProtocol)
			: base($"ProtocolTranslateException: failed to convert string '{originalProtocol}' to any IrcResponse enum.") {
			OrginalProtocol = originalProtocol;
		}

		public string OrginalProtocol { get; private set; }
	}
}