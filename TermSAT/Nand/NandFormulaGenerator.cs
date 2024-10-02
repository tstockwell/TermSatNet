/*******************************************************************************
 * termsat SAT solver
 *     Copyright (C) 2010 Ted Stockwell <emorning@yahoo.com>
 * 
 *     This program is free software: you can redistribute it and/or modify
 *     it under the terms of the GNU Affero General Public License as
 *     published by the Free Software Foundation, either version 3 of the
 *     License, or (at your option) any later version.
 * 
 *     This program is distributed in the hope that it will be useful,
 *     but WITHOUT ANY WARRANTY; without even the implied warranty of
 *     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *     GNU Affero General Public License for more details.
 * 
 *     You should have received a copy of the GNU Affero General Public License
 *     along with this program.  If not, see <http://www.gnu.org/licenses/>.
 ******************************************************************************/

using System.Collections.Generic;
using TermSAT.Formulas;
using TermSAT.RuleDatabase;

namespace TermSAT.Nand
{

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

}
