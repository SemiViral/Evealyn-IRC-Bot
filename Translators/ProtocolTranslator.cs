using System;
using System.Linq;
using Eve.Data.Protocols;

namespace Eve.Translators {
	public static class ProtocolTranslator {

		public static bool CheckTranslateProtocol(string type) {
			return typeof(IrcProtocol).GetFields().Any(field => type.Equals(field.GetValue(null)));
		}
	}
}
