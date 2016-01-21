using System;

namespace Eve.Types.Irc {
	public static class IrcProtocol {
		public const string Default = "";
		public const string User = "USER";
		public const string Nick = "NICK";
		public const string Quit = "QUIT";
		public const string Join = "JOIN";
		public const string Part = "PART";
		public const string Privmsg = "PRIVMSG";
		public const string Mode = "MODE";
		public const string Topic = "TOPIC";
		public const string Kick = "KICK";
		public const string Notice = "NOTICE";
		public const string Names = "NAMES";
		public const string List = "LIST";
		public const string Motd = "MOTD";
		public const string Version = "VERSION";
		public const string Stats = "STATS";
		public const string Links = "LINKS";
		public const string Time = "TIME";
		public const string Connect = "CONNECT";
		public const string Admin = "ADMIN";
		public const string Info = "INFO";
		public const string Servlist = "SERVERLIST";
		public const string Who = "WHO";
		public const string Whois = "WHOIS";
		public const string Whowas = "WHOWAS";
		public const string Kill = "KILL";
		public const string Ping = "PING";
		public const string Pong = "PONG";
		public const string Error = "ERROR";
		public const string Away = "AWAY";
		public const string Rehash = "REHASH";
		public const string Users = "USERS";
		public const string Userhost = "USERHOST";
		public const string Ison = "ISON";

		/* Numeric responses */

		/// <summary>
		///     [nick]![user]@[host]
		/// </summary>
		public const string Welcome = "001";

		/// <summary>
		///     Host is [servername], version [ver]
		/// </summary>
		public const string YourHost = "002";

		/// <summary>
		///     Created [date]
		/// </summary>
		public const string CreationDate = "003";

		/// <summary>
		///     [servername] [version] [available user modes] [available channel modes]
		/// </summary>
		public const string YourInfo = "004";

		/// <summary>
		///     Try server [server name], port [port number]
		/// </summary>
		public const string AltServer = "005";

		/// <summary>
		///     :*1[reply] *( " " [reply] )
		/// </summary>
		public const string UserhostReply = "302";

		/// <summary>
		///     :*1[nick] *( " " [nick] )
		/// </summary>
		public const string IsonReply = "303";

		/// <summary>
		///     [nick] :[away message]
		/// </summary>
		public const string NickAway = "301";

		/// <summary>
		///     :No longer marked away
		/// </summary>
		public const string NotAway = "305";

		/// <summary>
		///     :You have been marked as away
		/// </summary>
		public const string SelfAway = "306";

		/// <summary>
		///     [nick] [user] [host] * :[real name]
		/// </summary>
		public const string WhoisReply = "311";

		/// <summary>
		///     [nick] [server] :[server info]
		/// </summary>
		public const string WhoisServerReply = "312";

		/// <summary>
		///     [nick] :is an IRC operator
		/// </summary>
		public const string WhoisOperatorReply = "313";

		/// <summary>
		///     [nick] [integer] :seconds idle
		/// </summary>
		public const string WhoisIdleReply = "317";

		/// <summary>
		///     [nick] :End of WHOIS list
		/// </summary>
		public const string WhoisEnd = "318";

		/// <summary>
		///     [nick] :*( ( "@" / "+" ) [channel] " " )
		/// </summary>
		public const string WhoisChannelsReply = "319";

		/// <summary>
		///     [nick] [user] [host] * :[real name]
		/// </summary>
		public const string WhowasUserReply = "314";

		/// <summary>
		///     [nick] :End of WHOWAS
		/// </summary>
		public const string WhowasUserEnd = "315";

		/// <summary>
		///     [channel] [# visible] :[topic]
		/// </summary>
		public const string ListReply = "322";

		/// <summary>
		///     :End of LIST
		/// </summary>
		public const string ListEnd = "323";

		/// <summary>
		///     [channel] [nickname]
		/// </summary>
		public const string UniqueOpIs = "325";

		/// <summary>
		///     [channel] [mode] [mode params]
		/// </summary>
		public const string ChannelModeIs = "324";

		/// <summary>
		///     [channel] :No topic is set
		/// </summary>
		public const string NoChannelTopic = "331";

		/// <summary>
		///     [channel] :[topic]
		/// </summary>
		public const string ChannelTopic = "332";

		/// <summary>
		///     [version].[debuglevel] [server] :[comments]
		/// </summary>
		public const string VersionReply = "351";

		/// <summary>
		///     [channel] [user] [host] [server] [nick] ( "H" / "G" ] ["*"] [ ("@" / "+")] :[hopcount] [real name]
		/// </summary>
		public const string WhoReply = "352";

		/// <summary>
		///     [name] :End of WHO list
		/// </summary>
		public const string WhoReplyEnd = "315";

		/// <summary>
		///     ( "=" / "*" / "@" ) [channel] :[ "@" / "+" ] [nick] *( " " [ "@" / "+" ] [nick] )
		/// </summary>
		public const string NameReply = "353";

