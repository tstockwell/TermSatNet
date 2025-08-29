# Proofs

All of the structural rules in the SE system use cofactors to constrain and guide their application.  
In this document a procedure will be developed that efficiently computes cofactors.  

TODO:
First it will be shown that enough of the structural and cofactor relationships,  
required to find reducing cofactors,  
in a mostly-canonical expression,  
can be modeled with 2-CNF formulas.  

The process of creating a CNF model and computing its transitive closure is call *cofactor modeling*.  
Computing the transitive closure of 2-CNF formulas is known to be computable in O(N^3) steps (Krom) where 
N is the # of variables in the model.  
When cofactor modeling, all the subterms in an expression become the variables in the cofactor model.  
The number of variables in the cofactor model is therefore equal to the length of the expression.  

Therefore, if there are any reductive cofactors to be found then they can be found in O(N^3) steps where N is the length of the expression.  

TODO:
After cofactor discovery, SE finds the cofactor that can reduce the expression the most.  
It will be shown that if no reducing cofactors are computed then the expression is canonical.  

TODO:
Finally, it will be shown that...
- most reductions result in reducing the *length* of an expression.  
	- Only the single reduction just before the final reduction to canonical form can fail to reduce the length of an expression.  
	- Therefore it takes at most O(2N) reductions to fully reduce any given expression.
- it takes at most O(2N^4) steps to reduce any given unnormalized expression.  
	- it takes at most O(2N) reductions to reduce any given unnormalized expression.  
	- it takes at most O(N^3) steps to calculate cofactors for each reduction.

## Theorem: Cofactor Discovery is Polynomial




## Cofactor Logic

*Cofactor logic* is the name that SE gives to the custom logic it uses 
to identify opportunities to apply structural inference rules.  

SE could find grounding cofactors of an expression in polynomial time just by testing every term in the expression.  
If replacing the term cause the expression to reduce to a constant then you've found a ground cofactor.  
Each term in an expression can be tested in polynomial time, so all cofactors can be found in N times as much time, 
which is also a polynomial.

But using cofactor logic is far more efficient.

A *cofactor model* M is a set of clauses that model the relationships between the terms of a mostly-canonical expression 
such that if there exists an expression E', that is equivalent to E, and has a grounding cofactor S 
then the clause C that represents the cofactor is derivable from the model.

In other words, either M |- (S -> E'), or  M |- (S -> !E').  

SE uses Krom logic to discover cofactors since...  
- it works
- it's efficient, a cofactor models transitive closure can be computed in quadradic time.

Proofs in cofactor logic are polynomially bounded for the same reason as 2-CNF, 
there is a quadradic limit on the number of clauses that can possibly be generated. 

In this section a process for creating a cofactor model from a mostly-canonical expression is presented.  

### Modeling Variables

We start the construction of a cofactor model by defining the variables used in the model.  
Variables represent the sub-terms in an expression.  
Specifically, variables are numbers that correspond to the index of the term in an expression's flatterm.  

### Modeling a Context

Consider the expression (l r) and its flatterm...  
subterm 		index	
------------	-----	
(l r)		 	0		
l				1       
r				2		

The structural relationships in (l r) can be expressed with these clauses...  

- !1 -> 0; when the term at index 1 (l) has the value false then the term at index 0 must have the value true.
	> !l -> (l r)
- !2 -> 0; when the term at index 2 (r) has the value false then the term at index 0 must have the value true.
	> !r -> (l r)
- (1 & 2) -> !0; when the terms at indexes 1 and 2 have the value true then the term at index 0 must have the value false.
	> Note: (1 & 2) is equivalent to (T (l r))
	> So this rule is another way of saying (T (l r)) -> !(l r)

It's a problem that the last clause has three variables.  
If we can find a way to keep the # of variables to 2 or fewer then we can easily prove the 
polynomial limit on the complexity of cofactor logic.  
3 variables makes proving the polynomial limit very difficult.  

Here's how SE solves this problem...
Let a cofactor model of (l r) be a model of all relationships in the expression (T (l r)).  
That is, instead of modeling cofactors using a term's flatterm, SE uses the flatterm of the expressions' negation.

The flatterm for (T (l r)) is...
subterm 		index	
------------	-----	
(T (l r))		0		
T				1	
(l r)			2
l				3       
r				4		

