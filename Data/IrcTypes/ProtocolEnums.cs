using System;
using System.Collections.Generic;

namespace Eve.Data.Protocols {
	public static class IrcProtocol {
		public const string Default =	"";
		public const string Nick =		"NICK";
		public const string Quit =		"QUIT";
		public const string Join =		"JOIN";
		public const string Part =		"PART";
		public const string Privmsg =	"PRIVMSG";
		public const string Mode =		"MODE";
		public const string Topic =		"TOPIC";
		public const string Kick =		"KICK";
		public const string Notice =	"NOTICE";
		public const string Names =		"NAMES";
		public const string List =		"LIST";
		public const string Motd =		"MOTD";
		public const string Version =	"VERSION";
		public const string Stats =		"STATS";
		public const string Links =		"LINKS";
		public const string Time =		"TIME";
		public const string Connect =	"CONNECT";
		public const string Admin =		"ADMIN";
		public const string Info =		"INFO";
		public const string Servlist =	"SERVERLIST";
		public const string Who =		"WHO";
		public const string Whois =		"WHOIS";
		public const string Whowas =	"WHOWAS";
		public const string Kill =		"KILL";
		public const string Ping =		"PING";
		public const string Pong =		"PONG";
		public const string Error =		"ERROR";
		public const string Away =		"AWAY";
		public const string Rehash =	"REHASH";
		public const string Users =		"USERS";
		public const string Userhost =	"USERHOST";
		public const string Ison =		"ISON";

		/* Numeric responses */
		public const string Welcome =					"001"; // <nick>!<user>@<host>
		public const string YourHost =					"002"; // Host is <servername>, version <ver>
		public const string CreationDate =				"003"; // Created <date>
		public const string YourInfo =					"004"; // <servername> <version> <available user modes> <available channel modes>
		public const string AltServer =					"005"; // Try server <server name>, port <port number>
		public const string UserhostReply =				"302"; // :*1<reply> *( " " <reply> )
		public const string IsonReply =					"303"; // :*1<nick> *( " " <nick> )
		public const string NickAway =					"301"; // <nick> :<away message>
		public const string NotAway =					"305"; // :No longer marked away
		public const string SelfAway =					"306"; // :You have been marked as away
		public const string WhoisReply =				"311"; // <nick> <user> <host> * :<real name>
		public const string WhoisServerReply =			"312"; // <nick> <server> :<server info>
		public const string WhoisOperatorReply =		"313"; // <nick> :is an IRC operator
		public const string WhoisIdleReply =			"317"; // <nick> <integer> :seconds idle
		public const string WhoisEnd =					"318"; // <nick> :End of WHOIS list
		public const string WhoisChannelsReply =		"319"; // <nick> :*( ( "@" / "+" ) <channel> " " )
		public const string WhowasUserReply =			"314"; // <nick> <user> <host> * :<real name>
		public const string WhowasUserEnd =				"315"; // <nick> :End of WHOWAS
		public const string ListReply =					"322"; // <channel> <# visible> :<topic>
		public const string ListEnd =					"323"; // :End of LIST
		public const string UniqueOpIs =				"325"; // <channel> <nickname>
		public const string ChannelModeIs =				"324"; // <channel> <mode> <mode params>
		public const string NoChannelTopic =			"331"; // <channel> :No topic is set
		public const string ChannelTopic =				"332"; // <channel> :<topic>
		public const string VersionReply =				"351"; // <version>.<debuglevel> <server> :<comments>
		public const string WhoReply =					"352"; // <channel> <user> <host> <server> <nick> ( "H" / "G" > ["*"] [ ("@" / "+")] :<hopcount> <real name>
		public const string WhoReplyEnd =				"315"; // <name> :End of WHO list
		public const string NameReply =					"353"; // ( "=" / "*" / "@" ) <channel> :[ "@" / "+" ] <nick> *( " " [ "@" / "+" ] <nick> )
		public const string NamesReplyEnd =				"366"; // <channel> :End of NAMES list
		public const string LinksReply =				"364"; // <mask> <server> :<hopcount> <server info>
		public const string LinksReplyEnd =				"365"; // <mask> :End of LINKS list
		public const string BanListReply =				"367"; // <channel> <banmask>
		public const string BanListReplyEnd =			"368"; // <channel> :End of channel ban list
		public const string InfoReply =					"371"; // :<string>
		public const string InfoReplyEnd =				"374"; // :End of INFO list
		public const string MotdStart =					"375"; // :- <server> Message of the day -
		public const string MotdReply =					"372"; // :- <text>
		public const string MotdReplyEnd =				"376"; // :End of MOTD command
		public const string UsersReply =				"393"; // :<username> <ttyline> <hostname>
		public const string UsersReplyEnd =				"394"; // :End of users
		public const string NoUsersReply =				"395"; // :Nobody logged in
		public const string StatsLinkInfoReply =		"211"; // <linkname> <sendq> <sent messages> <sent Kbytes> <received messages> <received Kbytes> <time open>
		public const string StatsCommandsReply =		"212"; // <command> <count> <byte count> <remote count>
		public const string StatsReplyEnd =				"219"; // <stats letter> :End of STATS report
		public const string StatsUptimeReply =			"242"; // :Server Up %d days %d:%02d:%02d
		public const string StatsOnline =				"243"; // O <hostmask> * <name>
		public const string UserModeIsReply =			"221"; // <user mode string>
		public const string ServerListReply =			"234"; // <name> <server> <mask> <type> <hopcount> <info>
		public const string ServerListReplyEnd =		"235"; // <mask> <type> :End of service listing
		public const string TryAgainReply =				"263"; // <command> :Please wait a while and try again.

