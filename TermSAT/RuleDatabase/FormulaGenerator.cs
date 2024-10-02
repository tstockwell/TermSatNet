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
     * by the RuleGenerator by only using previously generated canonical formulas to 
     * generate new formulas (because, obviously, non-canonical formulas can be reduced 
     * by previously generated reduction rules).   
     * 
     * @author Ted Stockwell
     */
    public class FormulaGenerator
    {

        readonly FormulaDatabase _database;
        int _currentLength = 0;
        IEnumerator<Formula> _currentIterator;
        readonly int _maxVariableCount;
        readonly int _truthTableSize;
        readonly long _maxTruthTables;


        // a list of all possible formulas of length = 1
        List<Formula> _startingFormulas = new List<Formula>();


        public FormulaGenerator(FormulaDatabase database, int maxVariableCount)
        {
            _database = database;
            _maxVariableCount = maxVariableCount;
            _truthTableSize = 1 << _maxVariableCount;
            _maxTruthTables = 1 << _truthTableSize;

            _startingFormulas.Add(Constant.TRUE);
            _startingFormulas.Add(Constant.FALSE);
            for (int i = 1; i <= maxVariableCount; i++)
            {
                _startingFormulas.Add(Variable.NewVariable(i));
            }
        }

        public Formula GetStartingFormula()
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
                _currentIterator = GetFormulaConstructor(_database, formula);
            }
            _currentIterator.MoveNext();
            return _currentIterator.Current;
        }

        public virtual IEnumerator<Formula> GetFormulaConstructor(FormulaDatabase database, Formula startingFormula)
        {
            return new FormulaConstructor(database, startingFormula);
        }
        public virtual IEnumerator<Formula> GetFormulaConstructor(FormulaDatabase database, int formulaLength)
        {
            return new FormulaConstructor(database, formulaLength);
        }

        public Formula GetNextWellFormedFormula()
        {
            if (!_currentIterator.MoveNext())
            {

                _currentIterator.Dispose();

                IEnumerator<Formula> nextConstructor = null;
                while (nextConstructor == null)
                {
                    _currentLength++;
                    Trace.WriteLine("The formulas lengths have been increased to " + _currentLength);

                    var truthTableCount= _database.CountCanonicalTruthTables();
                    if (_maxTruthTables <= truthTableCount)
                    {
                        var longestPossibleFormula= _database.LengthOfLongestPossibleNonReducibleFormula();
                        if (longestPossibleFormula < _currentLength)
                        {
                            Trace.WriteLine("!!!!!! The Rule Database is Complete !!!");
                            return null;
                        }
                    }

                    IEnumerator<Formula> fc = GetFormulaConstructor(_database, _currentLength);
                    if (fc.MoveNext())
                        nextConstructor = fc;
                }
                _currentIterator = nextConstructor;

            }
            return _currentIterator.Current;
        }

    }

}
