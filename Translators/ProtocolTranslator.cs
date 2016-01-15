using System;
using Eve.Data.Protocols;

namespace Eve.Translators {
	public static class ProtocolTranslator {
		public static IrcResponse TranslateStringToEnum(string protocol) {
			IrcResponse type;

			Enum.TryParse(protocol, true, out type);

			if (type.Equals(IrcResponse.Default))
				throw new ProtocolTranslateException(protocol);

			return type;
		}
	}
}
