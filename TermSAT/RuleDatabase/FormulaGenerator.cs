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
        ResultIterator<Formula> _currentIterator;

        List<Formula> _startingFormulas = new List<Formula>();


        public FormulaGenerator(RuleDatabase database)
        {
            _database = database;

            _startingFormulas.add(Constant.FALSE);
            _startingFormulas.add(Constant.TRUE);
            for (int i = 1; i <= RuleDatabase.VARIABLE_COUNT; i++)
                _startingFormulas.add(Variable.newVariable(i));
        }

        public Formula getStartingFormula()
        {
            Formula formula = _database.getLastGeneratedFormula();
            if (formula == null)
            {
                _currentLength = 1;
                _currentIterator = new ResultIterator<Formula>()
                {
                    Iterator < Formula > _iterator = _startingFormulas.iterator();
                public void close()
                {
                    // do nothing
                }
                public boolean hasNext()
                {
                    return _iterator.hasNext();
                }
                public Formula next()
                {
                    return _iterator.next();
                }
                public void remove()
                {
                    throw new UnsupportedOperationException();
                }
            };
        } 
		else {
			_currentLength= formula.length();
			_currentIterator= new FormulaConstructor(_database, formula);
    }
		return _currentIterator.next();
	}

    public Formula getNextWellFormedFormula()
    {
        if (_currentIterator.hasNext() == false)
        {

            _currentIterator.close();

            FormulaConstructor nextConstructor = null;
            while (nextConstructor == null)
            {
                _currentLength++;
                System.out.println("The formulas lengths have been increased to " + _currentLength);

                if (TruthTables.MAX_TRUTH_TABLES <= _database.countCanonicalTruthTables())
                    if (_database.lengthOfLongestPossibleNonReducableFormula() < _currentLength)
                    {
                        System.out.println("!!!!!! The Rule Database is Complete !!!");
                        return null;
                    }

                FormulaConstructor fc = new FormulaConstructor(_database, _currentLength);
                if (fc.hasNext())
                    nextConstructor = fc;
            }
            _currentIterator = nextConstructor;

        }
        return _currentIterator.next();
    }

}

}
