using System.Threading.Tasks;
using TermSAT.Formulas;

namespace TermSAT.RuleDatabase;

public class ReductionRule
{
    public Decimal FromId { get; private set; }
    public FormulaRecord FromFormula {  get; private set; }
    public int FromPosition { get; private set; }

    public FormulaRecord To {  get; private set; }

    public ReductionRule(Formula formula, Formula reduction)
    {
        this.Formula = formula;
        this.Reduction = reduction;
    }

    override public string ToString()
    {
        return Formula.ToString() + " ==> " + Reduction.ToString();
    }
}


	public static class ReductionRuleUtilities
{

    /**
     * Returns a formula that is reduced by applying the given rule.
     * This method examines subterms of the given formula.
     * This method only applies the reduction rule once, it doesn't apply it as many times as 
     * possible.
     * @return A reduced formula.  Returns null if the given formula can't be reduced. 
     */
    public static async Task<Formula> ReduceUsingRule(this Formula formula, ReductionRule rule)
    {
        Formula reducedFormula;

        /// variable and constants can't be reduced
        if (formula is Variable || formula is Constant)
            return null;

        //
        // reduce subformulas before reducing this formula
        //
        if (formula is Negation negation)
        {
            Formula negated = negation.Child;
            Formula n = await ReduceUsingRule(negated, rule);
            if (n != null)
            {
                reducedFormula = Negation.NewNegation(n);
                return reducedFormula;
            }
        }
        else if (formula is Implication implication)
        {
            Formula antecent = implication.Antecedent;
            Formula consequent = implication.Consequent;
            Formula a = await ReduceUsingRule(antecent, rule);
            if (a != null)
            {
                reducedFormula = Implication.NewImplication(a, consequent);
                return reducedFormula;
            }
            Formula c = await ReduceUsingRule(consequent, rule);
            if (c != null)
            {
                reducedFormula = Implication.NewImplication(antecent, c);
                return reducedFormula;
            }
        }


        // check if given formula is a substitution instance of the reduction rules formula
        InstanceRecognizer instanceRecognizer = new InstanceRecognizer { rule.Formula };
        var matches = instanceRecognizer.FindGeneralizationNodes(formula, 1);
        if (matches == null)
            return null;

        // if formula and rule formula match then create reduced formula
        var info = matches[0];
        reducedFormula = rule.Reduction.CreateSubstitutionInstance(info.Substitutions);
        return reducedFormula;
    }



}
