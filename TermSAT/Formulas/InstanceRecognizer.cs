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
using System.Collections.Generic;
using System.Collections.Immutable;
using TermSAT.Common;
using System.Linq;

namespace TermSAT.Formulas
{



    /**
     * A utility for recognizing substitution instances of a set of formulas.
     * 
     * A InstanceRecognizer instance is populated with a set of formulas.
     * The InstanceRecognizer can then be used to determine if a given formula is a 
     * <a href="http://en.wikipedia.org/wiki/Substitution_(logic)">substitution instance</a>
     * of any of the formulas in the InstanceRecognizer.
     * 
     * Terminology used in TermSAT... 
     * ...the result of applying a substitution to a formula, F, is called a substitution instance of formula F.
     * ...if S is the formula that is the result of applying a substitution to formula F then F is called a 
     *  generalization of formula S
     * 
     * InstanceRecognizer implements an efficient method for recognizing substitution instances.
     * A InstanceRecognizer builds an internal trie structure for representing all 
     * the given formulas that enables it to avoid many comparisons, thus making it 
     * much more efficient than testing all formulas individually.      
     *  
     *  
     * @author Ted Stockwell
     *
     */
    public class InstanceRecognizer : TrieIndex<string, Formula>
    {
        public InstanceRecognizer() { }

        public InstanceRecognizer(ICollection<Formula> formulas)
        {
            foreach (var f in formulas) {  Add(f); }
        }

        public void Add(Formula formula) => Add(formula.GetIndexingKey(), formula);

        /**
         * Returns a list of formulas in this InstanceRecogniser that are generalizations of 
         * the given formula.
         * @param maxMatchCount maximum # of matches to find
         */
        public IList<SubstitutionInstance> FindGeneralizations(Formula formula, int maxMatchCount)
        {
            var matches = FindGeneralizationNodes(formula, maxMatchCount);
            if (matches == null)
                return ImmutableList<SubstitutionInstance>.Empty;
            return matches.Select<SearchResult, SubstitutionInstance>(match =>
                new SubstitutionInstance(match.Node.Value, match.Substitutions)).ToList();
        }

        /**
         * Returns a list of all formulas in this InstanceRecogniser that are 
         * generalizations of the given formula.
         */
        public IList<SubstitutionInstance> FindAllGeneralizations(Formula formula) => FindGeneralizations(formula, int.MaxValue);

        /**
         * Returns the first formula found in this InstanceRecogniser that is a 
         * generalization of the given formula.
         */
        public SubstitutionInstance FindFirstGeneralization(Formula formula)
        {
            var matches = FindGeneralizations(formula, 1);
            if (matches == null || matches.Count <= 0)
                return null;
            return matches[0];
        }


        public IList<SearchResult> FindGeneralizationNodes(Formula formula, int maxMatchCount)
        {
            var visitor = new RecognizerVisitor(formula, maxMatchCount);
            var matches = Accept(visitor);
            return matches;
        }

        public class SearchResult
        {
            public INode Node { get; private set; }
            public IDictionary<Variable, Formula> Substitutions { get; private set; }
            public SearchResult(INode node, IDictionary<Variable, Formula> substitutions)
            {
                Node = node;
                Substitutions = substitutions;
            }
        }

        /// <summary>
        /// This trie visitor finds all the formulas in a trie that are generalizations of the given formula.
        /// </summary>
        public class RecognizerVisitor : IVisitor<List<SearchResult>>
        {
            Stack<IDictionary<Variable, Formula>> _substitutions = new Stack<IDictionary<Variable, Formula>>();
            Stack<int> _position = new Stack<int>();

            public Formula FormulaToMatch { get; private set; }
            public int MaxMatchCount { get; private set; }

            public RecognizerVisitor(Formula formulaToMatch, int maxMatchCount)
            {
                MaxMatchCount = maxMatchCount;
                FormulaToMatch = formulaToMatch;
                _substitutions.Push(new Dictionary<Variable, Formula>());
                _position.Push(0);
            }

