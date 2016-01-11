﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Eve.Calculator {
	public partial class Calculator : IModule {
		private Stack<double> _operands;
		private Stack<string> _operators;

		private string _token;
		private int _tokenPos;
		private string _expression;

		public Dictionary<String, String> Def => new Dictionary<string, string> {
			{ "eval", "(<expression>) — evaluates given mathematical expression." }
		};

		public Calculator() {
			Reset();
		}

		public void Reset() {
			LoadConstants();
			Clear();
		}

		public void Clear() {
			_operands = new Stack<double>();
			_operators = new Stack<string>();

			_operators.Push(Token.Sentinel);
			_token = Token.None;
			_tokenPos = -1;
		}

		public double Evaluate(string expr) {
			Clear();
			_expression = expr;

			if (Normalize(ref _expression)) {
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
			return _operands.Peek();
		}

		private void ParseBinary() {
			ParsePrimary();

			while (Token.IsBinary(_token)) {
				PushOperator(_token);
				NextToken();
				ParsePrimary();
			}

			while (_operators.Peek() != Token.Sentinel)
				PopOperator();
		}

		private void ParsePrimary() {
			if (Token.IsDigit(_token))
				ParseDigit();
			else if (Token.IsName(_token))
				ParseName();
			else if (Token.IsUnary(_token)) {
				PushOperator(Token.ConvertOperator(_token));
				NextToken();
				ParsePrimary();
			} else if (_token == Token.PLeft) {
				NextToken();
				_operators.Push(Token.Sentinel);
				ParseBinary();
				Expect(Token.PRight, Token.Seperator);
				_operators.Pop();

				TryInsertMultiply();
				TryRightSideOperator();
			} else if (_token == Token.Seperator) {
				NextToken();
				ParsePrimary();
			} else
				ThrowException("Syntax error.");
		}

		private void ParseDigit() {
			StringBuilder tmpNumber = new StringBuilder();

			while (Token.IsDigit(_token)) {
				CollectToken(ref tmpNumber);
			}

			_operands.Push(double.Parse(tmpNumber.ToString(), CultureInfo.InvariantCulture));
			TryInsertMultiply();
			TryRightSideOperator();
		}

		private void ParseName() {
			StringBuilder tmpName = new StringBuilder();

			while (Token.IsName(_token))
				CollectToken(ref tmpName);

			string name = tmpName.ToString();

			if (Token.IsFunction(name)) {
				PushOperator(name);
				ParsePrimary();
			} else {
				if (_token == Token.Store) {
					NextToken();
					SetVariable(name, Parse());
				} else {
					_operands.Push(GetVariable(name));
					TryInsertMultiply();
					TryRightSideOperator();
				}
			}
		}

		private void TryInsertMultiply() {
			if (!Token.IsBinary(_token)
				&& !Token.IsSpecial(_token)
				&& !Token.IsRightSide(_token)) {
					PushOperator(Token.Multiply);
					ParsePrimary();
				}
		}

		private void TryRightSideOperator() {
			switch (_token) {
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
				_operators.Push(op);
				return;
			}

			while (Token.Compare(_operators.Peek(), op) > 0)
				PopOperator();

			_operators.Push(op);
		}

		private void PopOperator() {
			if (Token.IsBinary(_operators.Peek())) {
				double o2 = _operands.Pop();
				double o1 = _operands.Pop();
				Calculate(_operators.Pop(), o1, o2);
			} else
				Calculate(_operators.Pop(), _operands.Pop());
		}

		private void NextToken() {
			if (_token != Token.End)
				_token = _expression[++_tokenPos].ToString();
		}

		private void CollectToken(ref StringBuilder sb) {
			sb.Append(_token);
			NextToken();
		}

		private void Expect(params string[] expectedTokens)
		{
			if (expectedTokens.Any(t => _token == t))
			{
				NextToken();
				return;
			}

			ThrowException($"Syntax error: {Token.ToString(expectedTokens[0])} expected.");
		}

		private bool Normalize(ref string s) {
			s = s.Replace(" ", "").Replace("\t", " ").ToLower() + Token.End;

			if (s.Length < 2) return false;

			NextToken();
			return true;
		}

		private void ThrowException(string message) {
			Console.WriteLine(_token);
			Console.WriteLine(_operands.ToString());
			Console.WriteLine(_operators.ToString());
			throw new CalculateException(message, _tokenPos);
		}

		public ChannelMessage OnChannelMessage(ChannelMessage c) {
			ChannelMessage o = new ChannelMessage {
				Type = "PRIVMSG",
				Nickname = c.Recipient,
				Args = String.Empty
			};

			if (c._Args[0].Replace(",", string.Empty) != "eve"
				|| c._Args.Count < 2
				|| !c._Args[1].CaseEquals("eval"))
				return o;

			if (c._Args.Count < 3) {
				o.Args = "Not enough parameters.";
			}

			string evalArgs = (c._Args.Count > 3) ?
				c._Args[2] + c._Args[3] : c._Args[2];

			try {
				o.Args = Evaluate(evalArgs).ToString(CultureInfo.CurrentCulture);
			} catch (Exception e) {
				o.Args = e.Message;
			}
			
			return o;
		}
	}

	public class CalculateException : Exception {
		public CalculateException(string message, int position)
			: base($"Error at position: {position}, {message}") {
			TokenPosition = position;
		}

		public int TokenPosition { get; }
	}
}
