# Determining the satisfiability of NAND expressions in polynomial time.

## Abstract

It's known that nand operators are functionally complete 
and that any boolean expression can be expressed with just nand functions.  
It's also known that determining the satisfiability of a nand-only expression is an NP-complete problem.  

In this document it is shown that 
the structural logic of nand-only expressions can be modeled with disjunctive clauses 
such that the model is satisfiable if and only if the expression is satisfiable.
Thus, the satisfiability of nand-only expressions can be determined in polynomial time.

## Introduction to Nand Expressions

### Notation

The expressions used in this document use...
- the values T and F to represent the constant values true and false.
- numbers to represent variables.
	> Variables have onw of two values, true or false.
- a pair of brackets that bound two other expressions, called a *context*.  
	> A context has the same semantics as a nand gate

Examples; T, 1, [T 1], [1 2], and [[1 2] [2 [T 3]]].

> NOTES...
> The use of brackets instead of a nand operator is unique.
> The brackets are easier for humans to read than operators.  
> The bracket notation is inspired by existential graphs.  

### Natural Deduction

This is a little off topic but, if you're going to play around with structured expressions 
then there is a trick that makes structures expressions easier to use.  
The trick is to annotate contexts with the boolean operators used in natural deduction, 
doing so makes it a lot easier to think with SEs.  

For example, this boolean expression....
```a -> b```  
...is interpreted as 'if a then b' which I would write in SE as...  
```[a -> [T b]]```   
The trick is that if you strip the boolean operators out of the above expression 
then what's left will be a valid SE expression that is equivalent to the starting boolean expression...
```[a [T b]]```   

It easy to write these expressions with just a little practice once you know the rules...

    - NAND: a | b    => [a b]
    - NOT:  ~a       => ~[T a]
    - IMPL: a -> b   => [a -> [T b]]
    - AND:  a && b   => [T [a && b]]
    - OR:   a || b   => [[T a] || [T b]]

## Flatterm

A flatterm is an array of an expressions' subterms, enumerated in a depth-first fashion, starting with the expression itself.  
This structure is an extremely useful way to represent expressions.  

### Example
[a [b [b c]]]

Flatterm of [a [b [b c]]]...  

subterm 		index	depth
------------	-----	-------
[a [b [a c]]] 	0		0
a       		1       1
[b [a c]]		2		1
b               3		2
[b c]			4		2
b				5		3
c				6		3

### Length

Length = 7 = Sizeof( Flatterm( [a [b [b c]]] ));

The *length* of an LE expression is defined as the length of the expressions' flatterm.  

