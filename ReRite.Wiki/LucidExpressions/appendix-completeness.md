## LE Soundness and Completeness 

This is a proof that the LE system is sound and complete.  

Soundness is proved by proving the soundness of each rewrite rule.  
Specifically, for each rule, it is shown that the right-hand side of a rule is logically equivalent to the left-hand side.  
That is, each syntactic rule produces a new expression that is semantically equivalent to the beginning expression.  

### Soundness
#### Semantic vs Syntactic Entailment

- the symbol |- means syntactic entailment.  
> That is, if A |- B then B is syntactically derivable from A using the inference rules of the LE system.

- the symbol |= means semantic entailment.
> That is, if A |= B then any valuation that forces A to T also forces B to T.  

#### Cofactors  
A cofactor is a tuple (Q,S,V,R) such that Q[S=>V] |- R, where...  
- Q is an expression of the form (L R),
- S is a subterm in Q
- V (aka test value) is an expression, usually a constant
- R is the fully reduced, canonical, ground version of Q[S=>t].  

A subterm S of an expression E may be said to be a cofactor of E if there exists a cofactor (Q,S,V,R) for some value of Q,V, and R.

An F-cofactor is a cofactor where V== F.
A T-cofactor is a cofactor where V == T.
A *grounding* cofactor is a cofactor where R is a constant.
An *F-grounding* cofactor is a grounding cofactor where R == F.
A *T-grounding* cofactor is a grounding cofactor where R == T.

A left cofactor occurs in the left-hand side of an expression.  
A right cofactor occurs in the right-hand side of an expression.  

#### Order Reduction 

The order reduction rules...

0. (P Q) <=> (T Q), if P == Q
1. (P Q) <=> (Q P), if Q < P
2. (Q P) <=> (P Q), if P < Q

The Order Reduction rule only applies to expressions of (P Q),  
that is, to expressions that are based on cuts.  

Cuts are logically equivalent to NAND functions, which are commutative.  
That is, the LE expression (P Q) is semantically equivalent to the boolean expressions !(P && Q) and !(Q && P).
Rules 1. and .2 produce expressions that are semantically equivalent to their input, and are therefore sound.  

Rule 0 is sound because the only way that P == Q (that is, Compare(P,Q) == 0) is if P and Q are the exact same expression.  
In this case (P Q) is equivalent to (Q Q), which, by deiteration, is equivalent to (T Q).


#### Constant Introduction/Elimination
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

#### Generalized Iteration

Rule: Given a subterm S of an expression E of the form (L R),  
	where S is a left or right, F-grounding F-cofactor of E then 
	all copies of T in the other side of the expression may be replaced with S.  
	 						
Expressed as rewrite rules...  
	- (L[T] R[S]) => (L[T=>S] R[S]), or  
	- (L[S] R[T]) => (L[S] R[T=>S])

Soundness follows rather simply because when S has the truth value false then R[S] and L[S] also has the truth value false.  
When R[S](or L[S]) has the truth value false it doesn't matter what the other side of E, L[T] or R[T] evaluates to, E will evaluate to true.  
When R[S](or L[S]) has the truth value true then L[T=>S] (and R[T=>S]) will evaluate to the same value as L[T] (or R[T]).  
Therefore, the right-hand sides of the iteration rules always evaluate to the same truth value as the left-hand sides.  
In other words, the two sides of the iteration rewrite rules are semantically equivalent and the rules are sound.  

#### Generalized Insertion

Generalized insertion is like a special case of generalized iteration where one side of an expression E is F.  
In this special case, *any* expression can be iterated into the other side of E.  

Rule: Given an expression S, and an expression E of the form (L R),  
	where either L or R is F then 
	all copies of T in the other side of the expression may be replaced with S.  
	 						
Expressed as rewrite rules...  
	- (Q[T] F) => (Q[T=>S] F), or  
	- (F Q[T]) => (F Q[T=>S])

Soundness is trivial since, because of the presence of the F expression inside the cut, 
both sides of both rewrite rules will always evaluate to true.  
One can literally replace the other side of E with any expression, much less a derived expression.  
Therefore, the insertion rules are sound.  
	
Note that when L == T (or R == T), then generalized insertion is equivalent to classic insertion.  


#### Generalized Deiteration

Rule: Given a subterm S of an expression E of the form (L R),  
	where S is a left or right, F-grounding F-cofactor of E then 
	all copies of S in the other side of the expression may be replaced with T.  
	 						
Expressed as rewrite rules...  
1. (L[S] R[S]) => (L[S=>T] R[S]), or  
2. (L[S] R[S]) => (L[S] R[S=>T])

Since it reduces the left-hand side rather than the right-hand side,  
rule 1 is guaranteed to reduce an expression more than rule 2.  
Therefore, if an expression can be reduced using either one of these rules then rule 1 is preferred.  
In this sense, the rules are ordered.  

Note that when S == L, or S == R, then generalized deiteration is equivalent to classic deiteration.  

