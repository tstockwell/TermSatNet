# ReRite - Fast, Scalable, Reusable, Knowledge Compilation


ReRite is a [rewrite system](https://en.wikipedia.org/wiki/Rewriting) that reduces [nand-based](https://en.wikipedia.org/wiki/NAND_logic) propositional formulas to their [canonical form](https://en.wikipedia.org/wiki/Canonical_form).  
RR is meant to be used as a [SAT solver](https://en.wikipedia.org/wiki/SAT_solver), and to solve any problem that can be modeled in propositional logic.  
RR works by examining a formula and discovering rewrite rules that can reduce the formula to its canonical form.  
RR is most often used with an optional *rule database* that gives RR the ability to remember and reuse the rules that it discovers.  

The fewer rules that RR needs to discover for itself, the smarter and faster it is.  
When RR doesnt need to discover *any* rules to fully reduce a formula then it reduces formula in polytime.  
The commercial version of RR uses a shared database hosted by the RR project.  
It captures and shares all the rules discovered by all the RR clients on the planet, creating a global logic server.  

This document is a reference to the design and implementation of the RR system.

The original RR design was the result of experiments that used the Knuth-Bendix completion method to 
enumerate the rules of a rewrite system that reduces propositional formulas.  
Because, at that time, the author had convinced himself that there would a finite number of them ;-).  
That didn't pan out, but the system had promise and the author continued to develop it until  
he discovered that he had also built a knowledge compilation system.  

Discovering the relationship between his own work and *knowledge compilation* enabled the author to benefit from 
previous academic works wrt knowledge compilation and prove properties of the RR system.  

The document...
	- describes the ReRite knowledge compilation structure as a SQL database.
		The database has the following tables...
			- FORMULAS : Caches various invariants used in queries, like text, length, var count, AND REDUCTION INFO.
				Every formula has a unique Id, > 0, that's used in the LOOKUP table and as a value for NextReductionId.  
				Formulas may specify a NextReductionId, which if the formula reducible, is the ID of the formula to which this formula reduces.  
				If NextReductionId is not null then RuleDescriptor is set to a structured string that describes the reduction.  
				Varcount is similar to the concept of *support*, where varcount assumes variables are normalized (from 1 to N).
				Contains two kinds of formulas... 
				- Canonical : Cannot be reduced further.  
					NextReductionId is set to 0 (to indicate that there is no next formula)
					RuleDescriptor set to "IS_CANONICAL".
					Canonical formulas are also reduction rules, 
						because if another formula is a substitution instance of a canonical formula then its reducible.
				- Mostly-Canonical : A formula F = |ab where a and b are canonical but F is not.
					Eventually, NextReductionId will be set to the Id of this formulas' canonical form, 
					and RuleDescriptor will be set to a description of a 
					Mostly-Canonical formulas are also reduction rules, 
						because if another formula is a substitution instance of a Mostly-Canonical formula 
						then that formula is reducible to the generalizations' canonical form.

			- LOOKUP : A prefix-tree of all the formulas in the FORMULAS table.  
				Used to quickly find generalizations of a given formula, and thus find rules that can reduce the given formula.

			- IMPLICATIONS : A statement that says... If expression A is true then expression C is true.
				Note that, if a formula has the form [A Y(x)], where x does NOT occur in A, 
				but we can show that [A[Tx]] is true (ie A->x) then we can apply the DeIteration rule to A.
				RR only maps implications between canonical expressions 

			- COFACTORS : Lists substitution instances of a Mostly-Canonical formula where all of the T's (or all of the F's) 
					on uno side of the formula are substituted with a term Z from the other side of the mostly-canonical formula 
					such that the two formulas are logically equivalent.
				Cofactors reduce to mostly-canonical.  Mostly-Canonical reduce to canonical.
			- GROUNDINGS : for some mostly-canonical formula F in the FORMULAS table, 
					a grounding, GROUNDING(long FormulaId, enum Side, bool FormulaValue, long TermId, bool TermValue), 
					is a subset of all the instances of some term in some formula such that 

    public int[] Positions {  get; init; }specifies a term that occurs on uno side of F in the formula that A substitution instance of a Mostly-Canonical formula where all of the T's (or all of the F's) 
					on uno side of the formula are substituted with a term Z from the other side of the mostly-canonical formula 
					such that the two formulas are logically equivalent.
		terms that can force a formula to ground (T or F)
- describes a polytime procedure for reducing a given formula to its canonical form given that all necessary rules already exists in rule db.  
- describes a polytime procedure for adding new rules to a nand-trie (conjoining)
- describes a polytime procedure for discovering new rules necessary to reduce a given formula
	> That is, a procedure that, given F = |ab where a and b are in canonical form, discovers the rule(s) needed to reduce F.

## RR Notation