            public bool IsComplete { get { return Result != null && MaxMatchCount <= Result.Count; } }

            public List<SearchResult> Result { get; private set; } = new List<SearchResult>();

            public bool Visit(TrieIndex<string, Formula>.INode node)
            {
                int currentPosition = _position.Peek(); // current position within the formula to match
                var currentSubstitutions = _substitutions.Peek();
                var currentSymbol = node.Key;

                var instanceSubformula = FormulaToMatch.GetFormulaAtPosition(currentPosition);

                try
                {
                    if (Symbol.IsConstant(currentSymbol))
                    {
                        if (!currentSymbol.Equals(instanceSubformula.GetIndexingSymbol()))
                            return false;
                        currentPosition++;
                    }

                    // if this node is a variable then get the substitution associated 
                    // with the variable.
                    // If there is no substitution then create one 
                    else if (Symbol.IsVariable(currentSymbol))
                    {
                        if (currentSubstitutions.TryGetValue(currentSymbol, out Formula subtitute))
                        {
                            // A substitution already exists, if current subformula does not match previous 
                            // substitution then not a match 
                            if (!subtitute.Equals(instanceSubformula))
                                return false;
                        }
                        else
                        {
                            currentSubstitutions = new Dictionary<Variable, Formula>(currentSubstitutions)
                            {
                                { currentSymbol, instanceSubformula }
                            };
                        }
                        currentPosition += instanceSubformula.Length;
                    }
                    else if (currentSymbol != null)
                    {
                        // if the formula doesn't start with the symbol associated with 
                        // this node then the formula is not a match
                        if (!currentSymbol.Equals(instanceSubformula.GetIndexingSymbol()))
                            return false;

                        currentPosition++;
                    }

                    if (node.Children.Count <= 0)
                    {
                        // this node is a leaf but there is still formula left, so not a match
                        if (currentPosition < FormulaToMatch.Length)
                            return false;

                        // this node is a leaf and we have matched the entire 
                        // formula so we have found a match
                        if (Result == null)
                            Result = new List<SearchResult>();
                        Result.Add(new SearchResult(node, currentSubstitutions));
                        return false;
                    }

                    // this node is not a leaf but we're out of formula, so not a match
                    if (FormulaToMatch.Length <= currentPosition)
                        return false;

                    // keep searching
                    return true;
                }
                finally
                {
                    _substitutions.Push(currentSubstitutions);
                    _position.Push(currentPosition);
                }
            }

            public void Leave(TrieIndex<string, Formula>.INode node)
            {
                _substitutions.Pop();
                _position.Pop();
            }
        }
    }


}


//   protected List<NodeInfo> findUnifiableNodes(Formula formula, int maxMatchCount)
//   {
//       /**
//        * I originally intended to write an efficient unification procedure that navigates through the 
//        * trie nodes and eliminates whole branches of formulas, similar to the procedure in the 
//        * findMatchingNodes method.
//        * However, I found that it was too difficult for me to write the unification procedure in this way, 
//        * so I instead just navigate through the leaves of the trie and unify formulas using a more direct 
//        * and easier to understand procedure.
//        * I aspire to rewrite this method at some point in the future when I have a better grasp of 
//        * unification.   
//        */


