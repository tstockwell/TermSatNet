using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace TermSAT.Formulas
{
    /// <summary>
    /// A valuation is an assignment of boolean values to variables.
    /// Valuations are immutable, the Add method returns a new valuation.
    /// </summary>
    public class Valuation
    {
        IDictionary<Variable, bool> values = new Dictionary<Variable, bool>();

        public Valuation(IDictionary<Variable, bool> values)
        {
            foreach (var pair in values)
            {
                this.values[pair.Key] = pair.Value;
            }
        }
        public Valuation() { }

        public bool this[Variable key] { get { return this.values[key]; } }

        public ICollection<Variable> Variables { get { return this.values.Keys; } }

        public int Count { get { return values.Count; } }

        public bool IsReadOnly { get { return true; } }

        public Valuation Add(Variable key, bool value)
        {
            var v = new Valuation(this.values);
            v.values[key] = value;
            return v;
        }

        public bool ContainsVariable(Variable variable)
        {
            return values.ContainsKey(variable);
        }

    }
}