All the structural relationships in (l r) can now be expressed with these clauses...  
- !3 -> 2	; when !l then (l r), same as 1st rule in previous example
- !4 -> 2   ; when !r then (l r), same as 2nd rule in previous example
- 0 == !2   ; (T (l r)) == !(l r), subsumes last rule in previous example but now with fewer variables

### Example
Find the F-grounding cofactor in ((1 (T 2)) ((T 1) 2))).  BTW its (1 2).
Given (T ((1 (T 2)) ((T 1) 2))), use cofactor modeling to show that (1 2) -> !((1 (T 2)) ((T 1) 2))

a horizontal flatterm...
```
(T ((1 (T 2)) ((T 1) 2)))  
01 234 56 7   891 1  1  
        		0 1  2  
```
Goal ? -> !2 
Axioms
-----------------------------
identify equivalent terms..
4 == 11
7 == 12
identify negations...
2 != 0
5 != 7
5 != 12
4 != 9
9 != 11
identify implications...
!3 -> 2
!4 -> 3
!5 -> 3
!7 -> 5
!8 -> 2
!9 -> 8
!12 -> 8
axiom...
0 ; in other words, we're looking for solutions where ((1 (T 2)) ((T 1) 2)) is false

proof
!2
3
8


## Cofactor Discovery 

Theorem...  
Let E be a mostly-canonical expression of the form (L R).  
If the cofactor discovery procedure does not generate f-grounding cofactors of L and R that identify a deiteration or reductive exchange 
then E is canonical.

First, prove...
If E has no opportunities for deiteration or reductive exchange then E is canonical.

Then prove...

a->b & b->c |- a->c
That is, 



Structured expressions 
Boolean expressions come in all kinds called *normla forms*.  
The basics of structured expressions are presented in another document. 
There, a normal form, called 

In this document several normal forms of structured expression are defined, 
each normal form is a constrained version of the previous normal form.  
In this way, 

Each normal form has specific properties
a method of reducing structured expressions  The material in this document Before reading this you should be familiar with the [basics of structured expressions](structural-logic.md).

And before continuing it's important to know that the SE proof procedure works in a copule of important ways...
1.  It reduces expressions from the bottom up, reducing inner before outer terms.
	> In this way SE is always reducing mostly-canonical expressions to canonical expressions.	
2.	It never takes more than N applications of the exchange rule to reduce the length of any given expression expression.  
	> It follows that any given expression can be reduced to its canonical form in no more than O(N^2) of the exchange rule 
	> where N is the length of the expression


The SE proof procedure is based on concepts of structural logic.  
Structural logic can be understood as the application of logical principles or reasoning 
to analyze or understand the **structure** of a system or object.  
Structural logic involves using logical thinking to examine the organization, arrangement, and interrelationships 
of all the components within a given structure.  

In SE, structural logic is used to analyze structured expressions 
by modeling the relationships between all the parts of an expression.  

In this document it is shown that there is enough information embedded in a mostly-canonical structural logic embedded in structured formulas can be 
modeled using Krom formulas (2-CNF).  
Doing so makes it possible to determine the satisfiability of the original formula in a 
polynomially bounded number of steps.


The SE proof method is a method that reduces a given expression to its canonical form.  


	- 

The SE proof method transforms expressions from one normal form to another, 
so we need to discuss those normal forms first.




## Wildcards and Linear Normal Form 

### Definition: A *wildcard* is a term in an expression that can be the target of iteration or deiteration.
	
Examples..
- (T 1) and (1 1), the lhs is a wildcard
- (1 (T 2)), (1 (1 2)), and (1 (2 2)), the T marks a wildcard

Note that the exchange rule is excluded from the definition of a wildcard, 
that's because the exchange rule can be implemented using iteration and deiteration.  

### Definition: When a wildcard term is T then the wildcard term is called *grounded*.

T represents an empty space, and acts as a kind of placeholder for copies of literals from outer contexts.  

### Lemma: Every instance of T in an expression is a wildcard.  
Proof... because every instance of T can be a target of the iteration rule

### Lemma: A wildcard can be replaced with any term from an enclosing context.  
That's just another way of phrasing the iteration rule.

Example... in (1 (2 2)) the leftmost 2 is a wildcard, 
and therefore it may be replaced with the 1 from the outer expression 
to produce (1 (1 2))

