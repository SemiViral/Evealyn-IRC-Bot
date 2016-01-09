using System;
using System.Collections.Generic;

namespace Eve.Calculator {
	public partial class Calculator {
		public delegate void CalcVariableDelegate(object sender, EventArgs e);
		public event CalcVariableDelegate OnVariableStore;

		Dictionary<string, double> variables;

		public const string AnswerVar = "r";

		private void LoadConstants() {
			variables = new Dictionary<string, double>();
			variables.Add("pi", Math.PI);
			variables.Add("e", Math.E);
			variables.Add(AnswerVar, 0);

			if (OnVariableStore != null)
				OnVariableStore(this, new EventArgs());
		}

		public Dictionary<string, double> Variables {
			get { return variables; }
		}

		public void SetVariable(string name, double val) {
			if (variables.ContainsKey(name))
				variables[name] = val;
			else
				variables.Add(name, val);

			if (OnVariableStore != null)
				OnVariableStore(this, new EventArgs());
		}

		public double GetVariable(string name) {
			return variables.ContainsKey(name) ? variables[name] : 0;
		}
	}
}
