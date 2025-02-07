using System.Linq;

namespace TermSAT.RuleDatabase;

public static class FormulaRecordExtensions
{
    public static IQueryable<FormulaRecord> InFormulaOrder(this IQueryable<FormulaRecord> dbset) 
        => dbset.OrderBy(_ => _.VarOrder).ThenBy(_ => _.Length).ThenBy(_ => _.Text);
}