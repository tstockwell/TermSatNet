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
package com.googlecode.termsat.core.ruledb;

import java.util.List;

import com.googlecode.termsat.core.Formula;


/**
 * Spits out a bunch of information about the formulas in the Rule Database.
 *  
 * @author Ted Stockwell
 *
 */
public class DatabaseReport {
	
	private static const Object SHOW_REDUCTION_RULES = "-showReductionRules";

	public static void main(String[] args)
	throws Exception
	{
		boolean showReductionRules= false;
		
		for (String arg:args)
			if (arg.equals(SHOW_REDUCTION_RULES))
				showReductionRules= true;
		
		const RuleDatabase database= new RuleDatabase();
		
		/* 
		 * Count # of rules
		 */
		System.out.println("Total number of canonical formulas is "+database.countCanonicalFormulas());
		System.out.println("Total number of non-canonical formulas  is "+database.countNonCanonicalFormulas());
		
		int tt= 0;
		for (int truthValue= 0; truthValue < TruthTables.MAX_TRUTH_TABLES; truthValue++) {
			TruthTable truthTable= TruthTables.create(truthValue);
			if (0 < database.getLengthOfCanonicalFormulas(truthTable))
				tt++;
		}
		System.out.println("Have found formlas for "+tt+" of "+TruthTables.MAX_TRUTH_TABLES+ " truth tables");
		System.out.println("The length of the longest canonical formula is "+database.getLengthOfLongestCanonicalFormula());
		System.out.println();
		
		
		/*
		 * List lengths and # of canonical formulas
		 */
		database.getLengthOfCanonicalFormulas(TruthTables.create(0));
		System.out.println("TRUTH VALUE     LENGTH    COUNT");
		System.out.println("                FORMULAS");
		System.out.println("-------------   ------   ------");
		for (int truthValue= 0; truthValue < TruthTables.MAX_TRUTH_TABLES; truthValue++) {
			TruthTable truthTable= TruthTables.create(truthValue);
			List<Formula> canonicalFormulas= database.getCanonicalFormulas(truthTable);
			
			String t= ""+truthValue+"("+truthTable+")                 ";
			t= t.substring(0, 13);

			if (canonicalFormulas.isEmpty()) {
				System.out.println(t+"   *not yet determined*");
			}
			else {
				String l= "      "+canonicalFormulas.get(0).length();
				l= l.substring(l.length()-6);
				String c= "      "+canonicalFormulas.size();
				c= c.substring(c.length()-6);
				System.out.println(t+"   "+l+"   "+c);
				for (Formula formula:canonicalFormulas) {
					System.out.println("              "+formula.toString());
				}
			}
			System.out.println("-------------   ------   ------");
		}
		
		System.out.println();
		System.out.println("Canonical Formulas in Lexical Order");
		System.out.println("=====================================");
		ResultIterator<Formula> canonicalFormulas= database.getAllCanonicalFormulasInLexicalOrder();
		while (canonicalFormulas.hasNext()) {
			Formula formula= canonicalFormulas.next();
			System.out.println(formula);
		}
		
		if (showReductionRules) {
			System.out.println();
			System.out.println("Reduction Rules");
			System.out.println("=====================================");
			ResultIterator<Formula> nonCanonicalFormulas= database.getAllNonCanonicalFormulas();
			long count= 0;
			while (nonCanonicalFormulas.hasNext()) {
				Formula formula= nonCanonicalFormulas.next();
				System.out.println(new ReductionRule(formula, database.findCanonicalFormula(formula)));
				count++;
			}
		}
		
		
	}
	
}
