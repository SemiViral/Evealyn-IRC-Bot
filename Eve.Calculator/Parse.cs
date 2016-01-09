using System;
using System.Collections.Generic;
using System.Text;

namespace Eve.Calculator {
	public partial class Calculator : IModule {
		Stack<double> operands;
		Stack<string> operators;

		string token;
		int tokenPos;
		string expression;

		public Dictionary<String, String> def {
			get {
				return new Dictionary<string, string> {
					{ "eval", "(<expression>) — evaluates given mathematical expression." }
				};
			}
		}

		public Calculator() {
			Reset();
		}

		public void Reset() {
			LoadConstants();
			Clear();
		}

		public void Clear() {
			operands = new Stack<double>();
			operators = new Stack<string>();

			operators.Push(Token.Sentinel);
			token = Token.None;
			tokenPos = -1;
		}

		public double Evaluate(string expr) {
			Clear();
			expression = expr;
			Console.WriteLine(expression);
			if (Normalize(ref expression)) {
				double result = Parse();
				SetVariable(AnswerVar, result);
				return result;
			} else {
				ThrowException("Blank input expression.");
				return 0;
			}
		}

		private double Parse() {
			ParseBinary();
			Expect(Token.End);
			return operands.Peek();
		}

		private void ParseBinary() {
			ParsePrimary();

			while (Token.IsBinary(token)) {
				PushOperator(token);
				NextToken();
				ParsePrimary();
			}

			while (operators.Peek() != Token.Sentinel)
				PopOperator();
		}

		private void ParsePrimary() {
			if (Token.IsDigit(token))
				ParseDigit();
			else if (Token.IsName(token))
				ParseName();
			else if (Token.IsUnary(token)) {
				PushOperator(Token.ConvertOperator(token));
				NextToken();
				ParsePrimary();
			} else if (token == Token.PLeft) {
				NextToken();
				operators.Push(Token.Sentinel);
				ParseBinary();
				Expect(Token.PRight, Token.Seperator);
				operators.Pop();

				TryInsertMultiply();
				TryRightSideOperator();
			} else if (token == Token.Seperator) {
				NextToken();
				ParsePrimary();
			} else
				ThrowException("Syntax error.");
		}

		private void ParseDigit() {
			StringBuilder tmpNumber = new StringBuilder();

			while (Token.IsDigit(token)) {
				CollectToken(ref tmpNumber);
			}

			operands.Push(double.Parse(tmpNumber.ToString(), System.Globalization.CultureInfo.InvariantCulture));
			TryInsertMultiply();
			TryRightSideOperator();
		}

		private void ParseName() {
			StringBuilder tmpName = new StringBuilder();

			while (Token.IsName(token))
				CollectToken(ref tmpName);

			string name = tmpName.ToString();

			if (Token.IsFunction(name)) {
				PushOperator(name);
				ParsePrimary();
			} else {
				if (token == Token.Store) {
					NextToken();
					SetVariable(name, Parse());
				} else {
					operands.Push(GetVariable(name));
					TryInsertMultiply();
					TryRightSideOperator();
				}
			}
		}

		private void TryInsertMultiply() {
			if (!Token.IsBinary(token)
				&& !Token.IsSpecial(token)
				&& !Token.IsRightSide(token)) {
					PushOperator(Token.Multiply);
					ParsePrimary();
				}
		}

		private void TryRightSideOperator() {
			switch (token) {
				case Token.Factorial:
					PushOperator(Token.Factorial);
					NextToken();
					TryInsertMultiply();
					break;
				case Token.Seperator:
					ParsePrimary();
					break;
			}
		}

		private void PushOperator(string op) {
			if (Token.UnaryMinus == op) {
				operators.Push(op);
				return;
			}

			while (Token.Compare(operators.Peek(), op) > 0)
				PopOperator();

			operators.Push(op);
		}

		private void PopOperator() {
			if (Token.IsBinary(operators.Peek())) {
				double o2 = operands.Pop();
				double o1 = operands.Pop();
				Calculate(operators.Pop(), o1, o2);
			} else
				Calculate(operators.Pop(), operands.Pop());
		}

		private void NextToken() {
			if (token != Token.End)
				token = expression[++tokenPos].ToString();
		}

		private void CollectToken(ref StringBuilder sb) {
			sb.Append(token);
			NextToken();
		}

		private void Expect(params string[] expectedTokens) {
			for (int i = 0; i < expectedTokens.Length; i++)
				if (token == expectedTokens[i]) {
					NextToken();
					return;
				}

			ThrowException($"Syntax error: {Token.ToString(expectedTokens[0])} expected.");
		}

		private bool Normalize(ref string s) {
			s = s.Replace(" ", "").Replace("\t", " ").ToLower() + Token.End;

			if (s.Length >= 2) {
				NextToken();
				return true;
			}

			return false;
		}

		private void ThrowException(string message) {
			Console.WriteLine(token);
			Console.WriteLine(operands.ToString());
			Console.WriteLine(operators.ToString());
			throw new CalculateException(message, tokenPos);
		}

		public ChannelMessage OnChannelMessage(ChannelMessage c) {
			ChannelMessage o = new ChannelMessage {
				Type = "PRIVMSG",
				Nickname = c.Recipient,
				Args = null
			};

			if (c._Args[0].Replace(",", string.Empty) != "eve"
				|| c._Args.Count < 2
				|| c._Args[1] != "eval")
				return null;
			
			if (c._Args.Count < 3) {
				o.Args = "Not enough parameters.";
			}

			string evalArgs = (c._Args.Count > 3) ?
				c._Args[2] + c._Args[3] : c._Args[2];
			try {
				o.Args = Evaluate(evalArgs).ToString();
			} catch (Exception e) {
				o.Args = e.Message;
			}
			
			return o;
		}
	}

	public class CalculateException : Exception {
		int position;

		public CalculateException(string message, int position)
			: base($"Error at position: {position.ToString()}, {message}") {
			this.position = position;
		}

		public int TokenPosition {
			get { return position; }
		}
	}
}
