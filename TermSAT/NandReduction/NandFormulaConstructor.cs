using System;
using System.Collections;
using System.Collections.Generic;
using TermSAT.Formulas;
using TermSAT.RuleDatabase;

namespace TermSAT.NandReduction
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



