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
namespace TermSat.RuleDatabase
{
/**
 * A specialized container for efficiently managing truth tables.
 * Only works for formulas with 3 variable numbers <= 3.
 */

public class TruthTables {
	public const int MAX_TRUTH_VALUES= 1 << RuleDatabase.VARIABLE_COUNT;
	public const int MAX_TRUTH_TABLES= 1 << MAX_TRUTH_VALUES;

	private static String __zeropad; 
	static {
		char[] pad= new char[MAX_TRUTH_VALUES];
		for (int i= pad.length; 0 < i--;)
			pad[i]= '0';
		__zeropad= new String(pad);
	}
	
	private static const Map<Variable, Boolean>[] __valuations= new Map[MAX_TRUTH_VALUES];
	private static void init_valuations() {
		for (int i= 0; i < MAX_TRUTH_VALUES; i++) {
			String v= Integer.toString(i, 2);
			int l= RuleDatabase.VARIABLE_COUNT - v.length();
			if (0 < l) 
				v= __zeropad.substring(0, l) + v;
			
			Map<Variable, Boolean> valuation= new HashMap<Variable, Boolean>();
			for (int j= 1; j <= RuleDatabase.VARIABLE_COUNT; j++) {
				valuation.put(
						Variable.createVariable(j), 
						v.charAt(v.length()-j) == '0' ? Boolean.FALSE : Boolean.TRUE);
			}
			__valuations[i]= valuation;
		}
	}

	
	/*
	 * All truth tables have an associated human-readable string.
	 * Here we construct the human-readable strings for the tables
	 * String are read from left to right, the leftmost character is the 
	 * value when all variables are true, the rightmost variable is 
	 * the value of the formula when all variables are false.    
	 */
	private static const String[] _truthTableStrings= new String[MAX_TRUTH_TABLES];
	private static void init_truth_tables() {
		for (int i= 0; i < MAX_TRUTH_TABLES; i++) {
			String s= Integer.toString(i, 2);
			int l= MAX_TRUTH_VALUES - s.length();
			if (0 < l) 
				s= __zeropad.substring(0, l) + s;
			_truthTableStrings[i]= s;
		}
	}
	
	/**
	 * For 3 variables there are only 255 truth tables.
	 * Here we create all possible truth tables and store them in a map 
	 * indexed by their associated string representation.
	 */
	private static const Map<String, TruthTable> __tables= new TreeMap<String, TruthTable>();
	private static void init_tables() {
		for (int i= 0; i < MAX_TRUTH_TABLES; i++) {
			String key= _truthTableStrings[i];
			__tables.put(key, new TruthTable(i, key));
		}
	}

	// cache truth tables for formulas 
	const static private ReferenceQueue<Formula> __referenceQueue= new ReferenceQueue<Formula>();
	private static class FormulaReference extends SoftReference<Formula> {
		String _string;
		public FormulaReference(Formula referent) {
			super(referent, __referenceQueue);
			_string= referent.toString();
		}
	}
	const static private Map<String, TruthTable> __tablesForFormulas= new TreeMap<String, TruthTable>(); 
	const static private Map<String, FormulaReference> __formulas= new TreeMap<String, FormulaReference>(); 
	
	
	public static interface Builder {
		public boolean evaluate(Map<Variable, Boolean> valuation);
	}
	
	static TruthTable create(Builder builder) {
		char[] truthValues= new char[MAX_TRUTH_VALUES];
		for (int i= MAX_TRUTH_VALUES; 0 < i--;) 
			truthValues[MAX_TRUTH_VALUES-i-1]= builder.evaluate(__valuations[i]) ? '1' : '0';
		return __tables.get(new String(truthValues));
	}
	public static TruthTable create(String booleanString) {
		return __tables.get(booleanString);
	}
	public static TruthTable create(int i) {
		return __tables.get(_truthTableStrings[i]);
	}
	
	public static TruthTable getTruthTable(Formula formula) {
		TruthTable truthTable= null;
		String path= formula.toString();
		synchronized (__tablesForFormulas) {
			truthTable= __tablesForFormulas.get(path);
			if (truthTable == null) {
				__formulas.put(path, new FormulaReference(formula));
				truthTable= create(new TruthTables.Builder() {
					public boolean evaluate(Map<Variable, Boolean> valuation) {
						return formula.evaluate(valuation);
					}
				});
				__tablesForFormulas.put(path, truthTable);
			}
		}
		
		// clean up expired references
		FormulaReference ref;
		while ((ref= (FormulaReference)__referenceQueue.poll()) != null) {
			synchronized (__formulas) {
				__formulas.remove(ref._string);
				__tablesForFormulas.remove(ref._string);
			}
		}
		
		return truthTable;
	}
	
	static  {
		init_valuations();
		init_truth_tables();
		init_tables();
	}
	
	private TruthTables() { }
	
}

}


