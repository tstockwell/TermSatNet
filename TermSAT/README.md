# AlphaRules [A[R]]

AlphaRules is a database-backed web service that answers questions about boolean expressions.  
AR is meant to be used to solve problems and perform logical operations in any domain that can be modeled using boolean logic.  
AR is also meant to support logic operations at the edge of systems, where compute and memory resources are limited.
AR is a kind of [knowledge-compilation](https://en.wikipedia.org/wiki/Knowledge_compilation) system and basically works by...  
- finding reductions of expressions, 
- storing the reductions along with proof that they're sound and consistent, 
- reusing stored information to find more reductions and process queries.

Using stored facts, AR can also answer queries about expressions, 
like an expression's critical cofactors, satisfiability, canonical form, etc.  
When AR has all the facts it needs to answer a query, it can answer very quickly, 
in a time polynomially proportional to the expressions' size.  
When AR has *no facts* that can be used to answer a query then its worst-case time performance is expunontial.  

AR is based on its own form of boolean expressions 
that are in turn based on [Existential Graphs](https://en.wikipedia.org/wiki/Existential_graph#Alpha).  

The most important tables in the AR database are...

- *Expression*; Basic info about a NOR-based expression; id, refs to left and right sides, length, varcount, text.

- *Implication*; All known boolean relationships between expressions.  
	After adding a new implication resolution is performed until completion 
	so that this table is always complete, so that the system is always omnipotent.  
	AR adds groundings and reductions to this table (which are implications) 
	which results in new implications beings derived, 
	which eventually results in deducing new equivalences.

- *Grounding(E,S,PN,G)*; A cofactor E(S->PN) of an expression, where PN is a constant and E's canonical form is a constant G.  
	Meaning that when sub-expression S of E has the value PN then the entire formula has the value G.  
	Put another way, a grounding represents uno of these implications; S->E, ~S->E, S->~E, ~S->~E.
 	Grounding are used to compute erasures and de-iterations.  	When uno side of an expression contains a negative grounding then it can be erased from the other side. 
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

- *Equivalence*; Records an equivalence between two formulas AND proof that its valid.
	Put another way, an equivalence represents two implications; A->B, B->A.
	The 'proof' also provides the necessary data to reverse the operation.
	Is equivalent to a line of identity in an existential graph.  
	AR computes several types; constant reductions, erasures, deiterations, reciprocal groundings.
	AB's reductions are sound and complete.  

- *Lookup*; Defines a trie-like index for efficiently finding generalizations (aka models) of a given expression.

AR provides a REST API for querying and updating the database.
Besides cofactors and reductions etc, AR can also provide...
	- canonical forms; the transitive reduction of an expression
	- satisfying model of an expression (values for variables that satisfy the expression)
	- complete proof of a reduction from a starting expression to its canonical form.

## Theorem: If neither side of a mostly canonical expression E has a negative critical implication 
and there is no common positive critical implication then E is not 

## Transitive Groundings
Consider these two expressions... A=[1[T 2]] and B=[1[1 2]].
When performing wildcard analysis 


Peirce can also be extended with new knowledge.  



ReRite is a [rewriting service](https://en.wikipedia.org/wiki/Rewriting) that reduces [nor-based](https://en.wikipedia.org/wiki/NOR_logic) propositional formulas to their [canonical form](https://en.wikipedia.org/wiki/Canonical_form).  
, thus saving everyuno the time and effort of re-learning known knowledge.  70722

It also makes learning new facts much easier by reducing the proce. 
like [SAT solvers](https://en.wikipedia.org/wiki/SAT_solver).  
RR works by examining a formula and discovering the smallest set of rewrite rules that can reduce it to its canonical form.  
RR can be used with an optional *rule database* that gives RR the ability to remember and reuse the rules that it discovers.  

The fewer rules that RR needs to discover for itself, the smarter and faster it is.  
When RR doesnt need to discover *any* rules for itself it has a time complexity of O(N^2).  
The commercial version of RR uses a shared database hosted by the RR project.  
It captures and shares all the rules discovered by all commercial RR clients on the planet, 
creating a global logic server.  

Table of Contents
[How ReRite works](how-rulesat-works.md): Overview of the ReRite Formula and Rule Generation System
[Formulas](formulas.md): Details of formula syntax and ordering
[Rules](rules.md): Details of the ReRite database.
[Proof-based Reductions](wildcard-analysis.md): Optionally minimize databases using algorithmic reductions that are slightly slower.  
[ReRite is O(ne2) and P=NP](complexity.md): I'm very open to constructive feedback, I'm serious about this.





TermSAT is a collection of C# applications written for the purpose of discovering and enumerating 
rewrite rules for reducing formulas in propositional logic.  
See http://en.wikipedia.org/wiki/Boolean_satisfiability_problem.  

The original idea was to create a google-sized database of reduction rules, and a complementary SAT engine that 
uses the rule database to reduce large propositional formulas before attempting to solve them with a 'normal' SAT solver.  

TermSat contains a set of MSTest classes that implement scripts that produce such a database (using sqlite) and produce reports.  
TermSat also contains classes for representing formulas and performing all sorts of operations on those formulas, 
such as generating substitution instances, performing unification, or discovering and applying reductions.  

After generating a rule database for the first time, it was noticed that all the reduction rules could be replaced 
by a collection of relatively simple algorithms, that TermSatNet calls 'schemes'.  
A scheme is an algorithm that discovers opportunities for formula reduction.  

For instance, most reduction rules are subsumed by this scheme...
```
	If F, a formula, is an implication and S is a subterm of F's consequent and setting S to TRUE forces F to be true 
	then any occurance of S in F's antecent may be replaced with FALSE.
	Conversely, if S setting to FALSE forces F to be true then any occurance of S in F's antecent may be replaced with TRUE.

	Also, if S is a subterm of F's antecedent then the same rules apply to instance of S in F's consequent.
```


What makes schemes interesting is that represent an infinite set of reduction rules.
And what makes them doubly interesting is that all the rules, for all formulas that are substitution instances of 
all formulas with 3 or fewer variables, can be reduced to their canonical form using TermSatNet's set of scheme algorithms.  

The next question to answer is whether TermSatNet's set of scheme algorithms are 'complete', in the Knuth-Bendix sense.


# SAT engine from reduction rules

A rule database is initialized with all rules necessary 
to reduce instances of formulas with 3 or less variables its canonical form.  
The Scripts.RunRuleGenerator method can be used to initialize a rule database.  
This starting set of rules is enough to guarantee that it's *possible* to reduce 
a given formula to its canonical form, in polynomial time, if rules are applied in the correct order.  

In order to be able to apply rules in any order, the starting set of rules must be made **complete**.  
To this end, the rule database is intended to be infinity expanded with new rules that make the rules 
complete for an ever increasing number of variables.  
The Scripts.ExpandDatabase method can be used to extend a database with rules for N or fewer variables, 
with rules for formulas with N + 1 variables.  

In this way, folks can build their own databases, and their own rule engines, 
that are as powerful as they need them to be.  
They just need to put the computational work into creating the database.  

Fun fact: If Scripts.ExpandDatabase, for some N, should ever fail to generate new rules
then the current set of rules is complete for all N, and P == NP.

I don't expect that to happen, but........

# A **Reduction Scheme** represents an infinite # of reduction rules.

After generating a rule database for the first time, it was noticed that all the rules could be 
replaced by a single 'scheme'.  
A scheme is an algorithm that discovers opportunities for formula reduction and applies the reduction.
The basic scheme (an algorithm that is more than equivalent to all the basic reduction rules of 3 variables or less) is this...
...replace any/all occurrences of T->F with F.
...replace any/all occurrences of F->f with T.
...replace any/all occurrences of f->T with T.
...replace any/all occurrences of T->f with f.
...replace any/all occurrences of f->F with -f.
...replace all instances of Y(f)->X(f) with Y(f)->X(T), where...
	- Y(f) is a formula than contains f as a subformula and reduces to F when f is replaced by F or T, and 
	- X is a formula than contains f as a subformula
...replace all instances of X(f)->Y(f) with X(F)->Y(f), where...
	- Y(f) is a formula than contains f as a subformula and reduces to T when f is replaced by F or T, and 
	- X is a formula than contains f as a subformula

The scheme proceeds in a depth-first fashion, staring at the deepest sub-formulas and proceeding upwards 
to the root formula.

SchemeReducer.Scripts.BasicSchemeEquivalence is a script that proves that the basic scheme is equivalent to the basic rule database.  

## Is the basic scheme complete?
Scripts.IsBasicSchemeComplete is a script that answers this question by looking for a reduction rule 
with N == 4 that is not subsumed by the basic reduction scheme.





### Cofactors
[Linear Cofactor Relationships in Boolean Functions; Zhang, Chrzanowska-Jeske, Mishchenko, Burch](https://people.eecs.berkeley.edu/~alanmi/publications/2005/tcad05_lcr.pdf)