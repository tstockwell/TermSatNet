using System.Collections.Generic;
using TermSAT.Formulas;
using TermSAT.RuleDatabase;

namespace TermSAT.NandReduction;


/**
 * This class enumerates all possible formulas over a set of variables, starting with the shortest 
 * formulas and then generating longer formulas.
 * This class is used by the RuleGenerator application to generate reduction rules.
 * As formulas are generated they are saved in a database along with their truth table, length, and 
 * a flag that indicates whether the formula is canonical.
 * 
 * Then, this class can create new formulas by assembling formulas from previously 
 * created canonical formulas in the formula database.
 * 
 * This program greatly reduces the number of formulas that need to be considered 
 * by the RuleGenerator by only using previously generated canonical formulas to 
 * generate new formulas (because, obviously, non-canonical formulas can be reduced 
 * by previously generated reduction rules).   
 * 
 * @author Ted Stockwell
 */
public class NandFormulaGenerator : FormulaGenerator
{
    public NandFormulaGenerator(FormulaDatabase database, int maxVariableCount)
        : base(database, maxVariableCount)
    {
    }

    public override IEnumerator<Formula> GetFormulaConstructor(FormulaDatabase database, Formula startingFormula)
    {
        var formulas = new NandFormulaConstructor(database, startingFormula.Length);


        // skip formulas until we're at the starting formula
        if (startingFormula != null)
        {
            if (formulas.MoveNext())
            {
                while (formulas.Current.Equals(startingFormula) == false)
                {
                    System.Diagnostics.Trace.WriteLine("Skipping formula:" + formulas.Current);
                    if (!formulas.MoveNext())
                    {
                        break;
                    }
                }
            }
        }

        return formulas;
    }

    public override IEnumerator<Formula> GetFormulaConstructor(FormulaDatabase database, int formulaLength)
    {
        return new NandFormulaConstructor(database, formulaLength);
    }

}
