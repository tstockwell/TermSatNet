/*******************************************************************************
 *     termsat SAT solver
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
package com.googlecode.termsat.core.solver;

import java.io.BufferedReader;
import java.io.IOException;
import java.io.InputStream;
import java.io.InputStreamReader;

import com.googlecode.termsat.core.Formula;


/**
 * Reads a clausal SAT problem from a file.
 * File format described here: 
 * 		http://www.satcompetition.org/2004/format-solvers2004.html
 * 
 * @author Ted Stockwell <emorning@yahoo.com>
 */
public class CNFFile {

	public static CNFFile readAndReduce(InputStream inputStream, Solver solver)
	 throws IOException
	{
		CNFFile cnfFile= new CNFFile();		
		BufferedReader reader= new BufferedReader(new InputStreamReader(inputStream));
		String inputLine;
		while ((inputLine= reader.readLine()) != null) {
			
			if (inputLine.startsWith("c"))
				continue;
			
			String[] tokens= inputLine.split(" ");
			if (tokens[0].equals("p")) {
				cnfFile._variableCount= Integer.parseInt(tokens[2]);
				cnfFile._clauseCount= Integer.parseInt(tokens[3]);
				break;
			}
			
			throw new RuntimeException("Expected to find 'p' line before clauses");
		}
		
		// read all clauses and append into one big formula
		Formula formula= null;
		for (int count= 1; count <= cnfFile._clauseCount; count++) { 
			if ((inputLine= reader.readLine()) == null) 
					throw new RuntimeException("Premature end of file");
			
			// get all variables in the clause
			String[] tokens= inputLine.split(" ");
			if (tokens.length <= 1) // an empty clause
				continue;
			
			// end variable symbols with '.'
			for (int t= 0; t < tokens.length-1; t++) {
				tokens[t]= tokens[t].startsWith("-")?
					 "-"+tokens[t].substring(1)+".": 
					 tokens[t]+".";
			}
			
			// create clause
			Formula clause= Formula.createFormula(tokens[0]);
			for (int v= 1; v < tokens.length-1; v++) 
				clause= Formula.createImplication(
							Formula.createNegation(clause),
							Formula.createFormula(tokens[v]));
				
			// add clause to formula
			if (formula == null) {
				formula= clause;
			}
			else
				formula= Formula.createNegation(
						Formula.createImplication(
								formula,
								Formula.createNegation(clause)));
			
			System.out.println("processing clause "+count+" of "+cnfFile._clauseCount+"; formula length: "+formula.length());
			
			Formula reduced= solver.reduce(formula);
			formula= reduced;
		}
		
		cnfFile._formula= formula;
		return cnfFile;
	}
	
	public static CNFFile read(InputStream inputStream) throws IOException 
	{
		return readAndReduce(inputStream, Solver.FAUX_SOLVER);
	}
	
	private Formula _formula;
	private int _variableCount;
	private int _clauseCount;
	
	private CNFFile() { }

	public Formula getFormula() {
		return _formula;
	}
	
	public int getVariableCount() {
		return _variableCount;
	}

	public int getClauseCount() {
		return _clauseCount;
	}
}
