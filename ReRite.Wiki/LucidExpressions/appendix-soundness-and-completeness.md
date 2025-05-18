# LE Soundness and Completeness 

This document presents proofs that the LE system is sound and complete.  

Soundness means that LE's rewrite rules produce semantically equivalent formulas.  
That is, if P =>* Q or Q =>* P then P == Q is true.    
For each rule in LE it will be shown that the right-hand side is semantically equivalent to the left-hand side, and vice versa, and the rule is therefore sound.  

Completeness means that if two expressions are semantically equivalent then LE's rewrite rules can derive the expressions from each other.  
That is, if P == Q then P =>* Q, and Q =>* P.  
Put another way, if P -> Q then P =>* Q, for any two expressions P and Q  
Put another way...

	if T =>* (P (T Q) then P =>* Q, for any two expressions P and Q

...this is called the Deduction Theorem.  

If the Deduction Theorem is valid then LE is complete because...  

- when P == Q then T =>* (P (T Q) and T =>* (Q (T P)), 
- and therefore, by the Deduction Theorem, P =>* Q, and Q =>* P.  

## Soundness

### Order Reduction 

The order reduction rules...

0. (P Q) <=> (T Q), if Compare(P,Q) == 0
1. (P Q) <=> (Q P), if Compare(P,Q) > 0
2. (Q P) <=> (P Q), if Compare(P,Q) < 0

The Order Reduction rules only apply to expressions of the form (P Q),  
that is, to expressions that are based on cuts.  

Cuts are logically equivalent to NAND functions, which are commutative.  
That is, the LE expression (P Q) is semantically equivalent to the boolean expressions !(P && Q) and !(Q && P).
Rules 1. and .2 produce expressions that are semantically equivalent to their input, and are therefore sound.  

Rule 0 is sound because the only way that Compare(P,Q) == 0 is if P and Q are the exact same expression.  
In this case (P Q) is equivalent to (Q Q), which by deiteration, is equivalent to (T Q).  


### Constant Introduction/Elimination
These rules introduce or eliminate constants from expressions.  
These rules are bidirectional.

1. (T T) <=> F
2. (Q F) <=> T
3. (F Q) <=> T
4. (T (T Q)) <=> Q

These rules can be shown to be sound by translating them into their propositional/semantic equivalent and showing that those formulas are tautologies.  
And they are, here are the formulas...

1. !(true && true) == false     ; always evaluates to true	
2. !(Q && false) == true        ; always evaluates to true
3. !(false && Q) == true        ; always evaluates to true
4. !(true && !(true && Q)) == Q ; always evaluates to true

### Generalized Iteration

Rule: Given a subterm S of an expression E of the form (L R),  
	where S is a left or right, F-grounding F-cofactor of E then 
	all copies of T in the other side of the expression may be replaced with S.  
	 						
Expressed as rewrite rules...  
	- (L[T] R[S]) => (L[T=>S] R[S]), or  
	- (L[S] R[T]) => (L[S] R[T=>S])

Soundness follows rather simply because when S has the truth value ```false``` then R[S] and L[S] also has the truth value ```false```.  
When R[S](or L[S]) has the truth value ```false``` it doesn't matter what the other side of E, L[T] or R[T] evaluates to, E will always evaluate to ```true```.  
When R[S](or L[S]) has the truth value ```true``` then L[T=>S] (and R[T=>S]) will evaluate to the same value as L[T] (or R[T]).  
Therefore, the right-hand sides of the iteration rules always evaluate to the same truth value as the left-hand sides.  
In other words, the two sides of the iteration rewrite rules are semantically equivalent and the rules are sound.  


### Generalized Deiteration

Rule: Given a subterm S of an expression E of the form (L R),  
	where S is a left or right, F-grounding F-cofactor of E then 
	all copies of S in the other side of the expression may be replaced with T.  
	 						
Expressed as rewrite rules...  
1. (L[S] R[S]) => (L[S=>T] R[S]), or  
2. (L[S] R[S]) => (L[S] R[S=>T])

Soundness follows rather simply because when S has the truth value false then, by definition, one of R[S] or L[S] also has the truth value false.  
When R[S](or L[S]) has the truth value false it doesn't matter what the other side of E, L[T] or R[T] evaluates to, E will evaluate to true.  
When R[S](or L[S]) has the truth value true then L[S=>T] (and R[S=>T]) will evaluate to the same value as L[S] (or R[S]).  
Therefore, the right-hand sides of the iteration rules always evaluate to the same truth value as the left-hand sides.  
In other words, the two sides of the deiteration rewrite rules are semantically equivalent and the rules are sound.  


### Insertion

Insertion is like a special case of generalized iteration where one side of an expression E is F.  

Expressed as rewrite rules...  
	- (L[T] F) => (L[T=>S] F), or  
	- (F R[T]) => (F R[T=>S])

Soundness is trivial since, because of the presence of the F expression inside the cut, 
both sides of both rewrite rules will always evaluate to true.  
One can literally replace the other side of E with any expression, much less a derived expression.  
Therefore, the left and right sides of the rewrite rules are semantically equivalent  
since both sides will always evaluate to true.  
And therefore the insertion rules are sound.  
	
### Erasure

Erasure is like a special case of generalized deiteration where one side of an expression E is F.  

Expressed as rewrite rules...  
	- (L[S] F) => (L[S=>T] F), or  
	- (F R[S]) => (F R[S=>T])

Soundness is trivial since, because of the presence of the F expression inside the cut, 
both sides of both rewrite rules will always evaluate to true.  
One can literally replace the other side of E with any expression, much less a derived expression.  
Therefore, the left and right sides of the rewrite rules are semantically equivalent  
since both sides will always evaluate to true.  
And therefore the erasure rules are sound.  



## Deduction Theorem 

This section presents two proofs that together prove the Deduction Theorem.  

If ((...)) =>* ((..., P->Q))  
Then ((..., P->Q, P)) =>* ((..., P->Q, P, ..., Q))
Proof...  

	...						; some set of consistent axioms
	(P (T Q))				; axiom, P->Q
	P						; hypothesis
	(T (T Q))				; substitution: (P (T Q))[P<-T]
	Q						; dbl-cut elim 

If ((...)) =>* ((..., P->Q)) 
Then ((..., P->Q, (T Q))) =>* ((..., P->Q, (T Q), ..., (T P)))  
Proof...  

	...						; some set of consistent axioms
	(P (T Q))				; axiom, P->Q
	(T Q)					; hypothesis
	(P T)					; substitution: (P (T Q))[(T Q)<-T>]
	(T P)					; ordering


We humans don't have to use our eyeballs and our brains to build proofs, machines can do it.  
The LE 
 
## Mechanical Proof of Deduction Theorem using Cofactors

Calculating cofactors as we go guides substitution.

In this se

If ((...)) =>* ((..., P->Q))  
Then ((..., P->Q, P)) =>* ((..., P->Q, P, ..., Q))
Proof...  

	...						; some set of consistent axioms
	(P (T Q))				; axiom, P->Q
	P						; hypothesis
	(T (T Q))				; substitution: (P (T Q))[P<-T]
	Q						; dbl-neg elim


Find fgf-cofactor of ((1 (T 2)) ((T 1) 2)))	
Must find common tgf-cofactor of both sides
tgf-cofactors of left side are 1, and (T 2)
tgf-cofactors of right side are 2, (T 1)
No common cofactor :-(.

What we need are valid substitutions for each side that have a common tgf-cofactor.  
Both sides are canonical, so we can iterate all possible substitutions for those expressions, 
from the simplest to the most complex, 
using LE's rules of inference.  

So let's try that...

Let allLHS = Concat([(1 (T 2))], Increment((1 (T 2))))
	.Where(_ => Cofactors(_).Where(_ => _.R == T && _.C == F).Any())

Let allrHS = Concat([((T 1) 2)], Increment(((T 1) 2))))
	.Where(_ => Cofactors(_).Where(_ => _.R == T && _.C == F).Any())

// KA-BLAM, there's a common tgf-cofactor where _.S is (1 2)
Let commonFactor  = Join(allLHS, allRHS, _ => _.S).FirstOrDefault()



## References

### Native Proofs
[Native diagrammatic soundness and completeness proofs for Peirces Existential Graphs (Alpha); Caterina; Gangle; Tohme](https://philsci-archive.pitt.edu/21196/1/NativeAlphaFinal.pdf)


### Existential Graphs of Peirce
[Mathematical Logic with Diagrams, Based on the Existential Graphs of Peirce;Dau](http://www.dr-dau.net/Papers/habil.pdf).


### Term Indexing
[Term Indexing; Sekar; Ramakrishnan; Voronkov; in Handbook of Automated Reasoning; Robinson and Voronkov editors]()
