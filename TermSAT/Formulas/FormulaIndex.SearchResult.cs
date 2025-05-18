using System.Collections.Generic;

namespace TermSAT.Formulas;


public partial class FormulaIndex
{
    public class SearchResult
    {
        public Node Node { get; }
        public IReadOnlyDictionary<Variable, Formula> Substitutions => _substitutions;
        private Dictionary<Variable, Formula> _substitutions = new ();

        public SearchResult(Node node, IDictionary<int, Formula> substitutions)
        {
            Node = node;
            foreach (var substitution in substitutions)
            {
                _substitutions.Add(Variable.NewVariable(substitution.Key), substitution.Value);
            }
        }
    }

}