RR notation is based on existential graphs.
RR notation is a subset of existential graphs.
RR is a restricted form of EG that's grounded in the the expressive adequacy of the NAND operator.  

> You can think of a 'normal' EG as being grounded in the expressive adequacy of AND and NOT operators.  
> Thinking in terms of AND or NOT makes it easy for humans to use EGs to create models.  
> But RR doesn't need to care about what an EG 'means', RR is only used to minimize graphs.  
> RR uses EGs to reason about propositional formulas that use only NAND operators.  
> This is because using only NAND operators restricts the valid forms of EGs to forms that can be efficiently minimized.  	

### Syntax

The basic syntax is simple...
- The constants T and F are expressions
	> The symbol T represents an expression that's always true.  
	> You can think of T as the equivalent of an empty existential sheet.
	> The symbol F represents an expression that's never true.  
	> You can think of F as an empty cut in an EG graph.  
- Variables, expressed as numbers starting from 1 are also expressions.
- Two expressions may be bounded by brackets (NAND operation) and separated by a space
	> Examples; [T 1], [1 2], and [1 [T [2 1]]]
	> Spaces may be excluded between literals and brackets: [1[T[2 1]]]


RR expressions are inspired by existential graphs.  
RR expressions have the same *form* as EGs, 
and RR expressions support the same simplification rules as EGs, 
but have different semantics than EGs.
Computers don't have to care about what graphs mean, 
computers only need to understand the structure of graphs, 
so RR can use a form of EG that's not easy for humans to understand.

RR uses brackets [] to define an 'area' that contains exactly two other expressions.  
Think of brackets as the edges of an *area* (aka page, or sheet, a part of an EG), and the symbols in-between the brackets as symbols in the area.  

RR's expression syntax used to use a single *Sheffer Stroke* '|' but RR switched to EG-like bracket syntax.  
The brackets visually group elements together in a way that's similar to existential graphs.  
I'm convinced that the visual nature of EGs make them easier for humans to understand than linear notations 
that use a single character to represent operations.  
But as a computer programmer I find a linear data structure to be far easier to work with.  
While I (ts) dont get the benefit of that 'visual grouping' when using a single '|', I do when using opposing brackets.  
I think that the brackets have a kind of visual gestalt that works in a fashion similar to EGs.  
RR's notation is designed to appeal to both sides of my brain.  

Peirce's EGs treat [x,y] as an AND operation, because doing so makes it easy for humans to understand and minimize relatively simple graphs.
But reducing large EGs is computationally expunontial, so EGs take a lot of work minimize.  
And reducing logic is part of understanding logic, so in that sense EGs are hard to understand.  

RR evaluates [x,y] as a NAND operation, because doing so makes it easy to minimize expressions, 
and in that sense also make expressions as easy as possible to understand.  
For the humans, RR expressions can be efficiently translated to EG graphs.  
RR is the opposite of EG in the way that NAND is the opposite of AND.


## Common Definitions 

Let Compare(A,B) be a function that return {-1, 0, 1} if {A < B, A == B, or A < B} 

Let Vars(F) be a function that returns an ordered list of all the unique variables in a given formula.

Let HighVar(F) be a function that returns the highest numbered variable in a formula.  

Let Flatterm(F) be a function that enumerates the terms in an express in depth-first fashion.  
	> Example, [1,[T,[2,1]]] has 3*2+1=7 terms(aka clauses); [1,[T,[2,1]]], 1, [T,[2,1]], T, [2,1], 2, 1.

Let Length(F) => Flatterm(F).Count()

## Expression Ordering

Unlike EGs, RR formulas are ordered.
The ordering enumerates all expressions of 0 variables (the constants), then 1 variable, then 2 variables, etc.  
The first expression in the RR expression ordering with a specific truth function is the canonical expression of that truth function.  
The ordering is specifically designed to support a completeness proof by induction on the # of variables 
and to implement the concept of canonicity.  

1. Variable Ordering 
	Respect the induction order.
	if HighVar(A) < HighVar(B) then A comes before B and Compare(A,B) returns -1.
	if HighVar(A) > HighVar(B) then B comes before A and Compare(A,B) returns 1.
	
2. Term Count == Formula Length
	Expressions with fewer terms are simpler than formulas with more terms.
	if Flatterm(A).Count < Flatterm(B).Count then A comes before B and Compare(A,B) returns -1.
	if Flatterm(B).Count < Flatterm(A).Count then B comes before A and Compare(A,B) returns 1.
	The # of unique terms in a formula is equal to twice the # of bracket pairs (aka areas) + 1. 
	Example, [1,[T,[2,1]]] has 3*2+1=7 terms(aka clauses); [1,[T,[2,1]]], 1, [T,[2,1]], T, [2,1], 2, 1
	Note that the concept of term count defines an expressions *length*.
	
