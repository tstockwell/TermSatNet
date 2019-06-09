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

namespace TermSAT.RuleDatabase
{
    /**
     * Creates new formulas of a specified length by assembling 
     * the new formulas using previously created canonical formulas in the 
     * database.
     * Let L be the specified length. 
     * This constructor only generates new formulas from smaller canonical formulas,
     * that is, it does not create new formulas using smaller formulas that we already 
     * know are reducible.  
     * 
     * It does this by first creating all possible formulas of length L 
     * that begin with Formula.NEGATION by retrieving all canonical formulas 
     * of L-1 from the formula database and prepending them with
     * Formula.NEGATION.
     * 
     * Then it creates all possible formulas beginning with Formula.IF_THEN 
     * by iterating the length of the 'right-side' formula from L-2 to 1.
     * Let R be the length of the right-side formulas 
     * then 
     * 		Retrieve all canonical formulas of length R from the database.
     * 		Let the number of all such formulas be RCOUNT. 
     * 		Retrieve all canonical formulas of length L - R - 1 from the database.
     * 		Let the number of all such formulas be LCOUNT.
     * 		Create LCOUNT*RCOUNT new formulas by prepending Formula.IF_THEN 
     * 		to all possible combinations of right-side formula and left-side formula. 
     */
    public class FormulaConstructor : IEnumerator<Formula>
    {

        RuleDatabase _database;
        int _formulaLength;

        NegationFormulaConstructor _negationConstructor = null;
        IfThenFormulaConstructor _ifthenConstructor = null;
        int _rightLength;

        public Formula Current { get; private set; }

        object IEnumerator.Current { get => Current; }


        /**
         * Creates a FormulaConstructor that will bypass formulas 
         * until after if finds the given starting formula.
         * @param startingFormula
         */
        public FormulaConstructor(RuleDatabase database, Formula startingFormula)
            : this(database, startingFormula, startingFormula.Length)
        {
        }

        public FormulaConstructor(RuleDatabase database, int formulaLength)
            : this(database, null, formulaLength)
        {
        }

        private FormulaConstructor(RuleDatabase database, Formula startingFormula, int formulaLength)
        {

            _database = database;
            _formulaLength = formulaLength;
            _negationConstructor = new NegationFormulaConstructor(_formulaLength);
            _rightLength = formulaLength - 2;


            // skip formulas until we're at the starting formula
            if (startingFormula != null)
            {
                while (Current.Equals(startingFormula) == false)
                {
                    System.Diagnostics.Trace.WriteLine("Skipping formula:" + Current);
                    MoveNext();
                }
            }
        }

        public bool MoveNext()
        {
            while (true)
            {
                if (_negationConstructor != null)
                {
                    if (_negationConstructor.MoveNext())
                    {
                        this.Current = _negationConstructor.Current;
                        return true;
                    }
                    _negationConstructor.Dispose();
                    _negationConstructor = null;
                    if (_rightLength < 1)
                        return false;
                    _ifthenConstructor = new IfThenFormulaConstructor(_formulaLength, _rightLength);
                }
                else
                {
                    if (_ifthenConstructor == null)
                        return false;
                    if (_ifthenConstructor.MoveNext())
                    {
                        this.Current = _ifthenConstructor.Current;
                        return true;
                    }
                    _ifthenConstructor.Dispose();
                    _ifthenConstructor = null;
                    if (0 < --_rightLength)
                        _ifthenConstructor = new IfThenFormulaConstructor(_formulaLength, _rightLength);
                }
            }
        }

        public void Reset()
        {
            throw new NotSupportedException();
        }

        public void Dispose()
        {
            if (_negationConstructor != null)
            {
                try { _negationConstructor.Dispose(); } catch { }
                _negationConstructor = null;
            }
            if (_ifthenConstructor != null)
            {
                try { _ifthenConstructor.Dispose(); } catch { }
                _negationConstructor = null;
            }
        }
    }


    class NegationFormulaConstructor : IEnumerator<Formula>
    {

        IEnumerator<Formula> _formulas;
        int _formulaLength;

        public Formula Current { get { return _formulas.Current; } }

        object IEnumerator.Current { get { return Current; } }

        public NegationFormulaConstructor(int formulaLength)
        {
            _formulaLength = formulaLength;
            _formulas = _database.findCanonicalFormulasByLength(_formulaLength - 1).GetEnumerator();
        }

        public bool MoveNext()
        {
            return _formulas.MoveNext();
        }

        public void Reset()
        {
            throw new NotSupportedException();
        }

        public void Dispose()
        {
            _formulas.Dispose();
            _formulas = null;
        }
    }

    class IfThenFormulaConstructor : IEnumerator<Formula>
    {

        IEnumerator<Formula> _rightIterator;
        IEnumerator<Formula> _consequents;
        Formula _antecedent = null;
        int _formulaLength;
        public IfThenFormulaConstructor(int formulaLength, int lengthOfRightSideFormulas)
        {
            _formulaLength = formulaLength;
            _rightIterator = _database.findCanonicalFormulasByLength(lengthOfRightSideFormulas).ToEnumerator();
            _consequents = _database.findCanonicalFormulasByLength(_formulaLength - lengthOfRightSideFormulas - 1).ToEnumerator();
            if (!_rightIterator.MoveNext())
            {
                Dispose();
            }
            else
                _antecedent = _rightIterator.Current;
        }

        public Formula Current { get { return Implication.newImplication(_antecedent, _consequents.Current); } }

        object IEnumerator.Current { get { return Current; } }

        public void Dispose()
        {
            if (_rightIterator != null)
                _rightIterator.Dispose();
            if (_consequents != null)
                _consequents.Dispose();
            _rightIterator = null;
            _consequents = null;
            _antecedent = null;
        }

        public bool MoveNext()
        {
            if (_antecedent == null)
                return false;
            if (_consequents.MoveNext())
                return true;
            if (!_rightIterator.MoveNext())
            {
                Dispose();
                return false;
            }
            _antecedent = _rightIterator.Current;
            _consequents.Dispose();
            _consequents = _database.findCanonicalFormulasByLength(_formulaLength - _antecedent.Length - 1).ToEnumerator();
            return true;
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }
    }


}



