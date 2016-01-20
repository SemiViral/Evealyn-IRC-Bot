using System;

namespace Eve.Core.Calculator {
	public partial class Calculator {
		private void Calculate(string op, double op1, double op2) {
			double res = 0;

			try {
				switch (op) {
					case Token.Add:			res = op1 + op2; break;
					case Token.Subtract:	res = op1 - op2; break;
					case Token.Multiply:	res = op1 * op2; break;
					case Token.Divide:		res = op1 / op2; break;
					case Token.Mod:			res = op1 % op2; break;
					case Token.Power:		res = Math.Pow(op1, op2); break;
					case Token.Log:			res = Math.Log(op2, op1); break;
					case Token.Root:		res = Math.Pow(op2, 1 / op1); break;
				}

				_operands.Push(PostProcess(res));
			} catch (Exception e) {
				ThrowException(e.Message);
			}
		}

		private void Calculate(string op, double operand) {
			double res = 1;

			try {
				switch (op) {
					case Token.UnaryMinus:	res = -operand; break;
					case Token.Abs:			res = Math.Abs(operand); break;
					case Token.ACosine:		res = Math.Acos(operand); break;
					case Token.ASine:		res = Math.Asin(operand); break;
					case Token.ATangent:	res = Math.Atan(operand); break;
					case Token.Cosine:		res = Math.Cos(operand); break;
					case Token.Sine:		res = Math.Sin(operand); break;
					case Token.Tangent:		res = Math.Tan(operand); break;
					case Token.Ln:			res = Math.Log(operand); break;
					case Token.Log10:		res = Math.Log10(operand); break;
					case Token.Sqrt:		res = Math.Sqrt(operand); break;
					case Token.Exp:			res = Math.Exp(operand); break;
					case Token.Factorial:	for (int i = 2; i <= (int)operand; res *= i++);
						break;
				}

				_operands.Push(PostProcess(res));
			} catch (Exception e) {
				ThrowException(e.Message);
			}
		}

		private double PostProcess(double result) {
			return Math.Round(result, 10);
		}
	}
}
