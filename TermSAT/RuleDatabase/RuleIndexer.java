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
package com.googlecode.termsat.core.ruledb;

import java.sql.Connection;
import java.sql.SQLException;
import java.sql.Statement;
import java.util.HashMap;
import java.util.TreeMap;

import com.googlecode.termsat.core.Formula;
import com.googlecode.termsat.core.InstanceRecognizer;
import com.googlecode.termsat.core.utils.TrieMap;
import com.googlecode.termsat.core.utils.TrieMap.Node;

/**
 * A utility class that creates the index used by the RuleRepository class 
 * from the rule database constructed by the RuleGenerator program. 
 * 
 * An id is assigned to every formula.
 * Canonical formulas use negative numbers for Id.
 * Noncanonical formulas use positive numbers. 
 * 
 * @author Ted Stockwell <emorning@yahoo.com>
 *
 */
public class RuleIndexer {
	
	public const final String dbFolder= "db/rules-index";
	public const final String dbURL= RuleDatabase.protocol+dbFolder+RuleDatabase.options;
	
	public static void main(String[] args) throws SQLException {
		new RuleIndexer(args);
	}
	
	const RuleDatabase _ruleDatabase= new RuleDatabase();
	Connection _indexConnection;
	int _nextNoncanonicalIndex= 1;
	HashMap<Node<Formula>, Integer> _noncanonicalIdentifiers= new HashMap<Node<Formula>, Integer>();
	int _nextCanonicalIndex= -1;
	TreeMap<String, Integer> _canonicalIdentifiers= new TreeMap<String, Integer>();
	
	const private InstanceRecognizer _noncanonicalFormulas= new InstanceRecognizer();
	
	public RuleIndexer(String[] args) throws SQLException {
		_indexConnection= RuleDatabase.createConnection(dbURL);
		
		ResultIterator<Formula> nonCanonicalFormulas= _ruleDatabase.getAllNonCanonicalFormulas();
		int i= 0;
		while ( nonCanonicalFormulas.hasNext()) {
			Formula f= nonCanonicalFormulas.next();
			_noncanonicalFormulas.addFormula(f);
			System.out.println("Total non-canonical formulas loaded: "+(++i));
		}
		
		
		createNonCanonicalTable();
		createCanonicalTable();
		
		
		populateCanonicalTable();
		populateNoncanonicalTable();
	}
	

	private void populateCanonicalTable() throws SQLException {
		ResultIterator<Formula> canonicalFormulas= _ruleDatabase.getAllCanonicalFormulasInLexicalOrder();
		int i= 0;
		while ( canonicalFormulas.hasNext()) {
			Formula f= canonicalFormulas.next();
			int id= _nextCanonicalIndex--;
			_canonicalIdentifiers.put(f.toString(), id);
			String sql= "INSERT INTO CANONICAL VALUES (" + id+ ", '"+f.toString()+"')";
			Statement s = _indexConnection.createStatement();
			s.execute(sql);
			s.close();
			System.out.println("Total canonical formulas loaded: "+(++i));
		}
	}


	private void createNonCanonicalTable() throws SQLException {
			Statement s = _indexConnection.createStatement();

			s.execute(
				"create table NONCANONICAL (" +
				"ID int NOT NULL , " +
				"SYMBOL char(1) NOT NULL, " +
				"PARENT int NOT NULL, " +
				"CANONICAL_ID int NOT NULL, " + 
				"PRIMARY KEY (ID)" +
				" )"
			);
			s.execute("CREATE INDEX NONCANONICAL_INDEX_1 ON NONCANONICAL (PARENT, SYMBOL)");
			s.close();
	}
	

	private void createCanonicalTable() throws SQLException {
			Statement s = _indexConnection.createStatement();
			s.execute(
				"create table CANONICAL (" +
				"ID int NOT NULL , " +
				"FORMULA varchar(100) NOT NULL, " +
				"PRIMARY KEY (ID)" +
				" )"
			);
			s.close();
	}

	private void populateNoncanonicalTable() throws SQLException {
		_noncanonicalFormulas.accept(new TrieMap.Visitor<Formula, Void>() {
			public boolean visit(CharSequence key, Node<Formula> node) {
				addNonCanonicalRecord(key, node);
				return true;
			}
			public void leave(CharSequence key, Node<Formula> node) { /* do nothing */ }
			public boolean isComplete() { return false; }
			public Void getResult() { return null; }
		});
	}


	private void addNonCanonicalRecord(CharSequence key, Node<Formula> node)  {
		try {
			char symbol= node.getChar();
			int id= _nextNoncanonicalIndex++;
			int parentId= 0;
			Node<Formula> parent= node.getParent();
			if (0 < parent.depth()) // if not root
				parentId= _noncanonicalIdentifiers.get(parent);
			
			int canonicalId= 0;
			Formula rule= node.getValue();
			if (rule != null) {
				Formula canonical= _ruleDatabase.findCanonicalFormula(rule);
				canonicalId= _canonicalIdentifiers.get(canonical.toString());
			}
			
			String sql= "INSERT INTO NONCANONICAL VALUES (" + id+ ", '"+symbol+"',"+ parentId+ ","+ canonicalId+")";
			Statement s = _indexConnection.createStatement();
			s.execute(sql);
			s.close();
			
			_noncanonicalIdentifiers.put(node, id);
			
			System.out.println("Total non-canonical records created: "+_nextNoncanonicalIndex);
		} catch (SQLException e) {
			throw new RuntimeException(e);
		}
	}

}