//       // http://www.cs.trincoll.edu/~ram/cpsc352/notes/unification.html
//       // a= given formula
//       // b= formula stored in instance recognizer
//       // if a and b are both constants or empty list then 
//       //		if a.equals(b) then return Collections.emptyMap();
//       //      return null; 
//       // if a is a variable
//       //   if a occurs in b then return NULL;
//       //   return Map.create(a, b);
//       // if b is a variable
//       //   if b occurs in a then return NULL;
//       //   return Map.create(b,a)
//       // if a.length <= 0 || b.length <= 0 then return NULL;
//       // a0= first element of a
//       // b0= first element of b
//       // Map substitution1= unify(a0,b0)
//       // if substitution1 == null then return null;
//       // Formula a1= Formula.create(a, substition1);
//       // Formula b1= Formula.create(b, substition1);
//       // Map substitution2= unify(a1,b1);
//       // if (substitution2 == null) return null;
//       // return Map.union(substitution1, substitution2);
//       const int formula_length = formula.length();
//       List<NodeInfo> matchs =
//           accept(new Visitor<Formula, List<NodeInfo>>() {
//               ArrayList<NodeInfo> _matches;
//       Stack<Map<Variable, Formula>> _substitutions = new Stack<Map<Variable, Formula>>();
//       Stack<Integer> _position = new Stack<Integer>();  // position within given formula
//       {
//           _substitutions.push(Collections.< Variable, Formula > emptyMap());
//           _position.push(0);
//       }
//       public boolean visit(CharSequence key, TrieMap.Node<Formula> node)
//       {
//           int position = _position.lastElement(); // current position within the formula to match
//           Map<Variable, Formula> subs = _substitutions.lastElement();
//           try
//           {
//               char symbol = node.getChar();
//               Formula subformula = formula.FormulaAt(position);

//               // if this node is a variable then get the substitution associated 
//               // with the variable.
//               // If there is no substitution then create one 
//               if (Symbol.isVariable(symbol))
//               {

//                   // parse variable number out of key
//                   int end = key.length();
//                   int start = end - 1;
//                   while (0 < start)
//                   {
//                       if (Character.isDigit(key.charAt(start - 1)))
//                       {
//                           start--;
//                       }
//                       else
//                           break;
//                   }
//                   Variable var = Formula.createVariable(key.subSequence(start, end));

//                   Formula subtitute = subs.get(var);
//                   if (subtitute == null)
//                   {
//                       subs = new HashMap<Variable, Formula>(subs);
//                       subs.put(var, subformula);
//                   }
//                   else
//                   {
//                       // if current subformula does not match previous 
//                       // substitution then not a match 
//                       if (!subtitute.equals(subformula))
//                           return false;
//                   }
//                   position += subformula.length();
//               }
//               else if (subformula is Variable)
//               {
//                   Variable var = (Variable)subformula;
//                   Formula subtitute = subs.get(var);
//                   if (subtitute == null)
//                   {
//                       subs = new HashMap<Variable, Formula>(subs);
//                       subs.put(var, subformula);
//                   }
//                   else
//                   {
//                       // if current subformula does not match previous 
//                       // substitution then not a match 
//                       if (!subtitute.equals(subformula))
//                           return false;
//                   }
//                   position += subformula.length();
//               }
//               else if (Character.isDigit(symbol))
//               {
//                   return true; // proceed to end of variable
//               }
//               else
//               {
//                   // if the formula doesn't start with the symbol associated with 
//                   // this node then the formula is not a match
//                   if (symbol != formula.charAt(position))
//                       return false;

//                   position += 1;
//               }

//               if (node.getChildren().isEmpty())
//               {

//                   // this node is a leaf but there is still formula left, so not a match
//                   if (position < formula_length - 1)
//                       return false;

//                   // this node is a leaf and we have matched the entire 
//                   // formula so we have found a match
//                   if (_matches == null)
//                       _matches = new ArrayList<NodeInfo>();
//                   _matches.add(new NodeInfo(key.toString(), node, subs));

//                   return false; // stop searching this branch
//               }

//               // this node is not a leaf but were out of formula, so not a match
//               else if (formula_length - 1 < position)
//               {

//                   return false; // stop searching this branch
//               }

//               return true; // keep searching
//           }
//           finally
//           {
//               _substitutions.push(subs);
//               _position.push(position);
//           }
//       }
//       public void leave(CharSequence key, TrieMap.Node<Formula> node)
//       {
//           _substitutions.pop();
//           _position.pop();
//       }
//       public boolean isComplete() { return _matches != null && maxMatchCount <= _matches.size(); }
//       public List<NodeInfo> getResult() { return _matches; }
//   });

//	if (matchs == null)
//		return null;
//	return matchs;
//}




