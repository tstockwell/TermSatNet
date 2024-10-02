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

using System;
using System.Collections;
using System.Collections.Generic;
using TermSAT.Formulas;
using TermSAT.RuleDatabase;

namespace TermSAT.Nand
{
    /**
     * Like TermSAT.RuleDatabase.FormulaConstructor but for a NAND-based propositional system.
     * That is, NandForumlaConstructor generates formulas that use just the nand operator instead of 
     * the implication and negation operators.
     * 
     * Creates new formulas of a specified length by assembling 
     * the new formulas using previously created canonical formulas in the 
     * database.
     * Let L be the specified length. 
     * This constructor only generates new formulas from smaller canonical formulas,
     * that is, it does not create new formulas using smaller formulas that we already 
     * know are reducible.  
     * 
     * It does this by creating all possible formulas beginning with Formula.NAND 
     * by iterating the length of the 'right-side' formula from L-2 to 1.
     * Let R be the length of the right-side formulas 
     * then 
     * 		Retrieve all canonical formulas of length R from the database.
     * 		Let the number of all such formulas be RCOUNT. 
     * 		Retrieve all canonical formulas of length L - R - 1 from the database.
     * 		Let the number of all such formulas be LCOUNT.
     * 		Create LCOUNT*RCOUNT new formulas by prepending Formula.NAND 
     * 		to all possible combinations of right-side formula and left-side formula. 
     */
    public class NandFormulaConstructor : IEnumerator<Formula>
    {
        readonly int _formulaLength;
        FormulaDatabase _database;

        IEnumerator<Formula> _antecedentIterator;
        int _antecedentLength;
        Formula _currentAntecedent = null;

        IEnumerator<Formula> _consequentIterator;

        public NandFormulaConstructor(FormulaDatabase database, int formulaLength)
        {
            _database= database;
            _formulaLength = formulaLength;
            _antecedentLength = 1;
            _antecedentIterator = _database.FindCanonicalFormulasByLength(_antecedentLength).GetEnumerator();
        }

        public Formula Current { get { return Formulas.Nand.NewNand(_currentAntecedent, _consequentIterator.Current); } }

        object IEnumerator.Current { get { return Current; } }

        public void Dispose()
        {
            if (_antecedentIterator != null)
                _antecedentIterator.Dispose();
            if (_consequentIterator != null)
                _consequentIterator.Dispose();
            _antecedentIterator = null;
            _consequentIterator = null;
            _currentAntecedent = null;
        }

        public bool MoveNext()
        {
            bool StartNewConsequentsEnumerator()
            {
                _consequentIterator?.Dispose();
                _consequentIterator= null;
                var consequentLength = _formulaLength - _currentAntecedent.Length - 1;
                if (0 < consequentLength)
                {
                    var consequents = _database.FindCanonicalFormulasByLength(consequentLength);
                    if (consequents.Count <= 0)
                        return false;
                    _consequentIterator = consequents.GetEnumerator();
                    if (!_consequentIterator.MoveNext())
                        return false;
                }
                else
                    return false;
                return true;
            }

            if (_currentAntecedent == null) // start enumerating next antecedent
            {
                while (!_antecedentIterator.MoveNext()) // no more antecedents of length == _antecedentLength
                {
                    if (_formulaLength - 1 <=  _antecedentLength)
                    {
                        Dispose();
                        return false;
                    }
                    _antecedentLength++;
                    _antecedentIterator = _database.FindCanonicalFormulasByLength(_antecedentLength).GetEnumerator();
                }

                _currentAntecedent= _antecedentIterator.Current;
                _consequentIterator?.Dispose();
                _consequentIterator= null;

                if (!StartNewConsequentsEnumerator())
                {
                    Dispose();
                    return false;
                }

                return true;
            }

            else if (!_consequentIterator.MoveNext())
            {
                // go to next antecedent
                _currentAntecedent= null;
                return MoveNext();
            }

            return true;
        }

        public void Reset() => throw new NotSupportedException();


    }


}



