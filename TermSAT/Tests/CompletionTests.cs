/*******************************************************************************
 *     termsat SAT solver
 *     Copyright (C) 2019 Ted Stockwell <emorning@yahoo.com>
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
using TermSAT.Formulas;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System;

namespace TermSAT.Tests
{
	[TestClass]
    public class CompletionTests
    {
        public void testCriticalTerms() 
        {
            Formula formula1= Formula.CreateFormula("*FF");
        Formula formula2 = Formula.CreateFormula("*1.F");
        List<Formula> criticalTerms = Formula.findAllCriticalTerms(formula1, formula2);
        Assert.IsTrue(criticalTerms.Count == 1, "There should only be one critical term: *FF");
        Assert.Equals(Formula.CreateFormula("*FF"), criticalTerms[0]);
        }


    public void testIndependence() 
    {
        Formula formula1= Formula.CreateFormula("*1.2.");
        Formula formula2= formula1;
        Formula independent= formula1.createIndependentInstance(formula2);
        Assert.Equals(Formula.CreateFormula("*3.4."), independent);

        formula1= Formula.CreateFormula("**1.2.*-1.3.");
        formula2= Formula.CreateFormula("*1.F");
        independent= formula1.createIndependentInstance(formula2);
        Assert.Equals(Formula.CreateFormula("**4.2.*-4.3."), independent);
    }


public void testSyntaxEquality()
{
    Formula one = Formula.CreateFormula("-2.");
    Formula two = Formula.CreateFormula("-2.");
    assertTrue("Identical formulas not recognised",
            Formula.syntacticallyEqual(one, two));

    one = Formula.CreateFormula("-2.");
    two = Formula.CreateFormula("-1.");
    assertTrue("Identical formulas not recognised",
            Formula.syntacticallyEqual(one, two));

    one = Formula.CreateFormula("-2.");
    two = Formula.CreateFormula("-*1.3.");
    assertFalse("Non-identical formulas not recognised",
            Formula.syntacticallyEqual(one, two));
}


public void testCompletion() 
{
    // /
    // / this is based on an example from
    // http://comjnl.oxfordjournals.org/content/34/1/2.full.pdf
    // /
    // first, make sure the rules we are starting with are valid
    Assert.IsFalse(TruthTables.getTruthTable(Formula.CreateFormula("**T1.1."))
				.ToString().contains("0"));
    Assert.IsFalse(TruthTables.getTruthTable(Formula.CreateFormula("**1.TT"))
				.ToString().contains("0"));
    Assert.IsFalse(TruthTables
				.getTruthTable(Formula.CreateFormula("**1.-1.-1.")).ToString()
				.contains("0"));
    Assert.IsFalse(TruthTables
				.getTruthTable(Formula.CreateFormula("***1.2.3.*1.*2.3."))
				.ToString().contains("0"));

    Assert.IsTrue(TruthTables.getTruthTable(Formula.CreateFormula("*T1."))
				.equals(TruthTables.getTruthTable(Formula.CreateFormula("1."))));
    Assert.IsTrue(TruthTables.getTruthTable(Formula.CreateFormula("*1.T"))
				.equals(TruthTables.getTruthTable(Formula.CreateFormula("T"))));
    Assert.IsTrue(TruthTables
				.getTruthTable(Formula.CreateFormula("*1.-1."))
				.equals(TruthTables.getTruthTable(Formula.CreateFormula("-1."))));
    Assert.IsTrue(TruthTables.getTruthTable(Formula.CreateFormula("**1.2.3."))
				.equals(TruthTables.getTruthTable(Formula
						.CreateFormula("*1.*2.3."))));

    ArrayList<ReductionRule> startingRules = new ArrayList<ReductionRule>();
		startingRules.add(new ReductionRule(Formula.CreateFormula("*T1."),
				Formula.CreateFormula("1.")));
		startingRules.add(new ReductionRule(Formula.CreateFormula("*1.T"),
				Formula.CreateFormula("1.")));
		startingRules.add(new ReductionRule(Formula.CreateFormula("*1.-1."),
				Formula.CreateFormula("F")));
		startingRules.add(new ReductionRule(Formula.CreateFormula("**1.2.3."),
				Formula.CreateFormula("*1.*2.3.")));

		CompletionGenerator completionGenerator = new CompletionGenerator(
                startingRules);
Enumeration<ReductionRule> results = completionGenerator.run();
ArrayList<ReductionRule> generatedRules = new ArrayList<ReductionRule>();
		while (results.hasMoreElements()) {
			ReductionRule reductionRule = results.nextElement();
generatedRules.add(reductionRule);
		}
		Assert.IsFalse(generatedRules.isEmpty());
	}

	// these tests take a long time
	//
	// public void testCNFExample1() throws SQLException, IOException {
	// runCNFtest("cnf-example-1.txt");
	// }
	// public void testEqAtreeBraun12Unsat() throws SQLException, IOException {
	// runCNFtest("eq.atree.braun.12.unsat.cnf");
	// }
	// public void testrpoc_xits_08_unsat() throws SQLException, IOException {
	// runCNFtest("rpoc_xits_08_UNSAT.cnf");
	// }
	// public void testSAT_Dat_k45() throws SQLException, IOException {
	// runCNFtest("SAT_dat.k45.txt");
	// }
	//
	// void runCNFtest(String filename) throws SQLException, IOException {
	//
	// ClassLoader classLoader= getClass().getClassLoader();
	// String homeFolder=
	// getClass().getPackage().getName().replaceAll("\\.","/");
	// Solver solver= new Solver(new RuleRepository());
	//
	// InputStream inputStream=
	// classLoader.getResourceAsStream(homeFolder+"/"+filename);
	// assertNotNull("Missing input file:"+homeFolder+"/"+filename,
	// inputStream);
	// CNFFile file= CNFFile.readAndReduce(inputStream, solver);
	// Assert.Equals(Constant.FALSE, file.getFormula());
	// inputStream.close();
	// }
}

}

