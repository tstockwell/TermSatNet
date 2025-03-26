# Lucid Expressions: Boolean Expressions That Reduce to Canonical Form in Polynomial Time

This document describes Lucid Expressions (LEs).  
LEs are a version of [existential graph](https://en.wikipedia.org/wiki/Existential_graph) with a textual notation.  
LEs can be reduced to their canonical form in polynomial time.  

The basic structure of this document is as follows...  

- Introduction 

	 This section gives an overview of [existential graphs](https://en.wikipedia.org/wiki/Existential_graph) (EGs) and [reduction systems](https://en.wikipedia.org/wiki/Abstract_rewriting_system), the problems with EGs, and how LEs solve them.  
	
	- Logic and Reduction Systems 
		- A [logic system](https://en.wikipedia.org/wiki/Formal_system#Deductive_system) can be viewed as a kind of reduction system.  
		- A [reduction system can be a logic system](https://en.wikipedia.org/wiki/Rewriting#Logic).  
		- A good reduction system is sound, complete, terminates, and is confluent.  
		- A good logic system is sound, complete, and... *terminates, and is confluent?*  
		- Reduction systems with rules that are reversible are locally confluent.  
		- Rules that are guaranteed to simplify ordered expressions are also globally confluent and guaranteed to terminate.  

		> Therefore, an ordered rewrite system with reduction rules that are sound, complete, and reversible is also confluent and guaranteed to terminate.  
		And such a system can also be a system of logic. 

	- Existential Graphs and Reduction Systems
	
		- EGs are a system of logic for automated reasoning.
		- EGs are sound and complete, but not confluent.  Not many logic systems are, seems like a problem.  
		- EGs have 5 inference rules; insertion/erasure, iteration/de-iteration, and double cut elimination.  
			> Insertion/erasure, and iteration/de-iteration are *reversible, symmetric pairs of operations*.  
			Double-cut elimination is not.  
		- The cut elimination rule, because it's not reversible, destroys confluence in EGs.  


- Lucid Expressions  
	- LEs makes three changes to EGs; LEs are ordered, only use erasure and deiteration, and use NOR semantics.  
		> Dropping the cut elimination rule and adopting NOR semantics make LEs confluent while preserving completeness.  
		Defining a [path ordering](https://en.wikipedia.org/wiki/Path_ordering_(term_rewriting)) makes it possible to guarantee that rules only simplify expressions, and guarantees that reduction terminates.  
	- LEs use the constants T and F to represent an empty 'sheet of assertion' and an empty cut.
		> For example:  (r (((p q) r)  (p (p (p r)))))  
	- LEs are ordered.  
		> For example, (T a) is a simpler expression than (a T).  
	- LEs use two rules of reduction; Erasure(aka Eliminattion) and Deiteration
		> These rules work exactly the same as they do in EGs.
	- The inference ruled for LEs are sound, complete, reversible, and guaranteed to simplify expressions.  
		
- Reduction and Logic

	- Expressions are reduced by looking for opportunities to rewind/reverse an application of one of the inference rules.  
	- Completely reversing all inferences produces an expression that is canonical.  
	- The reduction method computes, and remembers, all *grounding cofactors* of all terms used by canonical expressions.  
	- The reduction method uses cofactors to discover opportunities to reduce expressions.  
		> Like EGs, LEs use abductive reasoning.
	- If there are no groundings then an expression is canonical.  
	- LEs can be reduced to thier canonical form in polynomial time.  
		> A satifying valuation can also be determined in polynomial time.  
	
- Implementation  

	This section contains pseudo-code for the reduction algorithm.  

- Conclusion

	Lucid expressions are a form of textual, ordered, existential graph that are capable of *tractable* automated reasoning.  

- Appendix: Completeness

	This section proves that the ME rules of inference are complete.  

- Appendix: Computational Complexity

	This section proves that the computational complexity of reduction is O(n4) in the very worst case.

## Introduction 

### Logic and Rewrite Systems

Logic systems can be viewed as abstract rewrite systems.  
For instance, [Modus Ponens](https://en.wikipedia.org/wiki/Modus_ponens) is famous logical rule of inference, 
but its also a brain-dead rewrite rule that can perform automated reasoning.  
The modus ponens rewrite rule is; if you have two expressions, A and A->B, then you can rewrite them as just A and B.  
Using this rule, one does not need to know anything about logic to do logical reasoning, 
reasoning is just a matter of mechanically and repeatedly applying this rule to a set of expressions.  
Modus Ponens all you need to know, but [knowing additional rewrite rules can make things easier](https://en.wikipedia.org/wiki/List_of_rules_of_inference).  
For instance, there's Double Negation Elimination, Conjunction and Disjunction Introduction, Absorption, and Resolution.  
All of those well-known logical inference rules are also rewrite rules.  
However, some of these inference rules have properties that other rules do not.  
Like confluence, not all rules are confluent.  

A good rewrite system is sound, complete, guaranteed to be confluent and terminate.  

If your rewrite system has rules that are reversible then your system is locally confluent.  
If your systems' rules are also guaranteed to simplify expressions then your system is guaranteed to terminate and is globally confluent.  
Therefore, a rewrite system with rules that are sound, complete, reversible, and guaranteed to simplify expressions 
is also globally confluent and guaranteed to terminate.  

ME's, the logic system presented in this document, 
are built with only reversible rules that are complete and guaranteed to reduce expressions, 
and are therefore guaranteed to be confluent and terminate.

### Existential Graphs and Rewrite Systems

EGs are a system of logic for automated reasoning.  
The inference rules for [EGs are sound and complete](#Existential_Graphs_of_Peirce), but not confluent.

EGs have 5 inference rules...
- insertion/erasure, equivalent to [Conjunction Introduction and Erasure](https://en.wikipedia.org/wiki/List_of_rules_of_inference#Rules_for_conjunctions) 
- iteration/de-iteration, equivalent to [Disjunction Introduction and Erasure](https://en.wikipedia.org/wiki/List_of_rules_of_inference#Rules_for_disjunctions) 
- and double cut elimination.  

Insertion/erasure, and iteration/de-iteration are *reversible, symmetric pairs of operations*, double cut elimination is not.  
Because it's not reversible, the [cut elimination rule destroys confluence](#Cut_Elimination) in EGs.  

The differences between LEs and EGs are...
- LEs introduce the constants T and F so that EGs may be represented in a textual notation.
- LEs don't use the cut elimination rule.
	- LEs adopt NOR semantics in order to preserve completeness after dropping cut elimination.  
	- The inference rules for LEs reduce to just 'neither,nor' introduction and erasure.  
- LEs are ordered, and rules may only be applied in ways that are guaranteed to simplify expressions.  

These changes are designed to transform EGs in a system that's sound, complete, and non-canonical expressions may always be simplified.  
In this way we will know that LEs are also confluent and that the minimization method is guaranteed to terminate.  

## Minimizable Expressions  

The most important thing about minimizable expressions is that they are minimizable :-).  
Being minimizable means that they can be mechanically reduced to their canonical form 
by repeatedly applying reduction rules until the process terminates.  

LEs are minimizable because they are *ordered* and their reduction rules are *reversible*.  
In order to create 

The most important things to know about minimizable expressions are...
- LEs use NOR semantics
	> That is, in LEs, (e f) is interpreted as e NOR f.  
	> In EGs, (e f) is interpreted as e AND f.  
- LEs dont use the cut elimination rule.
	> NOR semantics was adopted so that the cut elimination rule could be dropped and LEs would still be complete.  
- LEs are ordered.  
	> The ordering makes it possible to only apply rules when they *reduce* an expression.  
	> Since the ME rules are reversible the, ordering also guarantees that minimization will terminate.

with changes meant to make the inference rules confluent in addition to being sound and complete.  

The parenthese 
	- This section describes the syntax and semantics of LEs.  
	- LEs use the constants T and F to represent an empty 'sheet of assertion' and an empty cut.
	- LEs use NOR semantics instead of AND and NOT semantics like EGs.  
		> Adopting NOR semantics enables cuts to be removed from EGs.
	- Example:  (r (((p q) r)  (p (p (p r)))))  
	- LEs are ordered.  For example, (T a) is a simpler expression than (a T).  
	- LEs have four rules of inference; Insertion/Erasure and Iteration/Deiteration
		These rules work exactly the same as EGs.
	- The inference ruled for LEs are sound, complete, and reversible.  

### Syntax

The basic syntax is simple...
- The symbols T and F represent an empty 'sheet of assertion' and an empty cut.
- Variables are hexadecimal numbers, but using lower case letters.
- Two expressions may be bounded by parentheses (NOR operations) and separated by a space  
Examples; (T a), (a b), and (a (T (b a)))

LEs use parentheses as the edges of an EG *area* (aka page, or sheet, a part of an EG), 
and the symbols in-between the brackets as symbols in the area.  
The parentheses visually group elements together in a way that's similar to existential graphs.  

Peirce's EGs treat (x y) as a conjunction (AND operation), 
because doing so makes it easy for humans to understand graphs.  

But LEs prefer to treat (a b) as a neither,nor relation (NOR operation) 
because doing so enables LEs to drop the double cut elimination rule while preserving functional completeness.

### Common Function Definitions 

It's the ME convention to use capital letters, other than T or F, to represent expressions.
Let Vars(F) be a function that returns an ordered list of all the unique variables in a given formula.
Let HighVar(F) be a function that returns the highest numbered variable in a formula.  
Let Flatterm(F) be a function that enumerates the terms in an express in depth-first fashion.  
	> Example, (a (T (2 1))) has 3*2+1=7 terms; { (a (T (2 1))), a, (T (2 1)), T, (2 1), 2, 1 }.
Let Length(F) => Flatterm(F).Count()

### Ordering

Unlike EGs, LEs are ordered.  
The ordering is meant to be an enumeration of all possible expressions that would be created 
by constructing all the expressions in the transitive closure of a new expression with all previously enumerated expressions.

The ordering enumerates all expressions of 0 variables (the constants),  
then all expressions with just the variable 1,  
then all expressions with the variable 1 and the variable 2,  
then all expressions with the variables 1 and 2 and the variable 3, etc...  
Within each of those orders expressions are ordered by their length, and then by their left-hand sides.

Let Compare(A,B) be a function that return {-1, 0, 1} if {A < B, A == B, or A < B}.
Here's a pseudo specification for Compare...

1. Variable Ordering 
	if HighVar(A) < HighVar(B) then A coLEs before B and Compare(A,B) returns -1.
	if HighVar(A) > HighVar(B) then B coLEs before A and Compare(A,B) returns 1.
	
2. Term Count 
	Expressions with fewer terms are simpler than formulas with more terms.
	if Flatterm(A).Count < Flatterm(B).Count then A coLEs before B and Compare(A,B) returns -1.
	if Flatterm(B).Count < Flatterm(A).Count then B coLEs before A and Compare(A,B) returns 1.
	Note that the concept of term count defines an expressions *length*.
	
3. 	LHS <= RHS
	Expressions that are the same length and have the same HighVar are ordered by the complexity of their LHS.
	That is, expressions with less complex LHS come before those with more complex LHS.
	Expressions that are the same length and same variable order and have equal LHS are ordered by the complexity of their RHS.  

4. F coLEs before T

Examples..
Rule 1: T and F come before any other expressions
Rule 1: 1 coLEs before 2, coLEs before (1 2), coLEs before 3, coLEs before a
Rule 2: (a a) coLEs before (a (a a))
Rule 3: F before T, (1 (T 1)) coLEs before (1 (1 1)), coLEs before ((T 1) 1)
Rule 4: F coLEs before T

### Inference Rules


## Minimizable Expressions vs Existential Graphs
## Minimization and Determining Satisfiability
## Implementation  
## Conclusion
## Appendix - Soundness
## Appendix - Completeness
## Appendix - Reversibility
## Appendix - Complexity


## Minimizable Expressions

	- OLE is a ordered, linear, and extended, notation for expressing (existential graphs)(https://en.wikipedia.org/wiki/Existential_graph).  
	- Example:  ((((p !& q) !& r)  !&  (p !& ((p !& r) !& p))) == r)
	- EGs are designed to be easy to read and understand, but are difficult to express in plain text, so OLE.  
	- OLE does its best to honor Peirces' original goals for EGs by...
		- using parentheses to define an 'area'.  
		- separating terms using space, multiple lines, and indentations.  
	- I honestly don't have a hunch whether Peirce would prefer Python or LISP.  
		> OLE is like LISP, with some conventions for improving readability.  
	- OLE is ordered so that there is one, and only one, canonical way to express a boolean function in OLE.  
		- That is, as far as EG's are concerned, (a T) is the same expression as (T a).  
		- But in OLE that's not true, because in OLE (T a) is a simpler expression than (a T).  
			> In OLE, an expressions A is simpler than an expression B if the lhs of A is simpler than the lhs of B.  



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




## References

### Dau
[Mathematical Logic with Diagrams, Based on the Existential Graphs of Peirce;Dau](http://www.dr-dau.net/Papers/habil.pdf).

Includes proofs of soundness and completeness for EGs

### Cofactors
[Linear Cofactor Relationships in Boolean Functions; Zhang, Chrzanowska-Jeske, Mishchenko, Burch](https://people.eecs.berkeley.edu/~alanmi/publications/2005/tcad05_lcr.pdf)

### Rewriting Logic 
[Rewriting Logic as a Logical and Semantic Framework; Mart�-Oliet, LEseguer](https://www.sciencedirect.com/science/article/pii/S1571066104000404)

### Cut Elimination
[Confluence as a cut elimination property; Dowek](https://inria.hal.science/hal-04103228v1/document)

I'm not the first person to notice that cut elimination is a problem.  

### Orderings
[Orderings for term-rewriting systems; Dershowitz](https://www.computer.org/csdl/proceedings-article/focs/1979/542800123/12OmNqBbI2S)

### Existential Graphs of Peirce
[Mathematical Logic with Diagrams, Based on the Existential Graphs of Peirce;Dau](http://www.dr-dau.net/Papers/habil.pdf).
