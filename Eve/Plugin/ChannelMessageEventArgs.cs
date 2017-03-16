#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

#endregion

namespace Eve.Plugin {
    [Serializable]
    public class ChannelMessageEventArgs : EventArgs {
        // Unused regexs
        // private readonly Regex _argMessageRegex = new Regex(@"^:(?<Arg1>[^\s]+)\s(?<Arg2>[^\s]+)\s(?<Arg3>[^\s]+)\s?:?(?<Arg4>.*)", RegexOptions.Compiled);
        // private static readonly Regex PingRegex = new Regex(@"^PING :(?<Message>.+)", RegexOptions.None);

        // Regex for parsing raw messages
        private static readonly Regex _messageRegex =
            new Regex(@"^:(?<Sender>[^\s]+)\s(?<Type>[^\s]+)\s(?<Recipient>[^\s]+)\s?:?(?<Args>.*)", RegexOptions.Compiled);

        private static readonly Regex _senderRegex = new Regex(
            @"^(?<Nickname>[^\s]+)!(?<Realname>[^\s]+)@(?<Hostname>[^\s]+)",
            RegexOptions.Compiled);

        private static readonly string[] ShortIgnoreList = {
            "nickserv",
            "chanserv"
        };

        public Dictionary<string, string> Tags = new Dictionary<string, string>();

        public ChannelMessageEventArgs(IrcBot bot, string rawData) {
            MainBot = bot;
            RawMessage = rawData;
            Parse(RawMessage);
        }

        public bool IsIRCv3Message { get; private set; }

        public string RawMessage { get; }

        /// <summary>
        ///     Represents whether the realname processed was contained in the specified identifier list (ChanServ, NickServ)
        /// </summary>
        public bool IsRealUser { get; private set; }

        public DateTime Timestamp { get; private set; }

        public IrcBot MainBot;

        public string Nickname { get; private set; }
        public string Realname { get; private set; }
        public string Hostname { get; private set; }
        public string Recipient { get; private set; }
        public string Type { get; set; }
        public string Args { get; private set; }
        public List<string> SplitArgs { get; private set; } = new List<string>();

        private void ParseTagsPrefix(string raw) {
            if (!raw.StartsWith("@")) return;

            IsIRCv3Message = true;

            string fullTagsPrefix = raw.Substring(0, raw.IndexOf(' '));
            string[] primitiveTagsCollection = raw.Split(';');

            foreach (string[] splitPrimitiveTag in primitiveTagsCollection.Select(primitiveTag => primitiveTag.Split('='))) {
                Tags.Add(splitPrimitiveTag[0], splitPrimitiveTag[1] ?? string.Empty);
            }
        }

        public void Parse(string rawData) {
            if (!_messageRegex.IsMatch(rawData)) return;

            ParseTagsPrefix(rawData);



            Timestamp = DateTime.Now;

            // begin parsing message into sections
            Match mVal = _messageRegex.Match(rawData);
            Match sMatch = _senderRegex.Match(mVal.Groups["Sender"].Value);

            // class property setting
            Nickname = mVal.Groups["Sender"].Value;
            Realname = mVal.Groups["Sender"].Value.ToLower();
            Hostname = mVal.Groups["Sender"].Value;
            Type = mVal.Groups["Type"].Value;
            Recipient = mVal.Groups["Recipient"].Value.StartsWith(":")
                ? mVal.Groups["Recipient"].Value.Substring(1)
                : mVal.Groups["Recipient"].Value;

            Args = mVal.Groups["Args"].Value;

            // splits the first 5 sections of the message for parsing
            SplitArgs = Args?.Trim().Split(new[] {' '}, 4).ToList();
            // IsRealUser = false;

            if (!sMatch.Success) return;

            string realname = sMatch.Groups["Realname"].Value;
            Nickname = sMatch.Groups["Nickname"].Value;
            Realname = realname.StartsWith("~") ? realname.Substring(1) : realname;
            Hostname = sMatch.Groups["Hostname"].Value;
            // IsRealUser = !ShortIgnoreList.Contains(Realname.ToLower());
        }
    }
}