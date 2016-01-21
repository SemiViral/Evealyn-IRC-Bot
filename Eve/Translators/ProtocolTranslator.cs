using System.Linq;
using Eve.Types.Irc;

namespace Eve.Translators {
	public static class ProtocolTranslator {
		public static bool CheckTranslateProtocol(string type) {
			return typeof(IrcProtocol).GetFields().Any(field => type.Equals(field.GetValue(null)));
		}
	}
}