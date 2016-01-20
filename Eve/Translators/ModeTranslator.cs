using System.Linq;
using Eve.Types.Irc;

namespace Eve.Translators {
	public class ModeTranslator {
		public static IrcMode TranslateMode(char toTranslate) {
			return IrcModes.modes.FirstOrDefault(e => e.Mode.Equals(toTranslate));
		}
	}
}