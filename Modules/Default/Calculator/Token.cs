using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Eve.Calculator {
	public partial class Calculator {
		public enum CalcMode {
			Numeric,
			Logic
		}

		public static class Token {
			public const string PLeft = "(",
				PRight = ")",
				Power = "^",
				UnaryMinus = "_",
				Add = "+",
				Subtract = "-",
				Multiply = "*",
				Divide = "/",
				Factorial = "!",
				Mod = "%",
				Sentinel = "#",
				End = ";",
				Store = "=",
				None = " ",
				Seperator = ",";

			public const string Sine = "sin",
				Cosine = "cos",
				Tangent = "tan",
				ASine = "asin",
				ACosine = "acos",
				ATangent = "atan",
				Log = "log",
				Log10 = "log10",
				Ln = "ln",
				Exp = "exp",
				Abs = "abs",
				Sqrt = "sqrt",
				Root = "rt";

			private static readonly string[] BinaryOperators = {
				Multiply, Divide, Subtract, Add,
				Power, Log, Root, Mod
			};

			private static readonly string[] UnaryOperators = {
				Subtract, Sine, Cosine, Tangent,
				ASine, ACosine, ATangent,
				Log10, Ln, Exp, Abs, Sqrt
			};

			private static readonly string[] SpecialOperators = {Sentinel, End, Store, None, Seperator, PRight};

			private static readonly string[] RightSideOperators = {Factorial};

			private static readonly string[] FunctionList = {
				Sine, Cosine, Tangent,
				ASine, ACosine, ATangent,
				Log, Log10, Ln, Exp, Abs,
				Sqrt, Root
			};

			private static readonly string[] LastProcessedOperators = {Power};

			private static int Precedence(string op) {
				if (IsFunction(op)) return 64;

				switch (op) {
					case Subtract:
						return 4;
					case Add:
						return 4;
					case UnaryMinus:
						return 8;
					case Multiply:
						return 16;
					case Divide:
						return 16;
					case Power:
						return 24;
					case Mod:
						return 32;
					case Factorial:
						return 48;
					case PLeft:
						return 64;
					case PRight:
						return 64;

					default:
						return 0; // operators:  END, Sentinel, Store
				}
			}

			public static int Compare(string op1, string op2) {
				if (op1 == op2 && Contains(op1, LastProcessedOperators))
					return -1;
				return Precedence(op1) >= Precedence(op2) ? 1 : -1;
			}

			public static string ConvertOperator(string op) {
				switch (op) {
					case "-":
						return "_";
					default:
						return op;
				}
			}

			public static string ToString(string op) {
				switch (op) {
					case End:
						return "END";
					default:
						return op;
				}
			}

			private static bool Contains(string token, IEnumerable<string> array) {
				return array.Any(s => s == token);
			}

			#region Is... Functions

			public static bool IsBinary(string op) {
				return Contains(op, BinaryOperators);
			}

			public static bool IsUnary(string op) {
				return Contains(op, UnaryOperators);
			}

			public static bool IsRightSide(string op) {
				return Contains(op, RightSideOperators);
			}

			public static bool IsSpecial(string op) {
				return Contains(op, SpecialOperators);
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
		}
	}
}