		/* Error responses */
		public const string ErrorNoSuchNick =			"401"; // <nickname> :No such nick/channel
		public const string ErrorNoSuchServer =			"402"; // <server name> :No such server
		public const string ErrorNoSuchChannel =		"403"; // <channel name> :No such channel
		public const string ErrorCannotSendToChan =		"404"; // <channel name> :Cannot send to channel
		public const string ErrorTooManyChannels =		"405"; // <channel name> :You have joined too many channels
		public const string ErrorWasNoSuchNick =		"406"; // <nickname> :There was no such nickname
		public const string ErrorTooManyTargets =		"407"; // <target> :<error code> recipients. <abort message>
		public const string ErrorNoSuchService =		"408"; // <service name> :No such service
		public const string ErrorNoOrigin =				"409"; // :No origin specified - PING or PONG message missing originator parameter
		public const string ErrorNoRecipient =			"411"; // :No recipient given (<command>)
		public const string ErrorNoTextToSend =			"412"; // :No text to send
		public const string ErrorNoTopLevel =			"413"; // <mask> :No toplevel domain specified
		public const string ErrorWildTopLevel =			"414"; // <mask> :Wildcard in toplevel domain
		public const string ErrorBadMask =				"415"; // <mask> :Bad Server/host mask
		public const string ErrorUnknownCommand =		"421"; // <command> :Unknown command
		public const string ErrorNoMotd =				"422"; // :MOTD File is missing
		public const string ErrorNoAdminInfo =			"423"; // <server> :No administrative info available
		public const string ErrorFileError =			"424"; // :File error doing <file op> on <file>
		public const string ErrorNoNicknameGiven =		"431"; // :No nickname given
		public const string ErrorErreoneousNickname =	"432"; // <nick> :Erroneous nickname
		public const string ErrorNickNameInUse =		"433"; // <nick> :Nickname is already in use
		public const string ErrorNickCollision =		"436"; // <nick> :Nickname collision KILL from <user>@<host>
		public const string ErrorUnavailableResource =	"437"; // <nick/channel> :Nick/channel is temporarily unavailable
		public const string ErrorUserNotInChannel =		"437"; // <nick> <channel> :They aren't on that channel
		public const string ErrorNotOnChannel =			"442"; // <channel> :You're not on that channel
		public const string ErrorUserOnChannel =		"443"; // <user> <channel> :is already on channel
		public const string ErrorNoLogin =				"444"; // <user> :User not logged in
		public const string ErrorUsersDisabled =		"446"; // :USERS has been disabled
		public const string ErrorNotRegistered =		"451"; // :You have not registered
		public const string ErrorNeedMoreParams =		"461"; // <command> :Not enough parameters
		public const string ErrorAlreadyRegistered =	"462"; // :Unauthorized command (already registered)
		public const string ErrorNoPermForHost =		"463"; // :Your host isn't among the privileged
		public const string ErrorPasswordMismatch =		"464"; // :Password incorrect
		public const string ErrorYouAreBanned =			"465"; // :You are banned from this server
		public const string ErrorYouWillBeBanned =		"466"; // - Sent by a server to a user to inform that access to the server will soon be denied.
		public const string ErrorKeyset =				"467"; // <channel> :Channel key already set
		public const string ErrorChannelIsFull =		"471"; // <channel> :Cannot join channel (+l)
		public const string ErrorUnknownMode =			"472"; // <char> :is unknown mode char to me for <channel>
		public const string ErrorInviteOnlyChan =		"473"; // <channel> :Cannot join channel (+i)
		public const string ErrorBannedFromChan =		"747"; // <channel> :Cannot join channel (+b)
		public const string ErrorBadChannelKey =		"475"; // <channel> :Cannot join channel (+k)
		public const string ErrorBadChanMask =			"476"; // <channel> :Bad Channel Mask
		public const string ErrorNoChanModes =			"477"; // <channel> :Channel doesn't support modes
		public const string ErrorBanListFull =			"478"; // <channel> <char> :Channel list is full
		public const string ErrorNoPrivegedes =			"481"; // :Permission Denied- You're not an IRC operator
		public const string ErrorChanopPrivsNeeded =	"482"; // <channel> :You're not channel operator
		public const string ErrorCantKillServer =		"483"; // :You can't kill a server
		public const string ErrorRestricted =			"484"; // :Your connection is restricted
		public const string ErrorUniqopPrivsNeeded =	"485"; // :You're not the original channel operator
		public const string ErrorNoOperHost =			"491"; // :No O-lines for your host
		public const string ErrorUnknownModeFlag =		"501"; // :Unknown MODE flag
		public const string ErrorUsersDontMatch =		"502"; // :Cannot change mode for other user
	}

	public class ProtocolTranslateException : Exception {
		public ProtocolTranslateException(string originalProtocol)
			: base($"ProtocolTranslateException: failed to convert string '{originalProtocol}' to any IrcResponse enum.") {
			OrginalProtocol = originalProtocol;
		}

		public string OrginalProtocol { get; }
	}
}