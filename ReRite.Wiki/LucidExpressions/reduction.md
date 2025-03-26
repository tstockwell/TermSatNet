## Reduction 

### Example

Reducing an expression removes all the inferences embedded in an expression and strips it down to its axiomatic, canonical form.  
If an expression reduces to T/F then the expression is a tautology/contradiction.  
If an expression reduces to anything but F then the expression is satisfiable.  

For instance, the expression...  
 ```A and (if A then B)```  
 ...is written as an LE like so...  
```(T (a (a (T b))))```  
...and, using deiteration, can be reduced to...
```(T (a (T (T b))))```  
...and using double-cut elimination, reduces to...  
```(T (a b))```  
...which is equivalent to...  
```A and B```  

The reduction process produces a simpler expression that is logically equivalent to the starting expression.  

### Overview

Expressions are reduced by looking for opportunities to rewind/reverse an application of one of the LE rewrite rules.  
Completely reversing all the inferences embedded in an expression produces an expression that is canonical.  

The reduction method performs three fundamental forms of inference...  
- Induction : LE always reduces an expression by first reducing the simplest subterm that's not already known to be canonical.  
	> Thus, LE is always working from simpler expressions to more complex expressions, and 
	> thus knows a lot about an expressions' subterms when reducing more complex expressions.  
- Deduction : Grounding Terms (represented as 2-variable Krom clauses) are calculated for every canonical subterm of an expression.  
	> When a clauses is added to the set of all groundings then resolution is used to deduce any new grounding terms, in polytime.  
	> Clauses are added when canonical subterms are discovered.  
	> Thus, discovering canonical subterms results in more opportunities to reduce more complex expressions.  
- Abduction : Grounding terms guide the application of reduction rules, and only those results that are simpler are valid reductions.  

### Cofactors and Reduction

#### Definition: Cofactor
A *term cofactor*, or just *cofactor*, of an expression E is an expression that is derived from E 
by replacing all instances of a given term S in F with a constant value C (T or F).  
Put another way, Cofactor(F,S) = F(S->C).  

#### Definition: Grounding Cofactor
A *grounding cofactor* is a cofactor that's logically equivalent to T or F.  
That is, if IsConstant(Reduce(Cofactor(E,S))) then Cofactor(E,S) is a grounding cofactor.

#### Definition: Grounding Term
If Cofactor(E,S) is a grounding cofactor then S is called a *grounding term* of E.  
When Cofactor(E,S) is logically equivalent to F/T then S is called a *negative/positive grounding term*.  
A grounding term S of E is subterm that, when all instances of S in E are replaced with a given constant, 
**forces or grounds** E to a constant value.


There's a relationship between grounding terms and the iteration/deiteration rules...  
the iteration/deiteration rules may be applied to expressions where on side of the expression 
has a negative 
An expression produced by an application of iteration or deiteration produces an expression where 
one side of the expression is a grothe positive cofactor of the other.  
Just take a look at LE's four iteratation/deiteration rules...
			- (L(T) R(S)) => (L(T->S) R(S)),     
			- (L(S) R(T)) => (L(S) R(T->L)),  
			- (L(S) R(S)) => (L(S->T) R)      
			- (L(S) R(S)) => (L(S) R(L->T)),  
- E[(L X(T))] => E[(L X(T->L))] ; ignore, not a reductive rule
- E[(L X(L))] => E[(L X(L->T))] ; L's sibling is replaced with L's positive cofactor
- E[(X(T) R)] => E[(X(T->R) R)] ; ignore, not a reductive rule
- E[(X(R) R)] => E[(X(R->T) R)] ; R's sibling is replaced with R's positive cofactor



Note that applying the iteration rule to an expression produces a term cofactor.  
Note that applying the deiteration rule to an expression produces a term cofactor.  

The reduction method computes, and remembers, all grounding cofactors of all subterms in a *mostly-canonical* expression.  


The reduction method uses grounding cofactors to discover opportunities to apply the rules in a way that reduces an expression.  

A grounding cofactor can be represented by a tuple *Grounding(E,S,PN,G)*,  
where Grounding(E,S,PN,G) represents a cofactor E(S->PN) of an expression,  
where PN is a constant and E(S->PN)'s canonical form is a constant G.  
Meaning that when sub-expression S of E has the value PN then E reduces to G.  
Put another way, a grounding represents one of these implications; S->E, ~S->E, S->~E, ~S->~E.

Grounding are used to compute erasures and de-iterations.  
When one side of an expression contains a negative grounding then it can be erased from the other side (deiteration).  
If the other side of the formula is empty (T) then the term is iterated to one side (replacing the T) and then erased/deiterated.
	
Note that a grounding can be written in CNF form with clauses of size <= 2.  
Meaning that new groundings can be derived from existing grounding in polytime (aka krom logic).  
Also meaning that it's also easy to keep the set of groundings transitively, or maximally, complete.  
Think of all the cofactors discovered in an expression stored as a set of clauses. 
Then, whenever this set is expanded, resolution is performed until complete (in polytime) in order to derive any newly implied grounding and equivalences.

Groundings are conditional equivalences.  
Or, equivalences are 2 groundings where PN == G and PN != G.

If an expression has no grounding cofactors then an expression is canonical.  

### Mostly Canonical Expressions

An expression E = (x y) is a *mostly-canonical* expression if x and y are canonical but E is not.  
There's a big difference between mostly-canonical and all-canonical.  
With all-canonical, well, with all-canonical there's usually only one thing you can do :-).

### Reduction

This section contains a pseudo-code description of the Reduce function.   
The Reduce function accepts an expression and returns the canonical form of the expression.

```
Let CANONICAL = a global table of expressions known to be canonical.
Let GROUNDINGS = a global table of tuples that represent all known grounding cofactors and all derivable cofactors.

Function Reduce
{
	Let START = the expression to be reduced.
	Let NEXT =  the current reduction of START, initialized to START.
	While NEXT has terms not in CANONICAL
	{
		Let SMALLEST = the simplest term in NEXT that's not known to be canonical.  

		Compute and add all groundings of SMALLEST to GRONUDINGS.
			if SMALLEST is 

		if (SMALLEST == NEXT) // true when NEXT is mostly-canonical
		{
			For terms S that are common to both sides of an expression
			{
				If S is a negative cofactor of one side of NEXT then 
				{
					S can be erased from the other side of NEXT.  
					That is...  
					Let L and R be terms such that NEXT = (L R)
					If S is a negative cofactor of L then let NEXT = (L R(S->T))
					If S is a negative cofactor of R then let NEXT = (L(S->T) R)
				}
			}

			If one side of NEXT is empty (T) then 
				for any positive cofactor of the other side
					the cofactor is iterated to the empty side (replacing the T) and   then erased/deiterated.

			
		}
		else 
			Let REDUCED = Reduce(SMALLEST);
			Replace all instances of SMALLEST in NEXT with REDUCED.
	}

	Add NEXT to CANONICAL, if not already there.

	return NEXT
}
```

#### Equivalence

After computing groundings UKS can use them to discover erasures and de-iterations that can simplify an expression.  
When a simplification is discovered the fact is record in the ABOUT table as an equivalence between two expressions.   

A clause that records an equivalence between two formulas AND proof that its valid.
The 'proof' also provides the necessary data to reverse the operation.
Is equivalent to a line of identity in an existential graph.  
UKS computes several types; constant reductions, erasures, de-iterations.



### Complexity
- LEs can be reduced in polynomial time.  
	> The satisfiability of LEs can also be determined in polynomial time.  




