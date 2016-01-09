using System.Text.RegularExpressions;

namespace Eve.Calculator {
	public partial class Calculator {
		public enum CalcMode { Numeric, Logic };

		public static class Token {
			public const string PLeft = "(", PRight = ")", Power = "^", UnaryMinus = "_",
								Add = "+", Subtract = "-", Multiply = "*", Divide = "/",
								Factorial = "!", Mod = "%",
								Sentinel = "#", End = ";", Store = "=", None = " ",
								Seperator = ",";

			public const string Sine = "sin", Cosine = "cos", Tangent = "tan",
								ASine = "asin", ACosine = "acos", ATangent = "atan",
								Log = "log", Log10 = "log10", Ln = "ln", Exp = "exp",
								Abs = "abs", Sqrt = "sqrt", Root = "rt";

			static string[] binaryOperators = new string[] { Multiply, Divide, Subtract, Add,
														 Power, Log, Root, Mod };
			static string[] unaryOperators = new string[] { Subtract, Sine, Cosine, Tangent,
														ASine, ACosine, ATangent,
														Log10, Ln, Exp, Abs, Sqrt };

			static string[] specialOperators = new string[] { Sentinel, End, Store, None, Seperator, PRight };

			static string[] rightSideOperators = new string[] { Factorial };

			static string[] FunctionList = new string[] { Sine, Cosine, Tangent,
													  ASine, ACosine, ATangent,
													  Log, Log10, Ln, Exp, Abs,
													  Sqrt, Root };

			static string[] lastProcessedOperators = new string[] { Power };

			private static int Precedence(string op) {
				if (Token.IsFunction(op)) return 64;

				switch (op) {
					case Subtract: return 4;
					case Add: return 4;
					case UnaryMinus: return 8;
					case Multiply: return 16;
					case Divide: return 16;
					case Power: return 24;
					case Mod: return 32;
					case Factorial: return 48;
					case PLeft: return 64;
					case PRight: return 64;

					default: return 0; // operators:  END, Sentinel, Store
				}
			}

			public static int Compare(string op1, string op2) {
				if (op1 == op2 && Contains(op1, lastProcessedOperators))
					return -1;
				else
					return Precedence(op1) >= Precedence(op2) ? 1 : -1;
			}

			#region Is... Functions
			public static bool IsBinary(string op) {
				return Contains(op, binaryOperators);
			}

			public static bool IsUnary(string op) {
				return Contains(op, unaryOperators);
			}

			public static bool IsRightSide(string op) {
				return Contains(op, rightSideOperators);
			}

			public static bool IsSpecial(string op) {
				return Contains(op, specialOperators);
			}

			public static bool IsFunction(string op) {
				return Contains(op, FunctionList);
			}

			public static bool IsName(string token) {
				return Regex.IsMatch(token, @"[a-zA-Z0-9]");
			}

			public static bool IsDigit(string token) {
				return Regex.IsMatch(token, @"\d|\.");
			}
			#endregion

			public static string ConvertOperator(string op) {
				switch (op) {
					case "-": return "_";
					default: return op;
				}
			}

			public static string ToString(string op) {
				switch (op) {
					case End: return "END";
					default: return op.ToString();
				}
			}

			static bool Contains(string token, string[] array) {
				foreach (string s in array)
					if (s == token) return true;

				return false;
			}
		}
	}
}