### Lemma: A wildcard can be replaced with T when the wildcard term exists in an enclosing context.
That's just another way of phrasing the deiteration rule

Example... in (1 (2 2)) the leftmost 2 is a wildcard, 
and therefore it may be replaced with T.  

### Definition: A *linear* expression is an expression where every wildcard in the expression is grounded.

Linear expressions are fundamental to LE's reduction method.  
LE only uses linear expressions because doing so makes it possible 
to reduce a given expression B to another expression A such that A < B.  

By reducing expressions in a way that always produces a simpler expression, 
LE is able to guarantee a polynomial limit on the size of reductive proofs.  

Definition: Given an expression E, any subterm in E that's not a wildcard is called a *literal*.  
Definition: The *domain* of any particular wildcard is the set of literals that may be validly substituted for the wildcard via iteration.  
Definition: An *input* is an element in a wildcards domain.  

Definition: When a subterm identified as a wildcard is not T then we say that the term has been *derived*.

### Theorem: A single source for each for each possible wildcard term
For any wildcard W and any term P in W's domain, there is never more than one literal that provides P to W.  
	> Example: (2 (T 2)), it looks like the T is targeted by two literals, 
	> but the rightmost 2 is a wildcard, not a literal.
	> The leftmost 2 is a literal because its the outermost instance, and thus serves as the source of truth.
	> If there are more than one instance of a literal targeting a wildcard then one of those instances is really a wildcard.  
Follows from iteration/deiteration, since all inner instances and left sibling instances are wildcards.


### Theorem: A canonical expression does not contain any derived wildcards.
	> If a canonical expression had a derived term then that term could be replaced with T to produce an simpler, equivalent expression.  


### NonLinear to Linear Conversion



What is the max # of such clauses in a transitively complete system?

Given a mostly-canonical expression E of the form (L R), if L and R have no F-grounding 
where 
-----------------------


Proof is by induction on the # of variables.
Let E be the expression to derive.
Let N be the number of parentheses pairs in E, where 0 <= N.

When N == 0 then E is either T, F, or a variable.
Each of these has a single step proof and therefore the theorem holds true for N == 0.  

When N == 1 then E is one of the following expressions with a single variable and one parentheses pair...  
- (T F) ; == T
- (T T) ; == F
- (T a) ; == !a, canonical
- (F T) ; == T
- (F F) ; == T
- (F a) ; == T
- (a T) ; == !a 
- (a F) ; == T
- (a a) ; == !a
- (a b) ; == a->!b
- (b a) ; == b->!a
where a and b are variables where a < b.

- And each of those expressions can be derived from T or F in just one or two steps... 
todo: show derivations, like in above proofs

When 1 < N then E must have the form (L R), where L and R have fewer parentheses pairs than E.  
By the induction hypothesis it follows that L and R are both derivable from T of F in polynomial time.  

A derivation of E may start with one of the following expressions...  
- (T F) ; when L is a tautology and R is a contradiction  
- (T T) ; when L is a tautology and R is a tautology  
- (F T) ; when L is a contradiction and R is a tautology  
- (F F) ; when L is a contradiction and R is a contradiction  
...and then deriving L and R in place.


## Theorem: There is a derivation proof with a linear number of steps for any expression E that is equivalent to a expression C, where C is canonical.  

todo

## Theorem: Constant Expressions are Minimizable to T or F in a Polynomial # of Steps.

Proof is by induction on the # of parentheses in the expression.
Let E be the expression to derive.  

