using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TermSAT.Formulas;

namespace TermSAT.NandReduction
{
    /// <summary>
    /// Uses a SAT solver to determine the satisfiability of a CNF problem instance.  
    /// A given CNF problem is translated to a structured expression and then to a SAT-based cofactor model, all on the fly.  
    /// Based on 'Cofactor Modeling' as described in <see href="Documents/structured-expressions.md"/>.  
    /// </summary>
    public static class CofactorSolver
    {

        /// <summary>
        /// Populates the given SAT solver with clauses that represent the cofactor model of the CNF formula denoted by the given stream.  
        /// </summary>
        /// <param name="inputStream">A textual representation of a formula in conjunctive normal form, in DIMACS CNF format.</param>
        /// <param name="solver">A SAT solver that will be populated with a cofactor model of the input formula</param>
        public static void Cnf2CofactorModel(Stream inputStream, TwoSatSolver solver)
        {
            int variableCount= 0;
            int clauseCount = 0;

            var reader = new StreamReader(inputStream);
            String inputLine;
		    while ((inputLine= reader.ReadLine()) != null) 
            {
			
                // skip comments
			    if (inputLine.StartsWith("c"))
				    continue; 
			
			    String[] tokens = inputLine.Split(" ");
			    if (tokens[0].Equals("p")) {
				    variableCount= Int32.Parse(tokens[2]);
				    clauseCount= Int32.Parse(tokens[3]);
				    break;
			    }
			
			    throw new Exception("Expected to find 'p' line before clauses");
            }

            // read all clauses and append into one big formula
            Formula formula = null;
            for (int count = 1; count <= clauseCount; count++)
            {
                if ((inputLine= reader.ReadLine()) == null)
                    throw new Exception("Premature end of file");

                // get all variables in the clause
                String[] tokens = inputLine.Split(" ");
                if (tokens.Length <= 1) // an empty clause
                    continue;

                // end variable symbols with '.'
                for (int t = 0; t < tokens.Length-1; t++)
                {
                    tokens[t]= tokens[t].StartsWith("-") ?
                         "-"+tokens[t].Substring(1)+"." :
                         tokens[t]+".";
                }

                // create clause
                Formula clause = Formula.createFormula(tokens[0]);
                for (int v = 1; v < tokens.length-1; v++)
                    clause= Formula.createImplication(
                                Formula.createNegation(clause),
                                Formula.createFormula(tokens[v]));

                // add clause to formula
                if (formula == null)
                {
                    formula= clause;.
                                    }
                else
                    formula= Formula.createNegation(
                            Formula.createImplication(
                                    formula,
                                    Formula.createNegation(clause)));

                System.out.println("processing clause "+count+" of "+cnfFile._clauseCount+"; formula length: "+formula.length());

                Formula reduced = solver.reduce(formula);
                formula= reduced;
            }

            cnfFile._formula= formula;
            return cnfFile;
	    }
</param>
        /// <param name="solver"></param>
        /// <exception cref="RuntimeException"></exception>
        public static void CnfToCofactorModel(Stream inputStream, TwoSatSolver solver)
        {
            CNFFile cnfFile= new CNFFile();
            BufferedReader reader = new BufferedReader(new InputStreamReader(inputStream));
            String inputLine;
		    while ((inputLine= reader.readLine()) != null) {
			
			    if (inputLine.startsWith("c"))
				    continue;
			
			    String[] tokens = inputLine.split(" ");
			    if (tokens[0].equals("p")) {
				    cnfFile._variableCount= Integer.parseInt(tokens[2]);
				    cnfFile._clauseCount= Integer.parseInt(tokens[3]);
				    break;
			    }
			
			    throw new RuntimeException("Expected to find 'p' line before clauses");
    }

// read all clauses and append into one big formula
Formula formula = null;
for (int count = 1; count <= cnfFile._clauseCount; count++)
{
    if ((inputLine= reader.readLine()) == null)
        throw new RuntimeException("Premature end of file");

    // get all variables in the clause
    String[] tokens = inputLine.split(" ");
    if (tokens.length <= 1) // an empty clause
        continue;

    // end variable symbols with '.'
    for (int t = 0; t < tokens.length-1; t++)
    {
        tokens[t]= tokens[t].startsWith("-") ?
             "-"+tokens[t].substring(1)+"." :
             tokens[t]+".";
    }

    // create clause
    Formula clause = Formula.createFormula(tokens[0]);
    for (int v = 1; v < tokens.length-1; v++)
        clause= Formula.createImplication(
                    Formula.createNegation(clause),
                    Formula.createFormula(tokens[v]));

    // add clause to formula
    if (formula == null)
    {
        formula= clause;
    }
    else
        formula= Formula.createNegation(
                Formula.createImplication(
                        formula,
                        Formula.createNegation(clause)));

    System.out.println("processing clause "+count+" of "+cnfFile._clauseCount+"; formula length: "+formula.length());

    Formula reduced = solver.reduce(formula);
    formula= reduced;
}

cnfFile._formula= formula;
return cnfFile;
	}
    }
}
