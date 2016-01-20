using System;
using System.Collections.Generic;

namespace Eve.Core.Calculator {
	public partial class Calculator {
		public delegate void CalcVariableDelegate(object sender, EventArgs e);

		public const string AnswerVar = "r";

		public Dictionary<string, double> Variables { get; private set; }

		public event CalcVariableDelegate OnVariableStore;

		private void LoadConstants() {
			Variables = new Dictionary<string, double> {
				{"pi", Math.PI},
				{"e", Math.E},
				{AnswerVar, 0}
			};

			OnVariableStore?.Invoke(this, new EventArgs());
		}

		public void SetVariable(string name, double val) {
			if (Variables.ContainsKey(name))
				Variables[name] = val;
			else
				Variables.Add(name, val);

			OnVariableStore?.Invoke(this, new EventArgs());
		}

		public double GetVariable(string name) {
			return Variables.ContainsKey(name) ? Variables[name] : 0;
		}
	}
}