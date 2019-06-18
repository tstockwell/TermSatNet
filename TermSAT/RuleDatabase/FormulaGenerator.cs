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
using System.Diagnostics;
using TermSAT.Formulas;

namespace TermSAT.RuleDatabase
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
     * by the RuleGenerate by only using previously generated canonical formulas to 
     * generate new formulas (because, obviously, non-canonical formulas can be reduced 
     * by previously generated reduction rules).   
     * 
     * @author Ted Stockwell
     */
    public class FormulaGenerator
    {

        readonly RuleDatabase _database;
        int _startingLength = 0;
        int _currentLength = 0;
        IEnumerator<Formula> _currentIterator;

        List<Formula> _startingFormulas = new List<Formula>();


        public FormulaGenerator(RuleDatabase database)
        {
            _database = database;

            _startingFormulas.Add(Constant.FALSE);
            _startingFormulas.Add(Constant.TRUE);
            for (int i = 1; i <= TruthTable.VARIABLE_COUNT; i++)
                _startingFormulas.Add(Variable.newVariable(i));
        }

        public Formula getStartingFormula()
        {
            Formula formula = _database.GetLastGeneratedFormula();
            if (formula == null)
            {
                _currentLength = 1;
                _currentIterator = _startingFormulas.GetEnumerator();
            }
            else
            {
                _currentLength = formula.Length;
                _currentIterator = new FormulaConstructor(_database, formula);
            }
            _currentIterator.MoveNext();
            return _currentIterator.Current;
        }

        public Formula getNextWellFormedFormula()
        {
            if (!_currentIterator.MoveNext())
            {

                _currentIterator.Dispose();

                FormulaConstructor nextConstructor = null;
                while (nextConstructor == null)
                {
                    _currentLength++;
                    Trace.WriteLine("The formulas lengths have been increased to " + _currentLength);

                    if (TruthTable.MAX_TRUTH_TABLES <= _database.CountCanonicalTruthTables())
                        if (_database.LengthOfLongestPossibleNonReducableFormula() < _currentLength)
                        {
                            Trace.WriteLine("!!!!!! The Rule Database is Complete !!!");
                            return null;
                        }

                    FormulaConstructor fc = new FormulaConstructor(_database, _currentLength);
                    if (fc.MoveNext())
                        nextConstructor = fc;
                }
                _currentIterator = nextConstructor;

            }
            return _currentIterator.Current;
        }

    }

}
