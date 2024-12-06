using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using TermSAT.Formulas;
using TermSAT.RuleDatabase;

namespace TermSAT.NandReduction
{
    public static class NandFormulaEnumerations
    {
        public static Formula FormulaById(this RuleDatabaseContext ctx, int id) => 
            ctx.FormulaRecords
            .AsNoTracking()
            .Where(_ => _.Id == id)
            .Select(_ => Formula.Parse(_.Text))
            .First();

        public static IQueryable<FormulaRecord> FormulaRecords(this RuleDatabaseContext ctx, int id = 0, int length= 0, int varCount= 0, TruthTable truthValue= null)
        {
            IQueryable<FormulaRecord> records = ctx.FormulaRecords;
            if (0 < length)
            {
                records = records.Where(_ => _.Length == length);
            }
            if (0 < varCount)
            {
                records = records.Where(_ => _.VarCount == varCount);
            }
            if (truthValue != null)
            {
                records = records.Where(_ => _.TruthValue == truthValue.ToString());
            }
            if (0 < id)
            {
                records = records.Where(_ => _.Id == id);
            }

            return records;
        }
    }

}