When N (the # of parentheses) == 0 then E is either T or F, and our theorem holds true.  

When 0 < N then E must have the form (L R), where L and R have fewer parentheses than E.  
By induction hypothesis it follows that L and R are both minimizable to T or F in polynomial time, 
and so therefore E can be reduced by first reducing L and R to produce one of the following expressions...
- (F F)
- (F T)
- (T F)
- (T T)

And each of those expressions can be reduced to T or F in just one or two further steps... 
- E =>* (F F) => (F T) => T ; deiteration then erasure
- E =>* (F T) => T          ; erasure
- E =>* (T F) => (F T) => T ; ordering, then erasure
- E =>* (T T) => F          ; empty cut elimination 

And thus, all expressions with no variables are reducible to one of T or F in linear time, 
definitely no more than O(2N).

## Grounded Expressions

### Definition: A *grounded* expression is an expression that has no variables.  

### Lemma: A *grounded* expression is logically equivalent to either T or F.

### Theorem: Grounded expressions can be derived in a linear number of steps.  

A grounded expression is an expression that has no variables.  
Proof is by induction on the # of parentheses in the expression.
Let E be the expression to derive.
Let N be the number of parentheses pairs in E, where 0 <= N.

When N == 0 then E is either T or F, and the theorem holds true.  

When N == 1 then E is one of the following expressions...  
- (T F) ; == T
- (T T) ; == F
- (F T) ; == T
- (F F) ; == T

- And each of those expressions can be derived from T or F in just one or two steps... 
- F => (T T)                ; empty cut introduction 
- T => (T F)                ; insertion
- T => (T F) => (F T)       ; insertion then reorder
- T => (T F) => (F F)       ; insertion then iteration 

When 1 < N then E must have the form (L R), where L and R have fewer parentheses pairs than E.  
By the induction hypothesis it follows that L and R are both derivable from T of F in polynomial time.  

A derivation of E may start with one of the following expressions...  
- (T F) ; when L is a tautology and R is a contradiction  
- (T T) ; when L is a tautology and R is a tautology  
- (F T) ; when L is a contradiction and R is a tautology  
- (F F) ; when L is a contradiction and R is a contradiction  
...and then deriving L and R in place.

And thus, all tautologies and contradictions with no variables are derivable in linear time, 
definitely no more than O(2N).  

## Groundable Expressions

### Definition: A *groundable* expression is an expression that is logically equivalent to a ground expression.  
	> Tautologies are groundable expressions.
	> Contradictions are groundable expressions.

### Lemma: A *groundable* expression is logically equivalent to either T or F.

### Lemma: If E[x] is a groundable expression then E[x<-T] and E[x<-F] are also groundable expressions.  
> E[x<-T] and E[x<-F] are logically equivalent to E[x] and so must be groundable.   

## Contracted Expressions





## Theorem: All groundable expressions are derivable/reducible in a polynomial number of steps.  

Proof is by induction on the # of variables in the expression.
Let E be the groundable expression to derive.  
Let N be the # of variables in E.  
Let x be avariable in E.  

For N == 0, see prior proof that any ground expression is derivable in a polynomial number of steps.  

For 0 < N note that...
- Since E is groundable then E[x<-T] and E[x<-F] are also groundable.   
- by induction hypothesis, the derivations of E[x<-T] and E[x<-F] are polynomial

To derive E, start by deriving this axiom...  
```(x -> T) & (!x -> T)```
...and then derive E's Shannon expansion (by deriving E[x<-T] and E[x<-F] in place)...
```(x -> E[x<-T]) & (!x -> E[x<-F])```
...and then this (via iteration)...
```(x -> E) & (!x -> E)```
...and finally this, by resolution...
```E```

Each of those derivations is polynomial (todo) therefore the derivation E is also polynomial.  


Now, all that's needed is to prove that the number of expressions that need to be used in a proof is polynomial.
Wait, that's stupid, 
Hence....

### Reduction Procedure


# LE Reduction

## Overview

> It is assumed that the reader has already read the [Introduction to Lucid Expressions](lucid-expressions.md).  
> It's especially important to understand the LE concept of *cofactors*.  

A *reduction* is a proof that reduces a given expression to its canonical form.  

In the LE system a proof is the combination of...
- an *expression table*, a list of records that record the structure of every *mostly canonical* expression used to construct a proof.  
	> Assigns an ID to every expression.  
	> Records the left and right sub-terms of every expression.  
	> Two implications can be derived form every expression.  
	> Let expression E have the form (L R), then we can imply that !L -> E, !R -> E, L -> !R, and R -> !L.
- an *implication table*, a collection of Krom clauses that record all the binary logical relationships between all the records in the expression table.  
	> This table is updated with new clauses whenever records are added to the expression or proof tables.   
	> This table is kept transitively complete so that at all times it represents 
	> all binary logical relationships between all the terms in all the expressions in the expressions table 
	> that can be deduced from the structure of the expressions and reduction steps.
- a *proof table*, an ordered list of transforms that include...
	- before and after expression ids, 
	- an inference rule type Id, 
	- a collection of implication ids used to justify and/or implement the rule.

One can think of reduction as the *unwinding* of a derivation of an expression to it's axiomatic, canonical form.  
Because LEs' rules of inference are complete, symmetric, and confluent, we can reduce expression by repeatedly 
applying rules to an expression, but only in ways that produce simpler expressions.  
If an expression reduces to T/F then the expression is a tautology/contradiction.  
If an expression reduces to anything but F then the expression is satisfiable.  

The reduction process proceeds from the bottom up.  
This is because all the logical relationships between subterms must be known in order to compute reductions of higher order expressions.   
The only way to accomplish this is to start the reduction process at the leaves of the starting expression, 
where the terms are already canonical, and build up to more complex canonical expressions.

As reduction proceeds a table of implications is built.  
These implications are used to identify opportunities to further reduce an expression.  

## Example

In this section this derivation is constructed...  
(T ((1 (T 2)) ((T 1) 2))) =>* ((1 2) ((T 1) (T 2))).  

The antecedent basically says "1->2 and !1->!2", and the consequent says "1 == 2".  

Also, it will go without saying that every time an expression E of the form (L R) is added to the expression table that 
these four implications are also added to the implication table... 
- !L -> E ; if the antecedent is false then the expression must be true
- !R -> E ; if the consequent is false then the expression must be true
- L -> !R ; if the antecedent is true then the consequent must be false
- R -> !L ; if the consequent is true then the antecedent must be false

Also, it will go without saying that every time an assertion is added to the assertion table that 
these two implications are also added to the implication table... 
A -> C ; if the antecedent is true then the consequent must be true
!A -> !C ; if the antecedent is false then the consequent must also be false  
They basically say that A and C are equivalent expressions.

We'll build the reduction database (consisting of the expression, implication, and assertion tables).  


Start by adding (T ((1 (T 2)) ((T 1) 2))) to the expression table 
and then add the expression to the assertion table because it's an axiom.  
Obviously, expression records must first be created for all subterms.

Here's what we'll have...

EXPRESSIONS
-----------
ID	EXPRESSION						
1	T		
2	F		
3	1
4	(T 1)		
5	2		
6	(T 2)		
7	(1 2)		
8	(1 (T 2)
9	((T 1) 2)
10	((1 (T 2)) ((T 1) 2))
11	(T ((1 (T 2)) ((T 1) 2)))		

ASSERTIONS
----------
(T ((1 (T 2)) ((T 1) 2)))

IMPLICATIONS
------------

1	(T ((1 (T 2)) ((T 1) 2)))		
1	(T ((1 (T 2)) ((T 1) 2)))		
2	(T ((1 (1 2)) ((T 1) 2)))		
3	(T ((1 (1 2)) ((2 1) 2)))		
4	(T ((1 (1 2)) ((1 2) 2)))		
5	((1 2) ((1 (1 2)) ((1 2) 2)))	
6	((1 2) ((1 T) (2 T)))			
7	((1 2) ((T 1) (T 2)))			
8	((1 (1 2)) ((1 2) 2))			
9	(1 2)
10	((1 (T 2)) ((T 1) 2))		


Start by adding 1 to the expression table.
Since 1 does not have a structure (ie, left and right sides) we add no new implications.
Since

ID	EXPRESSION						JUSTIFICATION
1	(T ((1 (T 2)) ((T 1) 2)))		; axiom
2	(T ((1 (1 2)) ((T 1) 2)))		; iteration
3	(T ((1 (1 2)) ((2 1) 2)))		; iteration
4	(T ((1 (1 2)) ((1 2) 2)))		; ordering, 
5	((1 2) ((1 (1 2)) ((1 2) 2)))	; iteration, highest entropy, the paste in paste-and-cut, not possible in classic EG
6	((1 2) ((1 T) (T 2)))			; deiteration, reduced expression, the cut in paste-and-cut
7	((1 2) ((T 1) (T 2)))			; ordering, lowest entropy

8	((1 (1 2)) ((1 2) 2))			
9	(1 2)
10	((1 (T 2)) ((T 1) 2))		

The transition from line 4 to line 5 is not possible in one step in classic existential graphs.  
That transition is accomplished using LE's more powerful form of iteration.  
The essence of the transform is that an inference is drawn from two subgraphs 
and the conclusion is recorded in the outermost cut of a new graph.  
This is not a new kind of inference rule but a more powerful form of iteration.  
The iteration/deiteration rules in EG and LE are all based on the principles of weakening and contraction.
The presence of (1 2) in each subgraph makes it easy for a human to see that if (1 2) == F 
then ((1 (1 2)) ((1 2) 2)) reduces to F.  
And thus we also know that we can iterate (1 2) into the left side of (T ((1 (1 2)) ((1 2) 2))).  
And we know that if we do so then there will be two instances of (1 2) on the right and one on the left, 
and we know that both the left and right reduce to F when (1 2) is F, 
by therefore we also know that the paste-and-cut transform will reduce (T ((1 (1 2)) ((1 2) 2))).  

(a b) === a->!b, b->!a, !a -> e, !b -> e


Idea: Use KROM logic to record derivations an cofactors.
We can represent the previous proof using KROM implications...
ID	
1	1 == 2	;it
2	2 == 3	;it
3	3 == 4	;ord
4	!8 -> 4 ;co
5	8 == 10 ;it
6	!9 -> !8 ;co
7	!9 -> !10 ;resolution, that's a bingo!
8	4 == 5 ; it, since !9 -> !10

ID	
4	!8 -> 4 ;co
5	8 == 10 ;it
6	!9 -> !8 ;co
7	!9 -> !10 ;resolution, that's a bingo!
8	4 == 5 ; it, since !9 -> !10


Consider the lucid expression...  
```(T (a (a (T b))))```  
which means...  
```A and (if A then B)```.  

(a (a (T b))) is the simplest non-canonical subterm of the starting expression.  

Using deiteration, it can be reduced to...  
```(a (T (T b)))```  
which, using double-cut elimination can can be reduced to...
```(a b)```.  
thus reducing the starting expression to...  
```(T (a b))```  
which is canonical and equivalent to...  
```A and B```  


## Reasoning used in LE reduction

The reduction method performs three fundamental forms of reasoning.  

- Induction : LE reduces an expression by first reducing its subterms.  

	> Thus, LE is always working from simpler canonical expressions to more complex canonical expressions, and 
	> thus LE knows a lot about an expressions' subterms when reducing more complex expressions.  

- Deduction : New implications are deduced from the axiomatic implications associated with expressions and proofs.  

- Abduction : Inference rules are used to deduce new expressions, but only those that are simpler are used.  

### Reduction

This section contains a pseudo-code description of the Reduce function.   
The Reduce function accepts a mostly-canonical expression and returns the next reduction.


```
/// A reduced expression and proof (either a subtitution or a cofactor)
record ReduceResult(Expression Reduction, SubstitutionResult? Substitution, Cofactor? Cofactor)  

Let COFACTORS = a global table of tuples that represent all known grounding cofactors and all derivable cofactors of canonical expressions.  
Let SUBSTITUTIONS = a fixed, global table of rewrite rules, includes rewrite rules for cut elimination.  

ReduceResult? Function Reduce (Expression mostlyCanonical)
{
	// if the expression is already known to be canonical then we're done
	if (Contains(CANONICAL, mostlyCanonical))
	{
		return null
	}

	Let substitutionResult = TryFindGeneralization(SUBSTITUTIONS, mostlyCanonical)
	if (substitutionResult && ) 
	{
		// only use the substitution if the conclusion is simpler
		if (Compare(substitutionResult.Conclusion, mostlyCanonical) < 0)
		{
			return (substitutionResult.Conclusion, substitutionResult, null)
		}
	}

	// no substitutions found, that leaves deiteration or paste-and-cut.

	if (mostlyCanonical.RHS == T)
	{ 
		// paste into rhs and cut from lhs
		Let fGroundingfCofactors = Cofactors(mostlyCanonical.LHS).Where(_ => _.R == F && _.C == F)
		foreach (fGroundingfCofactor in fGroundingfCofactors)
		{
			Let reducedE = (mostlyCanonical.LHS[fGroundingfCofactor.S<-T] fGroundingfCofactor.S)
			if (Compare(reducedE, mostlyCanonical) < 0)
			{
				return (reducedE, null, fGroundingfCofactor)
			}
		}
	}
	else if (mostlyCanonical.LHS == T) 
	{ 
		// paste into lhs and cut from rhs
		Let fGroundingfCofactors = Cofactors(mostlyCanonical.RHS).Where(_ => _.R == F && _.C == F)
		foreach (fGroundingfCofactor in fGroundingfCofactors)
		{
			Let reducedE = (fGroundingfCofactor.S  mostlyCanonical.RHS[fGroundingfCofactor.S<-T])
			if (Compare(reducedE, mostlyCanonical) < 0)
			{
				return (reducedE, null, fGroundingfCofactor)
			}
		}
	}
	else 
	{  
		// deiterate, look for f-groundings f-cofactors of either side 
        Let rhsGroundings = Cofactors(mostlyCanonical.RHS).Where(_ => _.R == F && _.C == F)
		foreach (rhsGrounding in rhsGroundings)
		{
			Let reducedE = (rhsGrounding.S  mostlyCanonical.RHS[rhsGrounding.S<-T])
			if (Compare(reducedE, mostlyCanonical) < 0)
			{
				return (reducedE, null, rhsGrounding)
			}
		}

		Let lhsGroundings = Cofactors(mostlyCanonical.LHS).Where(_ => _.R == F && _.C == F)
        Let commonGroundings = Join(lhsGroundings, rhsGroundings, _ => _.S).FirstOrDefault()
        Let (leftCofactor, rightCofactor) = Join(lhsGroundings, rhsGroundings, _ => _.S).FirstOrDefault()
		if (leftCofactor != null)
		{
			Let reducedE = (leftCofactor.S, (leftCofactor.C rightCofactor.C))
		}
		
	}

	// the given expression is canonical
	return null
}
```

## Complexity

LEs can be reduced to their canonical form in polynomial time.  

The steps that the LE reduction method performs can be categorized into two types...  
- The number of times the starting expression is reduced.
	> In other words, the number of steps in the equivalence proof from the starting expression to its canonical form.   
- The number of steps in each reduction.
	> The number of steps in each reduction is proportional to the number of cofactor records created during each reduction, or 1 if substitution occurred.   

It will be shown that the maximum size of any equivalence/reduction proof is at most Pow(N,2), where N is the length of the expression to reduce.  

And it will be shown that the maximum number of cofactor records computed during a proof is limited to Pow(2N,2)

That makes LE's time complexity on the order of O(Pow(N,2) * Pow(2N,2)) = O(4Pow(N,4)).  



-----------------------------




# Insight : Unification of cofactors can identify a reduction in polytime.

Given an expression, the # of cofactors that can be directly computed from the expression is polynomial.  

We could use deiteration and iteration to expand the set of cofactors until we found a reduction (if one exists).  
However, there are a potentially exponential # of such cofactors.  
Instead, we can use unification to quickly deduce common tgf-cofactors that we can use to reduce the expression.  

Which seems doable, since....
- to minimize we need to find an fgf-cofactor of (T ((1 (T 2)) (2 (T 1)))) 
- computing deductive closure of cofactors does not yield a reduction
- Must find common tgf-cofactor of both sides.  
	- tgf-cofactors of left side are 1, and (T 2)  
	- tgf-cofactors of right side are 2, (T 1)
- unifying (T 2) and (T 1) is easy, but must do unification correctly  
	> ie dont use the standard Robinson unification algorithm which is exponential, see Handbook.  
- we need to retain the unifying substitution so that we can actually perform the rewrite.  
	That is, given that (1 2) is a fgf-cofactor of ((1 (T 2)) (2 (T 1))), how do we rewrite it?  
	By applying the unifying substitution and reordering to get ((1 (1 2)) (2 (1 2))).  
	Then we apply the substitution (1 2)<-T to get ((1 T) (2 T)), and therefore  
	(T ((1 (T 2)) (2 (T 1)))) => ((1 2) ((1 T) (2 T))) => ((1 2) ((T 1) (T 2))) 

Here are some of the cofactors calculated for the above expression, note the last two...

	Cofactors
	(S	==	R) -> (	E ==	C)
	2		F		2		F
	2		T		2		T
	2		F		(T 2)	T
	2		T		(T 2)	F
	(T 1)	F		(2 (T 1))	T	; rhs of (2 (T 1))
	(T 2)	F		(1 (T 2))	T	; rhs of (1 (T 2))

if we can unify (T 1) and (T 2) then we can create a common fgt-cofactor of both sides 
and thus a fgf-cofactor that we can use to reduce the expression.  

Still one issue... how to actually make the rewrite that reduces the expression?
That is, we know that (1 2)



	(1 (T 2)) == (1 (1 2))			; iteration
	(2 (T 1)) == (2 (1 2))			; iteration
	(1 2)	F		(2 (1 2))	T	; rhs of (2 (1 2))
	(1 2)	F		(2 (T 1))	T	; from previous two lines

	(1 2)	F		(2 (T 1))	T
	(1 2)	F		((1 (T 2)) (2 (T 1))))  F
	(1 2)	F		(1 (T 2))	T	; 

	|T||1|T.2|2|T.1 => ||1.2||T.1|T.2 ; eq to (1->2 && 2->1) => (2 == 1)
	|T||1|T.2|2|T.1 => ||1.2||T.1|T.2 ; eq to (1->2 && 2->1) => (2 == 1)

This is an example of an expression that can't be reduced by directly computing cofactors of the terms in the expression.  

	|T||1|T.2|2|T.1 => ||1.2||T.1|T.2


# Insight : Add an ordering rule that expressions with fewer unique terms are less than expressions with more terms.  
This new rule is applied before the length ordering rule, giving it a higher priority than the length rule.  
 
The effect would be this...  
Instead of this rule (which is quite difficult to implement)...  

	|T||1|T.2|2|T.1 => ||1.2||T.1|T.2 ; eq to (1->2 && 2->1) => (2 == 1)

the system would generate these rules, which are easier to implement...  

	|T||1|T.2|2|T.1 => 
	|T||1|1.2|2|1.2 => ; simple unification to find form that's reducible, using cofactor calculations?
	||1.2||T.1|T.2 ; paste-and-cut (iteration followed by deiteration), 

	|T||1|1.2|2|T.2 => 
	|T||1|1.2|2|1.2 => ; simple unification that finds common terms

PS:  Minimizing the unique terms makes more sense as a measure of 'simple' than the length of the expression.  
That's because when you think about the 'size' of the expression, the LE implementation can usually store 
expressions with fewer terms more efficiently than expressions that are 'shorter'.  
This is because, in a computer's memory, it's possible to reuse an expression as a pointer, 
which doesn't require the expression to be duplicated.  




## Definition: An expression that can be either true or false, depending on the values assigned to its variables, is called a *contingent expression*.  
## Definition: Given an expression E, a variable in E that can be replaced with T or F without effecting the truth function of an expression is called an *independent variable*.  
## Theorem: A canonical expression does not have any independent variables.
	> Proof by contradiction.
	> if E is a canonical expression with an independent variable then E[x<-T] or E == E[x<-F] is a simpler, equivalent expression, 
	> and therefore E is not canonical.  



In SE, a proof is usually a *reduction* from a non-canonical expression to a canonical expression.  

But a proof can also be a *derivation* from a canonical expression to a non-canonical expression.  

For every deri



Here's how SE solves this problem...
Instead of using the terms in a flatterm SE uses these terms...  

The flatterm for (T (l r)) is...
subterm 		index	
l				0       
(T l)			1
r				2		
(T r)			3
(l r)			4
(T (l r))		5		

All the structural relationships in (l r) can now be expressed with these clauses...  
- !0 -> 4	; when !l then (l r), same as 1st rule in previous example
- !2 -> 4   ; when !r then (l r), same as 2nd rule in previous example
- 5 -> !4   ; when (T (l r)) then !(l r), same rule as the last rule in previous example but now with fewer variables
- 4 -> 5
- 2 -> !3
- !3 -> 2
- 0 -> !1
- 1 -> !0
- 

- 
- 4 -> !5
- 0 -> !1
- 1 -> !0
- 3 -> !2
- 2 -> !3
- 1 -> !4
- 3 -> !4


Here's how SE solves this problem, it rewrite the previous three clauses as four clauses with some modalities....

- !1 -> 0; always part of cofactor model
- !2 -> 0; always part of cofactor model
- 1 -> !0; added to cofactor model if/when 2 is added 
- 2 -> !0; added to cofactor model if/when 1 is added 

The conditions on when expressions are true (ie, part of the model) make this a kind of modal logic.  
But it's a kind of modal logic that retains the polynomial proof complexity of Krom logic, 
because there's a quadradic limit on the number of 2-variable clauses that can possibly be generated 
by this logic.




