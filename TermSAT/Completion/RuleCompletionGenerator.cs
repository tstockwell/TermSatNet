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
using TermSAT.RuleDatabase;

namespace TermSAT.Completion
{

    /**
     * This program implements the 
     * <a href="http://en.wikipedia.org/wiki/Knuth%E2%80%93Bendix_completion_algorithm">Knuth-Bendix completion algorithm</a> 
     * for propositional formulas and completes the basic set of formulas generated 
     * by the RuleGenerator utility.
     *   
     * The database of complete reduction rules generated by this program is then 
     * used as input into the RuleIndexer program that generates a index of the rules 
     * suitable to use for solving real-world problems. 
     * 
     * @author Ted Stockwell
     */
    public class RuleCompletionGenerator
    {

        public static void Main(string[] args)
        {
            new RuleCompletionGenerator().run();
        }

        private List<ReductionRule> _rules = new List<ReductionRule>();

        public void run()
        {
            // http://comjnl.oxfordjournals.org/content/34/1/2.full.pdf		
            // axiom set - set of equations initially containing formulas generated by the 
            //			   RuleGenerator program
            // rule set  - an initially empty set of rewrite rules
            // reduction - the process of repeatedly applying reduction rules to a formula 		
            // superposition - the process of finding a critical pair by unifying all subterms of the left hand sides of two rules		
            //
            // while axiom set is not empty
            //		select and remove an axiom from the axiom set
            //		Reduce the selected axiom
            //		if the axiom is not of the form x=x then
            //			Introduce it as a new rule in the rule set.
            //			Superpose the new rule on all existing rules, including itself, and 
            //			introduce each new critical pair into the axiom set		
            //		end if		
            // 
            // 
            try
            {
                // initialize rule set with all rules from generated by the RuleGenerator program
                var ruleDatabase = new RuleDatabase();
                var nonCanonicalFormulas = ruleDatabase.getAllNonCanonicalFormulas();
                foreach (var nonCanonicalFormula in nonCanonicalFormulas)
                {
                    var canonicalFormula= ruleDatabase.findCanonicalFormula(nonCanonicalFormula);
                    var rule = new ReductionRule(nonCanonicalFormula, canonicalFormula);
                    _rules.Add(rule);
                    Trace.WriteLine("Added rule #" + _rules.Count + ": " + rule);
                }

                var generator = new CompletionGenerator1(_rules);
                Enumeration<ReductionRule> found = generator.run();
                while (found.hasMoreElements())
                {
                    ReductionRule rule = found.nextElement();
                    System.out.println("Found new rule : " + rule);
                }

            }
            catch (SQLException e)
            {
                throw new RuntimeException(e);
            }
        }
    }

}

