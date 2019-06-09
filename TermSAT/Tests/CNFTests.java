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
package com.googlecode.termsat.core.tests;

import java.io.IOException;
import java.io.InputStream;
import java.sql.SQLException;

import junit.framework.TestCase;

import com.googlecode.termsat.core.Constant;
import com.googlecode.termsat.core.solver.CNFFile;
import com.googlecode.termsat.core.solver.RuleRepository;
import com.googlecode.termsat.core.solver.Solver;

/**
 * Tests the Solver by running them against CNF examples.
 * 
 */
public class CNFTests extends TestCase {

	public void testCNFExample1() throws SQLException, IOException {
		runCNFtest("cnf-example-1.txt");
	}

	public void testEqAtreeBraun12Unsat() throws SQLException, IOException {
		runCNFtest("eq.atree.braun.12.unsat.cnf");
	}

	public void testrpoc_xits_08_unsat() throws SQLException, IOException {
		runCNFtest("rpoc_xits_08_UNSAT.cnf");
	}

	public void testSAT_Dat_k45() throws SQLException, IOException {
		runCNFtest("SAT_dat.k45.txt");
	}

	void runCNFtest(String filename) throws SQLException, IOException {

		ClassLoader classLoader = getClass().getClassLoader();
		String homeFolder = getClass().getPackage().getName().replaceAll("\\.", "/");
		Solver solver = new Solver(new RuleRepository());

		InputStream inputStream = classLoader.getResourceAsStream(homeFolder + "/" + filename);
		assertNotNull("Missing input file:" + homeFolder + "/" + filename, inputStream);
		CNFFile file = CNFFile.readAndReduce(inputStream, solver);
		assertEquals(Constant.FALSE, file.getFormula());
		inputStream.close();
	}
}