		/// <summary>
		///     [channel] :End of NAMES list
		/// </summary>
		public const string NamesReplyEnd = "366";

		/// <summary>
		///     [mask] [server] :[hopcount] [server info]
		/// </summary>
		public const string LinksReply = "364";

		/// <summary>
		///     [mask] :End of LINKS list
		/// </summary>
		public const string LinksReplyEnd = "365";

		/// <summary>
		///     [channel] [banmask]
		/// </summary>
		public const string BanListReply = "367";

		/// <summary>
		///     [channel] :End of channel ban list
		/// </summary>
		public const string BanListReplyEnd = "368";

		/// <summary>
		///     :[string]
		/// </summary>
		public const string InfoReply = "371";

		/// <summary>
		///     :End of INFO list
		/// </summary>
		public const string InfoReplyEnd = "374";

		/// <summary>
		///     :- [server] Message of the day -
		/// </summary>
		public const string MotdStart = "375";

		/// <summary>
		///     :- [text]
		/// </summary>
		public const string MotdReply = "372";

		/// <summary>
		///     :End of MOTD command
		/// </summary>
		public const string MotdReplyEnd = "376";

		/// <summary>
		///     :[username] [ttyline] [hostname]
		/// </summary>
		public const string UsersReply = "393";

		/// <summary>
		///     :End of users
		/// </summary>
		public const string UsersReplyEnd = "394";

		/// <summary>
		///     :Nobody logged in
		/// </summary>
		public const string NoUsersReply = "395";

		/// <summary>
		///     [linkname] [sendq] [sent messages] [sent Kbytes] [received messages] [received Kbytes] [time open]
		/// </summary>
		public const string StatsLinkInfoReply = "211";

		/// <summary>
		///     [command] [count] [byte count] [remote count]
		/// </summary>
		public const string StatsCommandsReply = "212";

		/// <summary>
		///     [stats letter] :End of STATS report
		/// </summary>
		public const string StatsReplyEnd = "219";

		/// <summary>
		///     :Server Up %d days %d:%02d:%02d
		/// </summary>
		public const string StatsUptimeReply = "242";

		/// <summary>
		///     O [hostmask] * [name]
		/// </summary>
		public const string StatsOnline = "243";

		/// <summary>
		///     [user mode string]
		/// </summary>
		public const string UserModeIsReply = "221";

		/// <summary>
		///     [name] [server] [mask] [type] [hopcount] [info]
		/// </summary>
		public const string ServerListReply = "234";

		/// <summary>
		///     [mask] [type] :End of service listing
		/// </summary>
		public const string ServerListReplyEnd = "235";

		/// <summary>
		///     [command] :Please wait a while and try again.
		/// </summary>
		public const string TryAgainReply = "263";

		/* Error responses */

		/// <summary>
		///     [nickname] :No such nick/channel
		/// </summary>
		public const string ErrorNoSuchNick = "401";

		/// <summary>
		///     [server name] :No such server
		/// </summary>
		public const string ErrorNoSuchServer = "402";

		/// <summary>
		///     [channel name] :No such channel
		/// </summary>
		public const string ErrorNoSuchChannel = "403";

		/// <summary>
		///     [channel name] :Cannot send to channel
		/// </summary>
		public const string ErrorCannotSendToChan = "404";

		/// <summary>
		///     [channel name] :You have joined too many channels
		/// </summary>
		public const string ErrorTooManyChannels = "405";

		/// <summary>
		///     [nickname] :There was no such nickname
		/// </summary>
		public const string ErrorWasNoSuchNick = "406";

		/// <summary>
		///     [target] :[error code] recipients. [abort message]
		/// </summary>
		public const string ErrorTooManyTargets = "407";

		/// <summary>
		///     [service name] :No such service
		/// </summary>
		public const string ErrorNoSuchService = "408";

		/// <summary>
		///     :No origin specified - PING or PONG message missing originator parameter
		/// </summary>
		public const string ErrorNoOrigin = "409";

		/// <summary>
		///     :No recipient given ([command])
		/// </summary>
		public const string ErrorNoRecipient = "411";

		/// <summary>
		///     :No text to send
		/// </summary>
		public const string ErrorNoTextToSend = "412";

		/// <summary>
		///     [mask] :No toplevel domain specified
		/// </summary>
		public const string ErrorNoTopLevel = "413";

		/// <summary>
		///     [mask] :Wildcard in toplevel domain
		/// </summary>
		public const string ErrorWildTopLevel = "414";

		/// <summary>
		///     [mask] :Bad Server/host mask
		/// </summary>
		public const string ErrorBadMask = "415";

		/// <summary>
		///     [command] :Unknown command
		/// </summary>
		public const string ErrorUnknownCommand = "421";

