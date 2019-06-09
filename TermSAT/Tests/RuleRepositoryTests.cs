package com.googlecode.termsat.core.tests;

import java.sql.SQLException;
import java.util.ArrayList;

import junit.framework.TestCase;

import com.googlecode.termsat.core.Formula;
import com.googlecode.termsat.core.ruledb.ResultIterator;
import com.googlecode.termsat.core.ruledb.RuleDatabase;
import com.googlecode.termsat.core.ruledb.TruthTable;
import com.googlecode.termsat.core.ruledb.TruthTables;
import com.googlecode.termsat.core.solver.RuleRepository;

public class RuleRepositoryTests  extends TestCase {

	/*
	 * We must check that our ordering of formulas by length is 'stable'.
	 * An ordering is stable if, for all equal terms, if t' > t'' then 
	 * t'[t/x] > t''[t/x].
	 * Our ordering is stable if for all formulas with the same truth value 
	 * there is no formula t that contains a variable x and a formula t' 
	 * such that t' < t and t' contains more instances of x than t. 
	 * 
	 * It is easy to see that our reduction ordering is also 'monotonic'.
	 * Also, all our reduction rules have the form t -> t' where t' < t.
	 * Therefore, if our reduction ordering is stable then our set of 
	 * reduction rules is Noetherian and thus any reduction process driven by 
	 * our reduction rules is guaranteed to terminate. 
	 */
	public void testOrderingStability() throws SQLException {
		RuleDatabase ruleDatabase= new RuleDatabase();
		class FormulaInfo {
			String text;
			int[] counts= new int[] { 0, 0, 0 };
		}
		
		for (int t= 0; t < TruthTables.MAX_TRUTH_TABLES; t++) {
			TruthTable tt= TruthTables.create(t);
			ResultIterator<Formula> i= ruleDatabase.getAllFormulas(tt);
			ArrayList<FormulaInfo> list= new ArrayList<FormulaInfo>();
			while (i.hasNext()) {
				FormulaInfo fi= new FormulaInfo();
				fi.text= i.next().toString();
				String[] vars= fi.text.split("[^1234567890]");
				for (String var:vars) { 
					if (0 < var.length())
						fi.counts[Integer.parseInt(var)-1]++;
				}
			}
			for (FormulaInfo a: list) {
				for (FormulaInfo b: list) {
					if (a.text.length() <= b.text.length())
						continue;
					for (int v= 0; v < 3; v++)
						if (a.counts[v] < b.counts[v])
							throw new RuntimeException("Reduction ordering is not stable for these two formulas:\n"+a.text+"\n"+b.text);
				}
			}
		}
	}

	// basic consistency checks
	public void testSoundness() throws SQLException {
		RuleRepository rules = new RuleRepository();
		RuleDatabase ruleDatabase= new RuleDatabase();
		ResultIterator<Formula> nonCanonicalFormulas= ruleDatabase.getAllNonCanonicalFormulas();
		
		// for all non-canonical formulas...
		while (nonCanonicalFormulas.hasNext()) { 
			Formula rule= nonCanonicalFormulas.next();
			assertTrue(rules.containsKey(rule.toString()));
			
			// ...make sure that reduced formula is actually shorter...
			Formula reduced= rules.findReducedFormula(rule);
			assertTrue(reduced.length() < rule.length());
			
			//..and has the same truth table as the original formula...
			assertEquals(TruthTables.getTruthTable(rule), TruthTables.getTruthTable(reduced));
			
			//..and has the same truth table as the associated canonical formula
			Formula canonical= ruleDatabase.findCanonicalFormula(rule);
			assertEquals(TruthTables.getTruthTable(rule), TruthTables.getTruthTable(canonical));
		}
	}

}
