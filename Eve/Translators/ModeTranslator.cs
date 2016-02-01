using System.Linq;
using Eve.Ref.Irc;

namespace Eve.Translators {
	public class ModeTranslator {
		public static IrcMode TranslateMode(char toTranslate) {
			return Modes.modes.FirstOrDefault(e => e.Mode.Equals(toTranslate));
		}
	}
}