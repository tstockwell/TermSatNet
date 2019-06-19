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
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace TermSAT.Formulas
{
    /**
     * Represents a propositional formula.
     * 
     * This system represents propositional formulas using only...
     * 	...constants TRUE and FALSE.
     *  ...variables.
     *  ...a negation operator 
     *  ...an implication operator (aka the if-then operator, most often written as ->).
     *  
     * This system supports a textual form for representing formulas.
     * The normal form uses Polish notation and...
     * 	...the symbols 'T' and 'F' for TRUE and FALSE.
     *  ...the symbol '-' for the negation operator, followed by a formula. 
     *  ...the symbol '*' for the implication operator, followed by the consequent 
     *  	and then the antecedent. 
     *  ...the symbol '.' followed by a sequence of digits, for representing variables.
     *  
     *  Polish notation is used because it is compact, eliminates the 
     *  necessity of using parenthesis, and is simple to parse.
     *  
     *  Because this system is, eventually, meant to be used to solve large and 
     *  complex formulas, care is taken to make the system as memory efficient as 
     *  possible.  
     *  There is only ever a single instance created of any particular formula.
     *  All subclasses of the Formula class cache instances of previously created formulas 
     *  so that only one instance of that formula is ever created.
     *  This makes it possible to optimize many routines by caching previously 
     *  considered formulas in a hashmap and then quickly identifying previously 
     *  considered formulas by their hashcode.
     *  Caching Formula instances also reduces memory use.
     *  Formulas are cached using weak references so it's possible that 
     *  a previously created formula is recreated with a different hashcode, but 
     *  that should only happen if there are no objects holding any references to 
     *  any previously created instance.
     *  
     *  #see PrettyFormula for converting formula to 'pretty' text. 
     *  
     * 
     * @author Ted Stockwell <emorning@yahoo.com>
     */
    abstract public partial class Formula : IEquatable<Formula>, IComparable<Formula>
    {
        private static readonly ConditionalWeakTable<Object, List<Variable>> __varListCache = new ConditionalWeakTable<Object, List<Variable>>();

        public int Length { get; }

        /// <param name="length">The number of symbols in this formula</param>
        protected Formula(int length)
        {
            Length = length;
        }

        /// <summary>
        ///     Here's an important point to understand about formula ordering...
        /// 
        ///     ... In order to prove that TermSAT's set of reduction rules is 'complete' we 
        ///         need to create an ordering of formulas in terms of 'simplicity'.
        ///         That is, 'simpler' formulas must come before more 'complex' formulas 
        ///         in the ordering.
        /// 
        ///     ... However, in order to use formulas in indexes we often want formulas ordered 
        ///         lexically.
        /// 
        ///     The ordering implemented here does both (I hope).
        /// 
        ///     Also, note that the characters used to represent formulas as strings were 
        ///     specifically chosen so that the string representations of a formula naturally 
        ///     sort in the same order as formulas.  
        /// 
        /// </summary>
        public int CompareTo(Formula other)
        {
            if (other == this)
                return 0;

            // shorter formulas are simpler than longer forumlas
            if (Length < other.Length)
                return -1;
            if (other.Length < Length)
                return 1;

            // constants are simpler than any other formula, T comes before F in this world.
            if (this is Constant)
            {
                return (other is Constant) ? (this == Constant.TRUE ? -1 : 1) : -1;
            }

            if (other is Constant)
            {
                return 1;
            }

            // variables are simpler than negation or implication
            if (this is Variable)
            {
                if (other is Variable)
                {
                    return (((Variable)this).Number < ((Variable)other).Number) ? -1 : 1;
                }
                return -1;
            }

            if (other is Variable)
            {
                return 1;
            }

            // negation is simpler than implication
            if (this is Negation)
            {
                if (other is Negation)
                {
                    // someday might need to remove this recursion.
                    // Can I use async/await as a way to avoid stack-based recursion?
                    return ((Negation)this).Child.CompareTo(((Negation)other).Child);
                }
                return -1; // negation comes before implication
            }

            if (other is Negation)
            {
                return 1;
            }

            // at this point both formulas must be Implications
            // implications with simpler antecendents come first
            var impThis = this as Implication;
            var impOther = other as Implication;
            {
                var aThis = impThis.Antecedent;
                var aOther = impOther.Antecedent;
                var c = aThis.CompareTo(aOther);
                if (c != 0)
                    return c;
            }

            var cThis = impThis.Consequent;
            var cOther = impOther.Consequent;
            var i = cThis.CompareTo(cOther);
            return i;
        }

        public bool Equals(Formula other)
        {
            return base.Equals(other);
        }

        // Force subclasses to implement ToString()
        override abstract public string ToString();

        /// <summary>
        ///   Evaluates a formula given a set of variable values
        /// </summary>
        /// <param name="valuation">A collection that denotes boolean values for all variables</param>
        /// <returns>true if the given valuation satisfies this formula, else false.</returns>
        public abstract bool Evaluate(IDictionary<Variable, bool> valuation);


        /**
         * Creates a new formula by replacing all the variables in this formula 
         * that also occur in the given formula with new variables that 
         * don't occur in the given formula
         */
        async public Task<Formula> createIndependentInstance(Formula formula)
        {
            var substitutions = new Dictionary<Variable, Formula>();
            var variables = formula.AllVariables;
            var newVariables = new List<Variable>();
            foreach (Variable variable in variables)
            {
                if (containsVariable(variable))
                {
                    for (int i = 1; ; i++)
                    {
                        Variable v = Variable.newVariable(i);
                        if (!containsVariable(v) && !variables.Contains(v) && !newVariables.Contains(v))
                        {
                            substitutions.Add(variable, v);
                            newVariables.Add(v);
                            break;
                        }
                    }
                }
            }
            if (substitutions.Count <= 0)
                return formula;
            Formula independent = await this.CreateSubstitutionInstance(substitutions);
            return independent;
        }

        /**
         * Create a formula from its textual representation in normal form
         */
        public static Formula CreateFormula(string path)
        {
            return Parse(path);
        }


        /**
         * Parses out the first formula from the beginning of the given sequence 
         */
        public static Formula Parse(string formula)
        {
            // find the end of the formula
            int last = -1;
            {
                int count = 0;
                int max = formula.Length - 1;
                for (int i = 0; i <= max; i++)
                {
                    char c = formula[i];
                    if (Symbol.isSymbol(c))
                        count++;
                    if (0 < count)
                    {
                        last = i + 1;
                        break;
                    }
                }
            }
            if (last < 0)
                throw new Exception("Not a valid formula:" + formula);


            Stack<Formula> stack = new Stack<Formula>();
            for (int i = last; 0 < i--;)
            {
                char c = formula[i];
                if (Symbol.isVariable(c))
                {
                    Formula f = stack.Pop();
                    stack.Push(Negation.newNegation(f));
                }
                else if (Symbol.isImplication(c))
                {
                    Formula antecendent = stack.Pop();
                    Formula consequent = stack.Pop();
                    stack.Push(Implication.newImplication(antecendent, consequent));
                }
                else if (Symbol.isTrue(c))
                {
                    stack.Push(Constant.TRUE);
                }
                else if (Symbol.isFalse('F'))
                {
                    stack.Push(Constant.FALSE);
                }
                else if (Symbol.isVariable(c))
                {
                    int end = i;
                    while (0 < i && Char.IsDigit(formula[i - 1]))
                        i--;
                    var n = int.Parse(formula.Substring(i, end - i));
                    stack.Push(Variable.newVariable(n));
                }
                else
                    throw new Exception("Unknown symbol:" + c);
            }

            if (stack.Count != 1)
                throw new Exception("Invalid postcondition after evaluating formula wellformedness: count < 1");

            Formula formula2 = stack.Pop();
            return formula2;
        }

        /**
         * Returns substitutions that will convert the given formulas into a single formula 
         * that is also substitution instance of both formulas.
         * The goal of unification is to find a substitution instance which demonstrates 
         * that two seemingly different terms are in fact either identical or just equal.
         * 
         * @return null if no such substitution exists otherwise, 
         * 		an empty map if the formulas are equal, 
         * 		else returns a map of substitutions for each formula.
         */
        public static async Task<IDictionary<Variable, Formula>> Unify(Formula left, Formula right)
        {
            if (left is Constant)
            {
                if (left.Equals(right))
                    return new Dictionary<Variable, Formula>();
                if (right is Variable)
                {
                    return new Dictionary<Variable, Formula>() { [(Variable)right] = left };
                }
                return null;
            }
            else if (right is Constant)
            {
                if (left is Variable)
                    return new Dictionary<Variable, Formula>() { [(Variable)left] = right };
                return null;
            }
            else if (left is Variable)
            {
                if (right.Equals(left))
                    return new Dictionary<Variable, Formula>();
                if (right.containsVariable((Variable)left))
                    return null;
                return new Dictionary<Variable, Formula>() { [(Variable)left] = right };
            }
            else if (right is Variable)
            {
                if (left.containsVariable((Variable)right))
                    return null;
                return new Dictionary<Variable, Formula>() { [(Variable)right] = left };
            }
            else if (left is Negation)
            {
                if (right is Negation)
                    return await Unify(((Negation)left).Child, ((Negation)right).Child);
                return null;
            }
            else if (right is Negation)
            {
                return null;
            }
            else if (left is Implication)
            {
                if (!(right is Implication))
                    return null;
                Formula la = ((Implication)left).Antecedent;
                Formula ra = ((Implication)right).Antecedent;
                var ua = await Unify(la, ra);
                if (ua == null)
                    return null;

                Formula lc = ((Implication)left).Consequent;
                Formula rc = ((Implication)right).Consequent;
                var ltask = lc.CreateSubstitutionInstance(ua);
                var rtask = rc.CreateSubstitutionInstance(ua);
                Formula lu = await ltask;
                Formula ru = await rtask;

                var uc = await Unify(lu, ru);
                if (uc == null)
                    return null;

                var map = new Dictionary<Variable, Formula>();

                foreach (Variable v in ua.Keys)
                {
                    // must create composition of substitutions
                    Formula f = ua[v];
                    f = await f.CreateSubstitutionInstance(uc);
                    map[v] = f;
                }
                foreach (Variable v in uc.Keys)
                {
                    map[v] = uc[v];
                }

                return map;
            }
            else
            {
                // should never get here
                throw new Exception("Internal Error - unknown formula type");
            }

            // I originally wrote this method based on this pseudocode I found on the net...
            //		function unify(E1, E2);
            //	    begin
            //	        case
            //	            both E1 and E2 are constants or the empty list:
            //	                if E1 = E2 then return {}
            //	                else return FAIL;
            //	            E1 is a variable:
            //	                if E1 occurs in E2 then return FAIL
            //	                 else return {E2/E1}
            //	            E2 is a variable
            //	                if E2 occurs in E1 then FAIL
            //	                    else return {E1/E2}
            //	            either E1 or E2 are empty then return FAIL
            //	            otherwise:
            //	                begin
            //	                    HE1 := first element of E1;
            //	                    HE2 := first element of E2;
            //	                    SUBS1 := unify(HE1, HE2);
            //	                    if SUBS1 := FAIL then return FAIL;
            //	                    TE1 := apply(SUBS1, rest of E1);
            //	                    TE2 := apply(SUBS1, rest of E2);
            //	                    SUBS2 := unify(TE1, TE2);
            //	                    if SUBS2 = FAIL then return FAIL;
            //	                         else return composition(SUBS1, SUBS2)
            //	                end
            //	            end
            //	        end		
        }

        abstract public bool containsVariable(Variable variable);

        /**
         * @return a list of all subterms.  Includes this formula.
         */
        virtual public ICollection<Formula> AllSubterms {
            get {
                var subterms = new HashSet<Formula>();
                this.GetAllSubterms(subterms);
                return subterms;
            }
        }
        abstract public void GetAllSubterms(ICollection<Formula> subterms);

        /**
         * Returns an ordered list of the variable that occur in this formula.
         * The returned list may be empty.
         */
        abstract public IList<Variable> AllVariables { get; }


        /**
         * Two rules are syntactically equal if they are identical except for variable names.
         * 
         */
        async public static Task<bool> syntacticallyEqual(Formula left, Formula right)
        {
            if (left == right)
                return true;

            var unification = await Formula.Unify(left, right);
            if (unification == null)
                return false;
            foreach (Formula f in unification.Values)
            {
                if (!(f is Variable))
                    return false;
            }
            return true;
        }

        /**
         * Generate all the critical terms generated by the unification of all subterms of two formulas, as described in 
         * An Introduction to Knuth-Bendix Completion, AJJ Dick, http://comjnl.oxfordjournals.org/content/34/1/2.full.pdf
         * 
         * @return a list of formulas 
         */
        async public static Task<List<Formula>> findAllCriticalTerms(Formula left, Formula right)
        {

            HashSet<Formula> criticalTerms = new HashSet<Formula>();
            var leftSubterms = left.AllSubterms;

            // First, go through all the subterms of the left formula and unify with the right formula.
            // Then, go through all the subterms of the right formula and unify with the left formula.
            foreach (Formula subTerm in leftSubterms)
            {
                // according to the paper cited above we can skip variables 
                // and constants since they will not produce critical terms 
                // of any value
                if (subTerm is Constant)
                    continue;
                if (subTerm is Variable)
                    continue;

                var unification = await Formula.Unify(subTerm, right);
                if (unification != null)
                {
                    Formula criticalTerm = await left.CreateSubstitutionInstance(unification);
                    criticalTerms.Add(criticalTerm);
                }
            }
            var rightSubterms = right.AllSubterms;
            foreach (Formula subTerm in rightSubterms)
            {
                if (subTerm is Constant)
                    continue;
                if (subTerm is Variable)
                    continue;

                var unification = await Formula.Unify(subTerm, left);
                if (unification != null)
                {
                    Formula criticalTerm = await right.CreateSubstitutionInstance(unification);
                    criticalTerms.Add(criticalTerm);
                }
            }

            return new List<Formula>(criticalTerms);
        }
    }


}

