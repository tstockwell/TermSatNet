using System.Collections.Generic;

namespace TermSAT.Formulas;


public partial class FormulaIndex
{
    public class SearchResult
    {
        public Node Node { get; }
        public IDictionary<int, Formula> Substitutions { get; }
        public SearchResult(Node node, IDictionary<int, Formula> substitutions)
        {
            Node = node;
            Substitutions = substitutions;
        }
    }

}
