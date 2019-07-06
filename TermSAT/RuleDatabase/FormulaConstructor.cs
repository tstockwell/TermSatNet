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
        readonly FormulaDatabase _database;
        readonly int _formulaLength;

        NegationFormulaConstructor _negationConstructor = null;
        ImplicationFormulaConstructor _ifthenConstructor = null;
        int _antecedentLength;

        public Formula Current { get; private set; }

        object IEnumerator.Current { get => Current; }


        /**
         * Creates a FormulaConstructor that will bypass formulas 
         * until after if finds the given starting formula.
         * @param startingFormula
         */
        public FormulaConstructor(FormulaDatabase database, Formula startingFormula)
            : this(database, startingFormula, startingFormula.Length)
        {
        }

        public FormulaConstructor(FormulaDatabase database, int formulaLength)
            : this(database, null, formulaLength)
        {
        }

        private FormulaConstructor(FormulaDatabase database, Formula startingFormula, int formulaLength)
        {

            _database = database;
            _formulaLength = formulaLength;
            _negationConstructor = new NegationFormulaConstructor(_database, _formulaLength);
            _antecedentLength = 1;


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
                    if (_formulaLength - 1 < _antecedentLength)
                        return false;
                    _ifthenConstructor = new ImplicationFormulaConstructor(_database, _formulaLength, _antecedentLength);
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
                    if (_antecedentLength++ < _formulaLength)
                        _ifthenConstructor = new ImplicationFormulaConstructor(_database, _formulaLength, _antecedentLength);
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
        FormulaDatabase _database;
        public int FormulaLength {  get; private set; }

        public Formula Current { get { return Negation.NewNegation(_formulas.Current); } }

        object IEnumerator.Current { get { return Current; } }

        public NegationFormulaConstructor(FormulaDatabase database, int formulaLength)
        {
            FormulaLength = formulaLength;
            _database= database;
            _formulas = _database.FindCanonicalFormulasByLength(FormulaLength - 1).GetEnumerator();
        }

        public bool MoveNext() => _formulas.MoveNext();

        public void Dispose()
        {
            _formulas.Dispose();
            _formulas = null;
        }

        public void Reset() => throw new NotSupportedException();
    }

    class ImplicationFormulaConstructor : IEnumerator<Formula>
    {

        IEnumerator<Formula> antecedentIterator;
        IEnumerator<Formula> _consequents;
        Formula _antecedent = null;
        readonly int _formulaLength;
        FormulaDatabase _database;

        public ImplicationFormulaConstructor(FormulaDatabase database, int formulaLength, int lengthAntecedents)
        {
            _database= database;
            _formulaLength = formulaLength;
            antecedentIterator = _database.FindCanonicalFormulasByLength(lengthAntecedents).GetEnumerator();
        }

        public Formula Current { get { return Implication.NewImplication(_antecedent, _consequents.Current); } }

        object IEnumerator.Current { get { return Current; } }

        public void Dispose()
        {
            if (antecedentIterator != null)
                antecedentIterator.Dispose();
            if (_consequents != null)
                _consequents.Dispose();
            antecedentIterator = null;
            _consequents = null;
            _antecedent = null;
        }

        public bool MoveNext()
        {
            bool StartNewConsequentsEnumerator()
            {
                _consequents?.Dispose();
                _consequents= null;
                var consequentLength= _formulaLength - _antecedent.Length - 1;
                if (0 < consequentLength) {
                    var consequents= _database.FindCanonicalFormulasByLength(consequentLength);
                    if (consequents.Count <= 0)
                        return false;    
                    _consequents = consequents.GetEnumerator();
                    if (!_consequents.MoveNext())
                        return false;
                }
                else
                    return false;
                return true;
            }

            if (_antecedent == null) // start enumerating next antecent
            {
                if (!antecedentIterator.MoveNext()) // no more antecedents
                {
                    Dispose();
                    return false;
                }
                _antecedent= antecedentIterator.Current;
                _consequents?.Dispose();
                _consequents= null;

                if (!StartNewConsequentsEnumerator())
                {
                    Dispose();
                    return false;
                }

                return true;
            }

            else if (!_consequents.MoveNext()) { 
                // go to next antecedent
                _antecedent= null;
                return MoveNext();
            }

            return true;
        }

        public void Reset() => throw new NotSupportedException();
    }


}