3. 	LHS <= RHS
	Expressions that are the same length and same variable order are ordered by the complexity of their LHS.
	That is, formulas with less complex LHS come before those with more complex LHS.
	Expressions that are the same length and same variable order and have equal LHS are ordered by the complexity of their RHS.  
	This rule also makes sure that F comes before T

Examples..
Rule 1: T and F come before any other expressions, 1 comes before 2, [1 2] comes before 3
Rule 2: [1 1] comes before [1[1 1]]
Rule 3: F before T, [1 [T 1]] comes before [1 [1 1]], comes before [[T 1] 1]

## The RR Minimization Method

RR reduces non-canonical expressions of the form [L R], 
where L and R are canonical expressions, to logically equivalent, canonical expressions.  

RR works by analyzing an expression from the bottom up, and as it does so, 
discovers the reduction rules needed to reduce the expression to its canonical form.  

Unlike EGs, RR expressions are reduced from the bottom up.  
Simplifying expressions from the bottom up is especially efficient because RR compiles knowledge as simpler expressions are reduced.  
That compiled knowledge is then re-used to reduce more complex formula built from the simpler formulas.  

RR uses a number of rules to reduce expressions.  
RR repeatedly applies rules to an expression until no more rules may be applied, at which point the result is canonical.  

### EOR: Expression Ordering Rule
If an expression A has the form [L R] and Compare(L,R) > 0 (ie R < L) then A reduces to [R L].  

This rule is sound because the result is logically equivalent and produces an expression that is 'reduced' 
in the sense that it precedes the starting expression in RR's expression ordering.

### UER: Unit Elimination Rules

RR calls these *unit elimination* rules... 
- [TT] => F
- [F x] => T, [FF] => T
	> Basically any area that contains an F will always evaluate to true and therefore may be reduced to just T.

These rules are the RR equivalent of Peirce's Double Cut Erasure Rule 3e.
These rules are sound for RR because the propositional expressions formed by the rules are tautologies.  

### Peirce's Iteration Rules
> Peirce's Rule 2:
> (i) Iteration: any EG-element in an area A can be duplicated in A or in any nested areas within A.
> (e) Deiteration: any EG-element whose occurrence could be the result of iteration may be erased.

Peirce's Deiteration Rule 2e applies to RR expressions as well as to EGs.
RR doesnt perform Iteration Rule 2i, RR never applies a rule that causes the length of the formula to grow, 
this plays a part in RR's complexity proof.  

Here's the RR version of Rule 2e... [x Y(x)] => [x Y(x/T)].
This rule says that given a formula A = [x Y(x)], 
where x is some term and Y is a term that contains uno or more instances of x, 
then A can be reduced by replacing some or all instances of x in Y with T.