		/// <summary>
		///     :MOTD File is missing
		/// </summary>
		public const string ErrorNoMotd = "422";

		/// <summary>
		///     [server] :No administrative info available
		/// </summary>
		public const string ErrorNoAdminInfo = "423";

		/// <summary>
		///     :File error doing [file op] on [file]
		/// </summary>
		public const string ErrorFileError = "424";

		/// <summary>
		///     :No nickname given
		/// </summary>
		public const string ErrorNoNicknameGiven = "431";

		/// <summary>
		///     [nick] :Erroneous nickname
		/// </summary>
		public const string ErrorErreoneousNickname = "432";

		/// <summary>
		///     [nick] :Nickname is already in use
		/// </summary>
		public const string ErrorNickNameInUse = "433";

		/// <summary>
		///     [nick] :Nickname collision KILL from [user]@[host]
		/// </summary>
		public const string ErrorNickCollision = "436";

		/// <summary>
		///     [nick/channel] :Nick/channel is temporarily unavailable
		/// </summary>
		public const string ErrorUnavailableResource = "437";

		/// <summary>
		///     [nick] [channel] :They aren't on that channel
		/// </summary>
		public const string ErrorUserNotInChannel = "437";

		/// <summary>
		///     [channel] :You're not on that channel
		/// </summary>
		public const string ErrorNotOnChannel = "442";

		/// <summary>
		///     [user] [channel] :is already on channel
		/// </summary>
		public const string ErrorUserOnChannel = "443";

		/// <summary>
		///     [user] :User not logged in
		/// </summary>
		public const string ErrorNoLogin = "444";

		/// <summary>
		///     :USERS has been disabled
		/// </summary>
		public const string ErrorUsersDisabled = "446";

		/// <summary>
		///     :You have not registered
		/// </summary>
		public const string ErrorNotRegistered = "451";

		/// <summary>
		///     [command] :Not enough parameters
		/// </summary>
		public const string ErrorNeedMoreParams = "461";

		/// <summary>
		///     :Unauthorized command (already registered)
		/// </summary>
		public const string ErrorAlreadyRegistered = "462";

		/// <summary>
		///     :Your host isn't among the privileged
		/// </summary>
		public const string ErrorNoPermForHost = "463";

		/// <summary>
		///     :Password incorrect
		/// </summary>
		public const string ErrorPasswordMismatch = "464";

		/// <summary>
		///     :You are banned from this server
		/// </summary>
		public const string ErrorYouAreBanned = "465";

		/// <summary>
		///     - Sent by a server to a user to inform that access to the server will soon be denied.
		/// </summary>
		public const string ErrorYouWillBeBanned = "466";

		/// <summary>
		///     [channel] :Channel key already set
		/// </summary>
		public const string ErrorKeyset = "467";

		/// <summary>
		///     [channel] :Cannot join channel (+l)
		/// </summary>
		public const string ErrorChannelIsFull = "471";

		/// <summary>
		///     [char] :is unknown mode char to me for [channel]
		/// </summary>
		public const string ErrorUnknownMode = "472";

		/// <summary>
		///     [channel] :Cannot join channel (+i)
		/// </summary>
		public const string ErrorInviteOnlyChan = "473";

		/// <summary>
		///     [channel] :Cannot join channel (+b)
		/// </summary>
		public const string ErrorBannedFromChan = "747";

		/// <summary>
		///     [channel] :Cannot join channel (+k)
		/// </summary>
		public const string ErrorBadChannelKey = "475";

		/// <summary>
		///     [channel] :Bad Channel Mask
		/// </summary>
		public const string ErrorBadChanMask = "476";

		/// <summary>
		///     [channel] :Channel doesn't support modes
		/// </summary>
		public const string ErrorNoChanModes = "477";

		/// <summary>
		///     [channel] [char] :Channel list is full
		/// </summary>
		public const string ErrorBanListFull = "478";

		/// <summary>
		///     :Permission Denied- You're not an IRC operator
		/// </summary>
		public const string ErrorNoPrivegedes = "481";

		/// <summary>
		///     [channel] :You're not channel operator
		/// </summary>
		public const string ErrorChanopPrivsNeeded = "482";

		/// <summary>
		///     :You can't kill a server
		/// </summary>
		public const string ErrorCantKillServer = "483";

		/// <summary>
		///     :Your connection is restricted
		/// </summary>
		public const string ErrorRestricted = "484";

		/// <summary>
		///     :You're not the original channel operator
		/// </summary>
		public const string ErrorUniqopPrivsNeeded = "485";

		/// <summary>
		///     :No O-lines for your host
		/// </summary>
		public const string ErrorNoOperHost = "491";

		/// <summary>
		///     :Unknown MODE flag
		/// </summary>
		public const string ErrorUnknownModeFlag = "501";

		/// <summary>
		///     :Cannot change mode for other user
		/// </summary>
		public const string ErrorUsersDontMatch = "502";
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