Notes...
- Length = (2 * (# of bracket pairs)) + 1.
- Expressions always have an odd length.  


### Depth 

Depth is a property of a subterm in an expressions' flatterm.  

Simply, a subgraphs' depth is the number of left brackets that you need to cross to get to that subgraph.  

0 <= Depth.

## Cofactors  

A *cofactor* is an implication between terms in an expression.  
Cofactors are statements of the form...  
'When some subterm, or subterms, of an expression E are true/false then E is true/false'.  

A cofactor can be represented by a 2-variable disjunctive clause.  
For instance, given an expression C of the form [l r], the clause (!l || C) can mean 'if the term l is false then the term C is true'.  

In a general sense, a cofactor records how facts in an expression are related to each other.  
Cofactors can be used to model the relationships between the parts of an expression.  

In this document it will shown how to efficiently determine the satisfiability of nand expressions 
using cofactors to model the expression.

## Cofactor Models

A cofactor model is a set of disjunctive clauses that model the cofactor relationships between the subterms of an expression  
such that the cofactor model is satisfiable if and only if the expression is satisfiable.  

After building a cofactor model, propositional [resolution](https://en.wikipedia.org/wiki/Resolution_(logic)) is used to determine if the model is satisfiable.  
It will be shown that the satisfiability of a cofactor model can be determined by only generating clauses with two or fewer clauses.  
In this way it will be shown that there is a polynomial limit on the number of clauses that need to be resolved in order to prove satisfiability.  

To build a cofactor model for an expression E, unique identifiers must be assigned to all the subterms in E.  
Nothing is relevant about identifiers except that they're unique.  

> Note...
> In this document a subterm's position in an expression's flatterm is often used as an id because it's convenient to do so.  
> However, a complete cofactor model will include clauses that represent every subterm, and its negation, in an expression.    

### Modeling an Expression

To build a cofactor model for an expression E...

1. Unique Ids: Assign unique ids to every subterm of E and, while building the cofactor model, to every expression associated with a join point.
2. Model Variables: Add equality clauses for every pair of subterms that represent the same variable.
3. Model Contexts: Add clauses that represent the relationship between the terms in every context.
4. Model Join Points: Add clauses that represent the unification of join points.  

Nothing is relevant about identifiers except that they be unique.  

When discussing cofactor models, it's convenient to use a subterm's position in an expression's flatterm as an id.  

Consider the expression [l r] and its flatterm...  
subterm 		index	
------------	-----	
[l r]		 	0		
l				1       
r				2		

In the cofactor model of [l r], the subterm l has an id 1, and [l r] has an id of 0.

The fact that l is a T-grounding F-cofactor of E is represented by the formula !1 -> 0.  

### Modeling Variables

Variables are modeled by adding equalities that constrain subterms that represent variables to be equivalent.  

Consider the expression [a a] and its flatterm...  
subterm 		index	
------------	-----	
[a a]		 	0		
a				1       
a				2		

In the cofactor model of [a a], the clauses 1 -> 2, and !1 -> !2 are added to the model 
so that the value of term 1 will always be equal to the value of term 2.

### Modeling A Context

Let E be a context of the form [l r]

Consider the expression [l r] and its flatterm...  
subterm 		index	
------------	-----	
[l r]		 	0		
l				1       
r				2		

The cofactor relationships in [l r] can be expressed with these clauses...  

- !1 -> 0; when the term at index 3, l, has the value false then the term at index 0 must have the value true.
	> Equivalent to: !l -> [l r].
	> Expressed as a disjunction: (0 || 1).
- !2 -> 0; when the term at index 4, r, has the value false then the term at index 0 must have the value true.
	> Equivalent to: !r -> [l r]
	> Expressed as a disjunction: (0 || 2).  
- (1 && 2) -> !0; when the terms at indexes 1 and 2 have the value true then the term at index 0 must have the value false.
	> Equivalent to: (l && r) -> [T [l r]]
	> Expressed as a disjunction: (!1 || !2 || !0).

A set of clauses that model a context is called a *context model*.  
The expression ```((0 || 1) && (0 || 2) && (!1 || !2 || !0))``` is a context model of the expression E = [l r].  
Another way to interpret the above expression is "if E then !l or !r, but if not E then l and r".  

Note that...  
- if we want to test E for satisfiability then we make truth the goal by asserting 0 in the model and resolving the remaining clauses.
- if we want to test E for un-satisfiability then we make contradiction the goal by asserting !0 in the model and resolving the remaining clauses.

Note that, once a goal is chosen for a context then the context model reduces to an instance of 2-SAT, clauses with just 2 or fewer variables.  
That is, when 0 is asserted (that is, when we want to prove that E can be true) then the cofactor model collapses to ```0 && (!1 || !2))```.  
Similarly, when !0 is asserted (when we want to prove that E can be false) then the cofactor model collapses to ```!0 && (1 && 2)```.  


### Modeling Join Points

Let E be a context of the form [[l r]

Consider the expression [l r] and its flatterm...  
subterm 		index	
------------	-----	
[l r]		 	0		
l				1       
r				2		





### Determining the Satisfiability of a Cofactor Model

This section outlines how a cofactor model is used to determine the satisfiability of an expression.

Cofactor models are examples of [3-SAT](https://en.wikipedia.org/wiki/Boolean_satisfiability_problem#3-satisfiability) problems, 
where clauses can have up to 3 literals.  

It will be shown that the problem of determining the satisfiability of cofactor model 
can be broken down into N [2-SAT](https://en.wikipedia.org/wiki/2-satisfiability) problems, 
where N is the length of the modeled expression. 

And since 2-SAT is polynomial, and since cofactor models can be broken down into a linear number of 2-SAT problems, 
the satisfiability of cofactor models can be determined in polynomial time.

Let SAT(E) be a function that returns true iif there is an assignment of variables in an expression E that causes E to resolve to T.
Let UNSAT(E) be a function that returns true iif there is an assignment of variables in an expression E that causes E to resolve to F.

Theorem: SAT([l r]) = UNSAT(l) || UNSAT(r)
	> Proof: If l can be false then [l r] can be true.  
	> Otherwise, if r can be false then [l r] can be true.  
	> If neither l nor r can ever be false then both l and r must always both be true, 
	> and thus E is always false and thus not satisfiable.

The significance of this theorem is that the problem of determining the satisfiability of a nand expression 
can be broken down into two simpler, completely independent, subproblems.

Theorem: UNSAT([l r]) = SAT(l && r) = SAT([T [l r]])
	> Proof: If l can be false then [l r] can be true.  
	> Otherwise, if r can be false then [l r] can be true.  
	> If neither l nor r can ever be false then both l and r must always both be true, 
	> and thus E is always false and thus not satisfiable.

Theorem: UNSAT(E) = SAT([T E])                                   
	> Proof: The only way that [T E] can be satisfiable is if there is an assignment that makes E false.  

Corollary: SAT([l r]) = SAT([T l]) || SAT([T r])

This corollary is very significant, it says that for nand expressions, 
the SAT problem can be reduced to checking the negation of its subterms.  

Let's consider what this corollary means from the perspective of a cofactor model.  
Let M = ```((0 || 1) && (0 || 2) && (!1 || !2 || !0))```.  
Let L = ```(2 && (0 || 1) && (!1 || !0))```.  
Let R = ```(1 && (0 || 2) && (!2 || !0))```.  
this means that to determine the satisfiability of ```((0 || 1) && (0 || 2) && (!1 || !2 || !0))```, 
it's only necessary to check the satisfiability of ```(1 && (0 || 2) && (!2 || !0))```, 
and possibly necessary to also check ```(2 && (0 || 1) && (!1 || !0))```.  

Notice that the problem of determining satisfiability went from a single problem with clauses with up to 3 literals, 
to two problems with clauses only up to two literals.

The advantage of dividing the SAT problem like this is that it allows one to avoid ever using the 3-variable clauses in any SAT proof, 
because all SAT problems with a 3-variable clause can be translated into two simple 2-SAT problems that dont contain that clause.  

Theorem: All SAT problems with more than one context divides into exactly N-1 SAT subproblems.  
> Proof: A NAND expression is a context with exactly N-1 sub-contexts, where N is the number of contexts in the expression.  

Theorem: NANDSAT has a complexity of O(N^3).  
> Proof: All NANDSAT problems can be divided into at most N subproblems.   
> Also, because each subproblem is composed of Krom clauses that contain no more than (N/2) + 1 variables,  
> it takes no more than O(N^2) steps to complete any subproblem.  
> Thus, the complexity of NANDSAT is O(N) * O(N^2) = O(N^3)

### Examples...

[a [a b]]
01 23 4

context models...
((0 || 1) && (0 || 2) && (!1 || !2 || !0))
((2 || 3) && (2 || 4) && (!3 || !4 || !2))
structural...

equalities...
1==3

remove equalities...
((0 || 1) && (0 || 2) && && (!1 || !2 || !0))
((2 || 1) && (2 || 4) &&  (!1 || !4 || !2))
0	; axiom, cause model to reduce to 2-SAT
	((1 || 2) && (!1 || !2))
	((2 || 4))
(!1 || 4)	 ; at this point no more clauses can be produced.
			 ; No contradiction deduced, therefore satisfiable

1; goal, a = true
4; b = true; therefore a = b = true is a solution

!1; goal, a = false, when a is false it doesnt matter what b is 

NOTE THAT AFTER SETTING THE GOAL THE ENTIRE MODEL REDUCED TO 2-SAT


### Examples...

[[a b] [c d]]
012 3  45 6

context models...
((0 || 1) && (0 || 4) && (!0 || !1 || !4))
((1 || 2) && (1 || 3) && (!1 || !2 || !3))
((4 || 5) && (4 || 6) && (!4 || !5 || !6))
equalities...none
0	; axiom, cause model to reduce to 2-SAT
	((!1 || !4))
	((1 || 2) && (1 || 3) && (!1 || !2 || !3))
	((4 || 5) && (4 || 6) && (!4 || !5 || !6))
(!1 || 4)	 ; at this point no more clauses can be produced.
			 ; No contradiction deduced, therefore satisfiable

1; goal, a = true
4; b = true; therefore a = b = true is a solution

!1; goal, a = false, when a is false it doesnt matter what b is 

NOTE THAT AFTER SETTING THE GOAL THE ENTIRE MODEL REDUCED TO 2-SAT

[[a b] [c d]] is true if [a b] is unsatisfiable or [cd] is unsat













In the next section we will show that the goal chosen for a context depends solely on the depth of the context.  
That is, contexts at even depths, that are assigned a goal, will be assigned a truth goal.  
And contexts at odd depths will eventually be assigned a contradiction goal.  
Another way to think about it is, 

(a (b c))
(a (T a))
01 23 4
```
((0 || 1) && (0 || 2) && (!1 || !2 || !0))
((2 || 3) && (2 || 4) && (!3 || !4 || !2))
The expression ```((0 || 1) && (0 || 2) && (!1 || !2 || !0))``` is a context model of a context at position 0.  

((0 || 1) && (0 || 2))
((2 || 1) && (!1 || !2))
(0 -1)
0

expression & flatterm...
((1 (T 2)) ((T 1) 2))
012 34 5   678 91 1
			    0 1
cofactor model...
((0 || 1) && (0 || 6) && (!1 || !6 || !0))
((1 || 2) && (1 || 3) && (!2 || !3 || !1))
((3 || 4) && (3 || 5) && (!3 || !4 || !5))
((6 || 7) && (6 || 11) && (!7 || !11 || !6))
((7 || 8) && (7 || 9) && (!8 || !9 || !7))

with equalities removed...
2==9, 5==11
((0 || 1) && (0 || 6) && (!1 || !6 || !0))
((1 || 2) && (1 || 3) && (!2 || !3 || !1))
((3 || 4) && (3 || 5) && (!3 || !4 || !5))
((6 || 7) && (6 || 5) && (!7 || !5 || !6))
((7 || 8) && (7 || 2) && (!8 || !2 || !7))

((1 || 2) && (1 || 3) && (!2 || !3 || !1))
((3 || 4) && (3 || 5) && (!3 || !4 || !5))
((6 || 7) && (6 || 5) && (!7 || !5 || !6))
((7 || 8) && (7 || 2) && (!8 || !2 || !7))
0 ; axiom, set 0 == T as the goal
(!1 || !6)	; 0, (!1 || !6 || !0)
(2 || !6)	; (!1 || !6), (1 || 2)
(3 || !6)	; (!1 || !6), (1 || 3)
(!1 || 7)	; (!1 || !6), (6 || 7)
(!1 || 5)   ; (!1 || !6), (6 || 5)
(2 || 7)	; (2 || !6), (6 || 7)
(2 || 5)	; (2 || !6), (6 || 5)
(3 || 7)	; (3 || !6), (6 || 7)
(3 || 5)	; (3 || !6), (6 || 5)
4
(!3 || !5), ; 4, (!3 || !4 || !5)
(!5 || !6)	; (!3 || !5), (3 || !6)
(!5 || 1)	; (!3 || !5), (1 || 3)
(!5 || 7)	; (!3 || !5), (3 || 7)
(!3 || 6)	; (!3 || !5), (6 || 5)
8 
(!2 || !7)




Thus, after constructing a complete cofactor model for an expression, the set of horn clauses can be reduced to a set of 
Kron clauses.  
Finally, equalities are added to the Krom model to make terms that represent variables equivalent.

#### Testing for Satisfiability/Un-Satisfiability

Note that to test the 





#### Theorem: Modeling A Context

Theorem: If M is a cofactor model of a context E, then M is satisfiable if and only if E is satisfiable.  

Let E be a context of the form [l r].  
Let M be a model of E, where M is represented by the expression ```((0 || 1) && (0 || 2) && (!1 || !2 || !0))```.

Note that this theorem *is not* stating that a model of a context is equivalent to a context,  
it's just stating that, as far as a satisfiability check, they will both will yield the same answer.  

To prove the theorem we will show that...
- if ```[l r]``` is satisfiable then M is also satisfiable, and  
- if ```[l r]``` is not satisfiable then M is also not satisfiable 
... by constructing a truth table and exhaustively enumerating the possibilities.  

First, note 1 == l is a true statement since, by definition, l is the expression at position 1 in the expression [l r].
Same reasoning applies to 2 == r.  
Therefore, we can rewrite the context model as ```(0 || l) && (0 || r) && (!l || !r || !0)```.  

And doing so makes it easier to check truth functions...  
```
			!l -> 0		!r -> 0		(l && r) -> !0 
0	l	r	(0 || l)	(0 || r)	(!l || !r || !0)	&&		[l r]	Theorem
--	--	--	--------	--------	------------		--		-----	--
T	T	T	T			T			F					F		F		choose 0 == F when [l r] == F
T	T	F	T			T			T					T		T		
T	F	T	T			T			T					T		T		
T	F	F	T			T			T					T		T		
F	T	T	T			T			T					T		F		
F	T	F	T			F			T					F		T		choose 0 == T when [l r] == T
F	F	T   F			T			T					F		T		choose 0 == T when [l r] == T
F	F	F	F			F			T					F		T		choose 0 == T when [l r] == T
```
Note that, whenever ```[l r]``` is true then there is no value of 0 that makes ```((!0 || l) && (!0 || r) && (!l || !r || 0))``` true.  
Thus, if E is not satisfiable then the context model of E is not satisfiable.  
Also note that, whenever ```[l r]``` is true there is a value of 0 that makes ```((!0 || l) && (!0 || r) && (!l || !r || 0))``` true.  
Thus, if E is satisfiable then the context model of E is satisfiable.  
Thus, the theorem is proved.  

#### Theorem: Modeling Expression Structure

Let U be an expression where every variable is unique.  
Let the cofactor model of U be the union of the cofactor models of every context in U.   
Theorem: The cofactor model of U is satisfiable iif U is satisfiable.

This will be shown to be true by induction of the # of contexts.  
It has already been shown that the theorem is true for N ==1.  

For 1 < N... 
(1 (2 3))
01 23 4
Cofactor model
(!0 || 1) && (!0 || 2) && (!1 || !2 || !0)
(!2 || 3) && (!0 || 4) && (!3 || !4 || !2)



### Modeling an Expression

An expression is modeled by modeling all of the contexts in the expression 
and adding equalities that constrain subterms that represent variables to be equivalent.  

Context models encode the structural logic in an expression while the equalities encode the equivalencies.  
In a sense, variables are facts and contexts record the relationships be

Equalities are not horn clauses but the equalities are easily removed, leaving just horn clauses.  
Therefore the satisfiability of an expression can also be reduced to determining the satisfiability of a set of horn clauses.  

If every context model for every context in an expression can be satisfied in a way 
that maintains the equivalence between subterms that represent equivalent variables 
then the original expression can be satisfied.

#### Satisfiability of Expression Models

todo: Prove that a cofactor model is satisfiable is and only if the expression it models is satisfiable.

Theorem: Prove that the cofactor model of an expression satisfiable if and only if the expression it models is satisfiable.

Prove by induction on the number of contexts.

In the previous section the theorem was proved for the case where N == 1.  
That is, if M is a context model of a context E, then M is satisfiable if and only if E is satisfiable.  





### Example
Find a satisfying valuation of [T [[1 [1 2]] [[1 2] 2]]] .  

```
[T [[1 [1 2]] [[1 2] 2]]]  
01 234 5  7   89          		  
```
NOTES...
- equality cant be represented in horn clauses, equivalence is handled by reusing the index of variable terms
- inequality works as-is

#### Cofactor Model		

2 -> !0 
!2 -> 0 

!3 -> 2			
!8 -> 2
3 & 8 -> !2

!4 -> 3
!5 -> 3
4 & 5 -> !3

!7 -> 5
!4 -> 5
4 & 7 -> !5

!5 -> 8
!7 -> 8
5 & 7 -> !8

#### Prove Satisfiability

0	; goal
!2	;0, 2 -> !0
3	;!2, !3 -> 2
8	;!2, !8 -> 2
!4	; goal var[1]=F
5	;
!7  ; var[2]=F


Because !4 and !7, 1=F and 2=F is a satisfying solution.  
We could have chosen !5 as a secondary goal instead of !4, 
in which case we would have derived 4 and 7 instead of !4 and !7, 
both of which are satisfying solutions.  


## Summary

It has been shown that...  

- Nand