You can think of T as an empty area, so replacing a term/area with T is the same as removing all elements from that area.  
The replacement will result in a new formula A' where A' < A in RR's expression ordering.  
Most often such a replacement will result in a A' where Length(A') < Length(A).  
If Length(A') == Length(A) then A' is canonical and can't be further reduced.  

The RR version of Rule 2i is the opposite operation... [x Y(T)] => [x Y(T/x)].
Since RR is a subset of EGs that always requires exactly two areas.  
RR uses T to represent an empty area, so the rule [x Y(T)] => [x Y(T/x)] says 
that any EG-element in an area A can be duplicated in A or in any nested areas within A.  
RR avoids using this rule because it will always result in a formula that is "more complex" than the starting formula,  
in order to be able to prove a polynomial time complexity, RR chooses to always reduce formulas.  
RR uses BIGs instead of rule 2i because BIGs efficiently guide the process 

The DeIteration rule is sound for RR because logically...  
when x == F then E will always evaluate to T, 
so uno can replace all x's in Y with T without changing A's truth function.  
The Iteration rule is sound for RR because logically...  
when x == F then E will always evaluate to T, 
so uno can replace all T's in Y with x without changing A's truth function.  

### HTRR Hyper Transitive Reduction Rule
Hyper Transitive Reduction Rule = Peirce's DeIteration Rule + RIGs

Note that, if a formula has the form [A Y(x)], where x does NOT occur in A, 
but we can show that [A[Tx]] is true (ie A->x) 
then we can apply the DeIteration rule.

[Francès de Mas,Bowles](#EGs) shows that BIGs can used to figure out these kind of reductions, in polytime.  
And shows that if an area A is not reducible using BIGs then A is acyclic 
BUT the *transitive reduction* of the BIG for A can still remove any redundant sub-expressions.  

RR computes BIGs for all sub-expressions as it proceeds.  
And then uses those BIGs to build BIGs for outer expressions.  
And uses the transitive reductions of those BIGs instead of rule 2i.

#### RR Implication Graph (RIG)
But wait there's more!...  
RR also uses the term-based generalization of BIGs described in [Francès de Mas,Bowles](#EGs).  
These BIGs are hypergraphs of implications between expressions, not just literals.  
RR also adds its own constraint on the definition of a BIG...  
In RR BIGs are hypergraphs of implications between *canonical* expressions.  
RR can't keep calling these things BIGs, so RR calls them RIGs for *RR implication graphs*. 
RIGs are implemented RR as the IMPLICATIONS table, see [the RR Database Schema](#RR_Database_Schema).

NOTE: The method that RR used to call 'wildcard analysis' is only capable of replacing redundant terms.  
What RR used to call 'wildcard swapping' used a kind of unification to do what 

### EPR: Equivalence projection rule
	Equivalent to EG Rule 2e (De-Iteration)

### OSIR: Opposite singletons implication rule

### TWSR: 


## References

### DNNF
[Decomposable Negation Normal Form; A Darwiche](https://citeseerx.ist.psu.edu/document?repid=rep1&type=pdf&doi=c1beb47fc0bdca63bc7c1cc4038072a7ed3fa758)

Describes a target knowledge compilation language.  
What makes this paper of interest to RR is that RR already had some-grown concepts similar to *conditioning*

This document defines the concept of 'conditioning' and uses it, along with satisfiability checking, 
to implement term entailments checks (I use the word 'term' instead of 'clause').  

> PS: I think the concept of conditioning a formula is the same as creating 
> a negative or positive cofactor of a function with respect to variable x. 

Nand-Tries also uses conditioning and satisfiability checks to discover term entailments, 
and then uses these entailments to logically reduce formulas and thus produce new reduction rules.

> From a Nand-Tries perspective, 'knowledge compilation' is the gradual expansion of a *nand-trie* with new reduction rules.
> New reduction rules are created when a nand-trie is used to minimize a given formula and the 
> trie is not yet *complete for that formula*.

### LCRs
[Linear Cofactor Relationships in Boolean Functions; J.S. Zhang; M. Chrzanowska-Jeske; A. Mishchenko; J.R. Burch](https://people.eecs.berkeley.edu/~alanmi/publications/2005/tcad05_lcr.pdf)

The important thing that I got out of this paper was this...
```
In this paper, we study linear (EXOR-based) relationships among any non-empty subset of the four two-variable cofactors of a Boolean function. 
These linear cofactor relationships represent sufficient conditions for the minimization of all of the above mentiunod decision diagrams (DD).  
```
Meaning that, calculating the LCRs for all 2-variable cofactors should be sufficient to minimize an RR database.  
It also puts a cap on the amount of work that needs to be performed to reduce a formula by uno step, O(4n(n-1)).

What this paper calls a 'cofactor' is the same thing as 'conditioning' a formula (when performing a clausal entailment check of a DNNF).  
Though, in this paper there are negative (x <- 0) and positive (x <- 1) cofactors of a function with respect to variable x.  
And wrt DNNF, *conditioning* a formula means just constructing the positive cofactor.

### Flatterms
[Flatterms, discrimination nets, and fast term rewriting; Christian J]()
Flatterms are a super useful way to represent formulas.  
Especially, when used with discrimination trees.

### EGs
[A novel framework for systematic propositional formula simplification based on existential graphs; Francès de Mas; Bowles](https://arxiv.org/pdf/2405.17072)
A propositional simplification method based on Existential Graphs.  

It seems to me that EGs can just as well be used to represent nand-based formulas.  
And that the same reduction rules apply.
In fact, this paper says exactly that, at the beginning of chapter 3...
```
Charles S. Peirce (1839-1914) provided the following sound and complete set of inference rules for EGs, 
where an *EG-element* is any arbitrary term, or clause, expressed as an EG: 
1.	(i) Insertion: in an odd area(nesting level 2k+1,k∈N),wecandrawany EG-element. 
	(e)Erasure: anyEG-element inanevenarea(nestinglevel2k,k∈N)canbe deleted. 3Wereferheretothegenericnotionof ‘clause’,understoodasapropositional formulaconsistingofa finitecollectionof literalsandlogicalconnectives.6 
2.	(i) Iteration: any EG-element in an area a can be duplicated in a or in any nested areas within a. 
	(e) Deiteration: any EG-element whose occurrence could be the result of iteration may be erased. 
3.	(i) Double Cut Insertion: a double negation may be drawn around any collection of zero or more EG-elements in any area. 
	(e) Double Cut Erasure: any double negations can be erased.
```

I think that the main contribution of this paper is that it orders the application of Peirce's rules so that... 
	- graphs never grow in size, thereby guaranteeing a monotonically decreasing number of variables, lauses and literals 
	- preserves equivalence and other structural problem information. 

However, from what I know about reduction systems, this re-ordering of rules 
is probably only necessary because the reduction rules themselves are not *confluent*.  
And I suspect that in order to produce a confluent set of rules that it would be necessary to apply 
an RR-like ordering to EGs.  



## RR Database Schema