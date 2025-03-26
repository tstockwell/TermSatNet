## Lucid Reduction Algorithm

One possible implementation of Lucid Reduction is presented here.
The implementation Lucis is implemented using a database.
- 
UKS is a tool for solving problems in constraint programming, job scheduling, routing, protein folding, symbolic execution, formal verification, and the like.
The simplest thing that UKS does is find, and store, all the 'grounding cofactors' of the terms in an expression.  
After that, UKS can compute other properties of an expression, like satisfiability, and simplifications.  

UKS is a (*knowledge compiler*)(https://en.wikipedia.org/wiki/Knowledge_compilation).  
UKS analyses boolean expressions and produces a set of (Krom clauses)(https://en.wikipedia.org/wiki/2-satisfiability) 
that describe relationships between the terms in the expression, like grounding cofactors and equivalencies.  
After compiling, UKS can use the resulting clauses to determine if the expression may be simplified, 
or if the expression is satisfiable, or the canonical form of the expression, etc.
UKS simplification is sound, complete, and has a polynomial time complexity.  

UKS can reuse those clauses to help answer more difficult questions about the same expression, 
or to simplify other closely-related expressions, 
so it makes sense to store them somewhere so we can reuse them.

(	
	(
		if		
		you-like-flamigos
	)     
	own-it    
)
(you_like_flamingos -> Own_It)
<If cond="@you_like_flamingos">
	<Own_It/>
</If>

That is why UKS is implemented as a service.  
UKS is meant to be used to repeatedly solve problems in any domain that can be modeled using boolean logic.  
As UKS solves problems in that domain it gets better at solving those problems.  
UKS is polytime to begin with, but when solving many problems, or solving problems of ludicrous size, 
the reduction in the effort it takes to answer future questions is significant.  
UKS is also meant to support logic operations at the edge of systems, where compute and memory resources are limited.

## UKS DATABASE
Knowledge compilation languages usually focus intently on the structure that represents knowledge, 
in order to choose a structure that has some desired properties.  
UKS saves 'knowledge' in a database, this section describes the tables.

### EXPRESSION TABLE
Schema: (numeric Id, numeric LhsId, numeric RhsId, long Length, int Varcount)
Basic info about expressions.
An expression's Id can be derived from the expression, Id == UKS_NUMBER(expression)

### ABOUT TABLE
A table of 2-variable clauses that describe relationships between expressions.  
Specifically; groundings, equivalences, and all derived facts.

After adding a new clause, resolution is performed until completion .  
AR adds groundings and reductions to this table (which are implications with just 2 variables) 
which results in new implications beings derived, 
which eventually results in deducing new equivalences.

#### Groundings
 
A *Grounding(E,S,PN,G)* is a cofactor E(S->PN) of an expression, where PN is a constant and E's canonical form is a constant G.  
Meaning that when sub-expression S of E has the value PN then the entire formula has the value G.  
Put another way, a grounding represents uno of these implications; S->E, ~S->E, S->~E, ~S->~E.

Grounding are used to compute erasures and de-iterations.  
When uno side of an expression contains a negative grounding then it can be erased from the other side. 
If the other side of the formula is empty (T) then the term is instead de-iterated to the other side.
	
Note that a grounding can be written in CNF form with clauses of size <= 2.  
Meaning that new groundings can be derived from existing grounding in polytime.
Also meaning that it's easy to keep the set of groundings as transitively, or maximally, complete.  
Think of all the information stored in AR as a set of clauses. 
Then, after adding the clauses that represent a grounding resolution needs to be performed until complete 
in order to derive any newly implied grounding and equivalence.

Groundings can be calculated when formulas are added, plus they need to always transitive.
Groundings are conditional equivalences.
Or, equivalences are 2 groundings where PN == G and PN != G.

#### Equivalence

After computing groundings UKS can use them to discover erasures and de-iterations that can simplify an expression.  
When a simplification is discovered the fact is record in the ABOUT table as an equivalence between two expressions.   

A clause that records an equivalence between two formulas AND proof that its valid.
The 'proof' also provides the necessary data to reverse the operation.
Is equivalent to a line of identity in an existential graph.  
UKS computes several types; constant reductions, erasures, de-iterations.

### LOOKUP TABLE
Defines a trie-like index for efficiently finding generalizations (aka models) of a given expression.

