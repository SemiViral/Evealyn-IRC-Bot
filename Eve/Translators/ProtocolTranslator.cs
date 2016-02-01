using System.Linq;
using Eve.Ref.Irc;

namespace Eve.Translators {
	public static class ProtocolTranslator {
		public static bool CheckTranslateProtocol(string type) {
			return typeof(Protocols).GetFields().Any(field => type.Equals(field.GetValue(null)));
		}
	}
}