using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TermSAT.Formulas
{
    /**
     * Represents a propositional formula.
     * 
     * @see wiki/formulas.md 
     * 
     * This system represents propositional formulas using ...
     * 	...constants TRUE and FALSE.
     *  ...variables.
     *  ...a negation operator 
     *  ...an implication operator.
     *  ...a nand operator .
     *  
     *  Because this system is, eventually, meant to be used to solve large and 
     *  complex formulas, care is taken to make the system as memory efficient as 
     *  possible.  
     *  
     *  There is only ever a single instance created of any particular formula.
     *  All subclasses of the Formula class cache instances of previously created formulas 
     *  so that only one instance of that formula is ever created.
     *  This makes it possible to optimize many routines by caching previously 
     *  considered formulas in a hashmap and then quickly identifying previously 
     *  considered formulas by their hashcode.
     *  Caching Formula instances also reduces memory use.
     *  Expressions are cached using weak references so it's possible that 
     *  a previously created formula is recreated with a different hashcode, but 
     *  that should only happen if there are no objects holding any references to 
     *  any previously created instance.
     *  
     *  #see PrettyFormula for converting formula to 'pretty' text. 
     *  
     * 
     * @author Ted Stockwell <emorning@yahoo.com>
     */
    abstract public partial class Formula : IEquatable<Formula>, IComparable<Formula>, IFlatTerm<Formula>
    {

        /// <summary>
        /// Provide implicit cast from strings to Expressions.
        /// 
        /// I'm on the fence about whether this is a good idea or not.
        /// All formula classes have these implicit casts defined, they make writing tests *very* readable.
        /// OTOH, I wonder if an implicit cast will become a source of hidden problems.
        /// I'm going to leave them in and leave this warning.
        /// Note: I think I originally wrote this code in 2019, no regrets as of 2024.
        /// </summary>
        public static implicit operator Formula(string formulaText) => FormulaParser.GetOrParse(formulaText);


        public int Length { get; }

        public string Text => ToString();

        /// <param name="length">The number of symbols in this formula</param>
        protected Formula(int length)
        {
            Length = length;
        }

        /// <summary>
        ///     This method implements TermSAT's ordering.
        /// </summary>
        public int CompareTo(Formula other)
        {
            if (other == this)
                return 0;

            if (this.Equals(other))
                return 0;

            // shorter formulas are simpler than longer formulas
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

            if (this is Nand nandThis)
            {
                if (other is Nand impOther) 
                {

                    // if we get to here then the formulas have the same length and are *not* equal.
                    // return the lexicographical comparison
                    var thisEnum = new FormulaDFSEnumerator(this);
                    var otherEnum = new FormulaDFSEnumerator(other);
                    while (otherEnum.MoveNext())
                    {
                        thisEnum.MoveNext();
                        if (thisEnum.Current is Nand)
                        {
                            if (!(otherEnum.Current is Nand))
                            {
                                return 1;
                            }
                        }
                        else if (otherEnum.Current is Nand)
                        {
                            return -1;
                        }
                        else
                        {
                            var value = thisEnum.Current.CompareTo(otherEnum.Current);
                            if (value != 0)
                            {
                                return value;
                            }
                        }
                    }

                    return 0;
#if DEBUG
                    throw new TermSatException($"Since these formulas are not equal, we shouldn't get here");
#endif
                }

                return -1; // nand comes before implication
            }

            {
                // at this point both formulas must be Implications
                // implications with simpler antecedents come first
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
        }

        /// Implement IEquatable<Formula>.Equals
        /// Expressions are singletons, ie formula references are equal when they reference the same object.
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


        /// <summary>
        /// Creates a new formula by replacing all the variables in this formula 
        /// that also occur in the given formula with new variables that 
        /// don't occur in the given formula
        /// </summary>
        public Formula CreateIndependentInstance(Formula formula)
        {
            var substitutions = new Dictionary<Variable, Formula>();
            var variables = formula.AllVariables;
            var newVariables = new List<Variable>();
            foreach (Variable variable in variables)
            {
                if (ContainsVariable(variable))
                {
                    for (int i = 1; ; i++)
                    {
                        Variable v = Variable.NewVariable(i);
                        if (!ContainsVariable(v) && !variables.Contains(v) && !newVariables.Contains(v))
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
            Formula independent = CreateSubstitutionInstance(substitutions);
            return independent;
        }

        /// <summary>
        /// Parses out the first formula from the beginning of the given string
        /// </summary>
        public static Formula GetOrParse(string formulaText) => 
            FormulaParser.GetOrParse(formulaText);
        public static T GetOrCreate<T>(string formulaText, Func<T> provider) where T:Formula => 
            FormulaParser.GetOrCreate(formulaText, provider);

        /// <summary>
        /// Returns substitutions that will convert the given formulas into a single formula 
        /// that is also substitution instance of both formulas.
        /// The goal of unification is to find a substitution instance which demonstrates
        /// that two seemingly different terms are in fact either identical or just equal.
        ///  
        /// @return null if no such substitution exists otherwise,
        /// an empty map if the formulas are equal,
        /// else returns a map of substitutions for each formula.
        /// </summary>
        public static IDictionary<Variable, Formula> TryUnify(Formula left, Formula right)
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
                if (right.ContainsVariable((Variable)left))
                    return null;
                return new Dictionary<Variable, Formula>() { [(Variable)left] = right };
            }
            else if (right is Variable)
            {
                if (left.ContainsVariable((Variable)right))
                    return null;
                return new Dictionary<Variable, Formula>() { [(Variable)right] = left };
            }
            else if (left is Nand)
            {
                if (!(right is Nand))
                    return null;
                Formula la = ((Nand)left).Antecedent;
                Formula ra = ((Nand)right).Antecedent;
                var ua = TryUnify(la, ra);
                if (ua == null)
                    return null;

                Formula lc = ((Nand)left).Subsequent;
                Formula rc = ((Nand)right).Subsequent;
                Formula lu = lc.CreateSubstitutionInstance(ua);
                Formula ru = rc.CreateSubstitutionInstance(ua);

                var uc = TryUnify(lu, ru);
                if (uc == null)
                    return null;

                var map = new Dictionary<Variable, Formula>();

                foreach (var v in ua.Keys)
                {
                    // must create composition of substitutions
                    Formula f = ua[v];
                    f = f.CreateSubstitutionInstance(uc);
                    map[v] = f;
                }
                foreach (Variable v in uc.Keys)
                {
                    map[v] = uc[v];
                }

                return map;
            }
            else if (left is Negation)
            {
                if (right is Negation)
                    return TryUnify(((Negation)left).Child, ((Negation)right).Child);
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
                var ua = TryUnify(la, ra);
                if (ua == null)
                    return null;

                Formula lc = ((Implication)left).Consequent;
                Formula rc = ((Implication)right).Consequent;
                Formula lu = lc.CreateSubstitutionInstance(ua);
                Formula ru = rc.CreateSubstitutionInstance(ua);

                var uc = TryUnify(lu, ru);
                if (uc == null)
                    return null;

                var map = new Dictionary<Variable, Formula>();

                foreach (var v in ua.Keys)
                {
                    // must create composition of substitutions
                    Formula f = ua[v];
                    f = f.CreateSubstitutionInstance(uc);
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

        public abstract bool ContainsVariable(Variable variable);

        /**
         * @return a list of all subterms.  Includes this formula.
         */
        virtual public ICollection<Formula>  AllSubterms
        {
            get
            {
                var subterms = new HashSet<Formula>();
                this.GetAllSubterms(subterms);
                return subterms;
            }
        }

        public Formula this[int index] => throw new NotImplementedException();

        abstract public void GetAllSubterms(ICollection<Formula> subterms);

        /**
         * Two rules are syntactically equal if they are identical except for variable names.
         * 
         */
        public static bool SyntacticallyEqual(Formula left, Formula right)
        {
            if (left == right)
                return true;

            var unification = Formula.TryUnify(left, right);
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
        public static ICollection<Formula> FindAllCriticalTerms(Formula left, Formula right)
        {

            HashSet<Formula> criticalTerms = new HashSet<Formula>();

            Parallel.Invoke(
                () =>
                {
                    // Go through all the subterms of the left formula and unify with the right formula.
                    var leftSubterms = left.AllSubterms;
                    foreach (var subTerm in leftSubterms)
                    {
                        // according to the paper cited above we can skip variables 
                        // and constants since they will not produce critical terms 
                        // of any value
                        if (subTerm is Constant)
                            return;
                        if (subTerm is Variable)
                            return;

                        var unification = Formula.TryUnify(subTerm, right);
                        if (unification != null && 0 < unification.Count)
                        {
                            Formula criticalTerm = left.CreateSubstitutionInstance(unification);
                            lock (criticalTerms) { criticalTerms.Add(criticalTerm); }
                        }
                    }
                },
                () =>
                {
                    // go through all the subterms of the right formula and unify with the left formula.
                    var rightSubterms = right.AllSubterms;
                    foreach(var subTerm in rightSubterms)
                    {
                        if (subTerm is Constant)
                            return;
                        if (subTerm is Variable)
                            return;

                        var unification = Formula.TryUnify(subTerm, left);
                        if (unification != null && 0 < unification.Count)
                        {
                            Formula criticalTerm = right.CreateSubstitutionInstance(unification);
                            lock (criticalTerms) { criticalTerms.Add(criticalTerm); }
                        }
                    }
                }
            );

            return criticalTerms;
        }

        public IEnumerator<Formula> GetEnumerator() => new FormulaDFSEnumerator(this);

        IEnumerator IEnumerable.GetEnumerator() => new FormulaDFSEnumerator(this);
    }





}

