

----------
This post contains a non-constructive proof that expressions in a certain system of propositional logic can be minimized in a polynomial # of steps.

First, a simple system of propositional logic is presented.  
Then a confluent set of reduction rules that can minimize the expressions of that system is constructed/derived.  
This set of reduction rules is derived using a custom version of the Knuth/Bendix Completion method that has been specifically designed to enumerate the reduction rules of the logic system.  
Then it will be shown that the reduction rules can minimize expressions of the logic system in a polynomial number of steps, where each step is an application of a inference rule of the system.  

The set of rules is infinite and not realizable, and the rules themselves are not computed in polytime.
The point of this is just to show that polynomial minimization proofs always exist for any given expression, 
not that you can actually compute these proofs in real time.  

---------------
# System T
Let system T be a system of propositional logic. 
In system T the constants T and F (represented by the numbers 1 and 2) are expressions.  
Variables are also expressions (represented as numbers, starting with 3).  
An expression may also be composed of nand operator and two expressions, the lhs (left hand side) and the rhs (right hand side).  
There are two things that make system T different than other logic systems...
- Expressions are ordered
- The structural inference rules are only applied to special terms called 'cofactors'
## Contexts
Instead of writing nand operations with a '|', nand operations are denoted with parentheses, like so... (x y).
A pair of parentheses is called a *context*.  
## Canonical and Mostly-Canonical
The concept of minimization, or proof, can be simplified right now, before we continue further, by realizing the consequences of nesting in expressions.  
Given an expression E of the form (L R), reducing an expression comes down either reducing L, R, or E.  
At some point L and R will not have any subterms and the only way to simplify E is to simply E.  
(1 1) is an example of a reducible expression where the subterms are not reducible, it reduces to (T 1).  
By noticing this recursion we can simplify the proof procedure by making the procedure recursive.    
We will start by defining two important kinds of expressions...  
- *Canonical*, an expression where there is no expression X such that X is equivalent to E and X is simpler than than E.  
- *Mostly-Canonical*, an expression E of the form (L R) where L and R are canonical but E is not.  
> Note that the concept of *simple* has not been defined yet, *entropy* will be defined later.  
What's important in these definitions is the idea that canonical expressions are the *simplest* expressions and that mostly-canonical expressions are the *next simplest* kind of expressions.  
That is, mostly-canonical expressions are one proof step away from canonical expressions.
The name 'mostly-canonical' was inspired by the concept of being mostly dead, because there's a big difference between being mostly dead and all dead.  
*All* the real work in a proof is done by converting mostly-canonical expressions into canonical expressions.  
## Cofactors
The expression E[S=>R] |- C means that if you replace S in E with R then the result is C.
A *cofactor* is an implication between terms in an expression, such that E[S=>R] |- C, where...  
- E is a mostly-canonical expression 
- S is a subterm in E, S is canonical since E is mostly-canonical 
- R (aka replacement), is a minimized expression, aka axiomatic
- C (aka conclusion), is a minimized expression, aka axiomatic
The structural inference rules in system T are only applied to subterms of an expression that are cofactors.
Doing so makes it possible to use cofactors to discover and reverse applications of the structural inference rules.  
In a general sense, a cofactor records how the terms of an expression are related to each other.
### F and T cofactors
An *F-cofactor* is a cofactor of the form E[S=>F] |- C, that is, a cofactor where all instances of S are replaced by F.
A *T-cofactor* is a cofactor of the form E[S=>T] |- C, that is, a cofactor where all instances of S are replaced by T.
### F-grounding and T-grounding cofactors
An *F-grounding* cofactor is a cofactor where the conclusion is F.
A *T-grounding* cofactor is a cofactor where the conclusion is T.
### Left and Right Cofactors
Given an expression E of the form (L R) then...  
- A left cofactor occurs in L, the left-hand side of an expression.  
- A right cofactor occurs in R, the right-hand side of an expression.  
## Ordering
System T also includes an ordering of expressions based on entropy/simplicity...  
- Constants have the least entropy, T has the least, then F  
- All variables have higher entropy than constants.  
- A variable represented by a higher number has more entropy than a variable with a lower number.  
- All other things being equal, expressions with the lowest numbered variables have less entropy  
- All other things being equal, expressions with the highest numbered variables have more entropy  
- All other things being equal, shorter expressions have less entropy than longer expressions 
- All other things being equal, expressions with lower entropy on the left side have less entropy  
## Inference Rules
System T has the following inference rules...
- ordering. Example (2 1) => (1 2).   Orders terms in an expression according to their emtropy.
- double negation elimination. (T (T x)) => x
- erasure.  ((T T) Q) => T 
- iteration, aka weakening.  Example: (T x) => (x x)
	Iteration makes copies of a term within an expression without changing the truth function of the expression.
	Given a subterm S of an expression E of the form (L R),  where S is a left or right, F-grounding F-cofactor of E then any or all copies of T in the other side of the expression may be replaced with S.   						
	Iteration expressed as rewrite rules...  
	1. (L[T] R[S]) => (L[T=>S] R[S]), or  
	2. (L[S] R[T]) => (L[S] R[T=>S])
	Note that when R[S] == S, or L[S] == S then generalized iteration is similar to classic EG iteration.  
	Note that when R[S] == F, or L[S] == F then generalized iteration is similar to classic EG insertion  
- deiteration, aka contraction.  Example: (x x) => (T x)
	Deiteration removes unnecessary terms from an expression without changing the truth function of the expression.
    Given a subterm S of an expression E of the form (L R),  where S is an f-grounding f-cofactor of either L or R	then any or all instances of S in the other side of the expression may be replaced with T.  
	Deiteration expressed as rewrite rules...  
	1. (L[S] R[S]) => (L[S=>T] R[S]), or  
	2. (L[S] R[S]) => (L[S] R[S=>T])
	Note that when R[S] == S, or L[S] == S then generalized iteration is similar to classic EG deiteration.  
	Note that when R[S] == F, or L[S] == F then generalized iteration is similar to classic EG erasure.  
- exchange (aka permutation.  Example: (T ((x (T y)) ((T x) y))) => ((x y) ((T x) (T y)))
	The exchange rule exchanges terms between the sides of a context in a way that doesn't change the truth function of the expression.  
	Let E be an expression E of the form (L R).  
	If there is an F-grounding cofactor X of L,  
	and if there is an F-grounding cofactor Y of R,  
	then an expression E' may be produced from E by...
	- replacing X.S in L with !Y.S and 
	- replacing Y.S in R with !X.S
	Expressed as a rewrite rule...  
	1. (L[X.S] R[Y.S]) => (L[X.S <- !Y.S]  R[Y.S <- !X.S]) 
	This rule produces a reduction or a deduction depending on the entropies of the cofactor subterms 
	and the # of times each term is iterated.  

## System T is equivalent to Existential Graphs.
> This can be proved by translating expressions to/from both systems.  
> In such translation, nand operators become cuts, T's become empty spaces, F represents an empty cut.  
> Dau provides a similar proof for a system used to prove completeness of existential graphs.
> Complete proof upon request :-)
Existential Graphs are known to be complete and consistent, see Dau.  
Therefore T is complete and consistent.  

----------------------------
# RULES

Now, a set of reduction rules for the system will be constructed, using a method that is a custom version of the Knuth-Bendix Completion method.  

Let RULES be a prefix tree of every known reduction rule, initially empty.
The prefix tree itself encodes the left-hand sides of rules, and the leaf nodes of tree include the RHS of a rule.  Thus, the tree implements a mapping from the lhs of a rule to the rhs of the rule.  
To find a rule that can be applied to a given expression, the tree and the expression are simultaneously navigated, and unification is used to match the lhs of a rule to the expression.  
A match is found by navigating all the way down to a leaf node that provides the rhs of the corresponding reduction rule.
Its already known that prefix tree lookup has a linear time complexity, even for an infinite list, the use of unification in this procedure doesn't change that.

Let CANONICAL be a dictionary of all known canonical expressions, indexed by a string that represents the expression's truth function.
Let TRUTH(e) be a function that returns a string representation of an expressions truth function.

The rule generation process works by enumerating all possible expressions, from the simplest possible expressions to more complex expressions and discovering new reduction rules.  
> The workings of this enumeration will take a lot of space to document and is not included here.  

Let E be an expression of the form (L R) in the enumeration.
E is always a mostly-canonical expression, that is, both L and R are canonical.
If E can already be reduced by any rule in RULES then 
	Continue to the next expression in the enumeration.  
If E can't be reduced by any rule in RULES then 
	Let F = TRUTH(E).
	If CANONICAL already contains a value for F then
		LET C = CANONICAL[F].
		Let R be a new reduction rule of the form E => C.  
		Add R to RULES.  
	else		
		E is canonical, the simplest expression of a truth function.
		SET CANONICAL[F] = E
		Continue to next expression in the enumeration
This procedure will run forever, producing an infinite list of reduction rules.
We are interested in the infinite list of RULES generated by this procedure because the rules in the RULES list are easily seen to be globally confluent.  
And its not obvious, but the generated rules are individual applications of the inference rules.
> Proof provided upon request :-).  
> We can prove this by showing that if we start the traditional Knuth-Bendix method with a completed RULES list then the traditional method will never produce a new rule.  
> In other words, the starting set of rules was already complete and therefore the set of rules generated by the above method is complete.
> Interesting Note: If you try this with a different logic system you wont get reduction rules that are applications of known inference rules.  
> This works for system T because it seems that the inference rules are emergent properties of the structure of the expressions.  


----------------------------
# Minimization Procedure

Now assume that Apollo, the god of logic and reason, has run the rule discovery procedure to completion and made the RULES tree available to us.  
If a given expression can be reduced then we will be able to find a matching reduction rule in RULES in linear time!

The procedure to minimize an expression then works like this...
Given an expression E
While there is a term S in E that can be reduced by a rule in the RULES tree
	Let R = the result of apply the rule to S
	Let E' = E[S=>R], that is, replace all instances of S in E with R.
	Set E to E' and repeat.
When E is no longer reducible then E is canonical

The question is... how many reductions can it take to reduce an expression to its canonical form?
It takes at most O(N^2) reductions, here's why... 
Almost every reduction reduces the length of the expression.  
The only time that an expression is not reduced in length by a reduction rule 
is when the canonical form of the expression is the same length as the non-canonical expression.



So... if a reduction rule is applied to an expression, and the result is the same length as the expression, 
then you know the result is canonical and cannot be further reduced.  
Thus, the most reductions required to reduce the length of an expression is N, the # of terms in the expression, also the length of the expression.  

And at most you could repeat this process N-1 times before the expression has a length of 1.
Therefore it can take at most N * (N -1) reductions to reduce an expression to it canonical form.

----------------------------
# Summary

An expression ordering is necessary in order to define what 'simple' is.  
And cofactors are necessary in order to limit the structural inference rules to applications that are logically valid.
The reduc


















In hindsight...
- I should have known what the response would be :-).
- I should have put *non-constructive* in the title.
I'm serious though, I want to know if anyone sees value in this.
I'm convinced that it should be possible to build an efficient, concrete minimization algorithm from this but I've concluded that I'm not the guy to do it.  
I have a C# implementation of everything described here, and several attempts to build a concrete minimization algorithm in a private github project. This outline is ripped from the code documentation.
I might do something with this project if I dont get totally torched doing this :-).  
But torch away....
PS: 
I've had to split my response into 3 additional posts...

- The System
      First, a simple system of propositional logic is presented.  

- Reduction Rule Generation
Then a confluent set of reduction rules that can minimize the expressions of that system is constructed/derived.  
This set of reduction rules is derived using a custom version of the Knuth/Bendix Completion method that has been specifically designed to enumerate the reduction rules of the logic system.  

- Minimization, Complexity, Summary
Then it will be shown that the reduction rules can minimize expressions of the logic system in a polynomial number of steps, where each step is an application of a inference rule of the system.  
The point of this is just to show that polynomial minimization proofs always exist for any given expression, not that you can actually compute these proofs in real time.  
---------------
# System T
Let system T be a system of propositional logic. 
In system T the constants T and F (represented by the numbers 1 and 2) are expressions.  
Variables are also expressions (represented as numbers, starting with 3).  
An expression may also be composed of nand operator and two expressions, the lhs (left hand side) and the rhs (right hand side).  
There are two things that make system T different than other logic systems...
- Expressions are ordered
- The structural inference rules are only applied to special terms called 'cofactors'
## Contexts
Instead of writing nand operations with a '|', nand operations are denoted with parentheses, like so... (x y).
A pair of parentheses is called a *context*.  
## Canonical and Mostly-Canonical
The concept of minimization, or proof, can be simplified right now, before we continue further, by realizing the consequences of nesting in expressions.  
Given an expression E of the form (L R), reducing an expression comes down either reducing L, R, or E.  
At some point L and R will not have any subterms and the only way to simplify E is to simply E.  
(1 1) is an example of a reducible expression where the subterms are not reducible, it reduces to (T 1).  
By noticing this recursion we can simplify the proof procedure by making the procedure recursive.    
We will start by defining two important kinds of expressions...  
- *Canonical*, an expression where there is no expression X such that X is equivalent to E and X is simpler than than E.  
- *Mostly-Canonical*, an expression E of the form (L R) where L and R are canonical but E is not.  
> Note that the concept of *simple* has not been defined yet, *entropy* will be defined later.  
What's important in these definitions is the idea that canonical expressions are the *simplest* expressions and that mostly-canonical expressions are the *next simplest* kind of expressions.  
That is, mostly-canonical expressions are one proof step away from canonical expressions.
The name 'mostly-canonical' was inspired by the concept of being mostly dead, because there's a big difference between being mostly dead and all dead.  
*All* the real work in a proof is done by converting mostly-canonical expressions into canonical expressions.  
## Cofactors
The expression E[S=>R] |- C means that if you replace S in E with R then the result is C.
A *cofactor* is an implication between terms in an expression, such that E[S=>R] |- C, where...  
- E is a mostly-canonical expression 
- S is a subterm in E, S is canonical since E is mostly-canonical 
- R (aka replacement), is a minimized expression, aka axiomatic
- C (aka conclusion), is a minimized expression, aka axiomatic
The structural inference rules in system T are only applied to subterms of an expression that are cofactors.
Doing so makes it possible to use cofactors to discover and reverse applications of the structural inference rules.  
In a general sense, a cofactor records how the terms of an expression are related to each other.
### F and T cofactors
An *F-cofactor* is a cofactor of the form E[S=>F] |- C, that is, a cofactor where all instances of S are replaced by F.
A *T-cofactor* is a cofactor of the form E[S=>T] |- C, that is, a cofactor where all instances of S are replaced by T.
### F-grounding and T-grounding cofactors
An *F-grounding* cofactor is a cofactor where the conclusion is F.
A *T-grounding* cofactor is a cofactor where the conclusion is T.
### Left and Right Cofactors
Given an expression E of the form (L R) then...  
- A left cofactor occurs in L, the left-hand side of an expression.  
- A right cofactor occurs in R, the right-hand side of an expression.  
## Ordering
System T also includes an ordering of expressions based on entropy/simplicity...  
- Constants have the least entropy, T has the least, then F  
- All variables have higher entropy than constants.  
- A variable represented by a higher number has more entropy than a variable with a lower number.  
- All other things being equal, expressions with the lowest numbered variables have less entropy  
- All other things being equal, expressions with the highest numbered variables have more entropy  
- All other things being equal, shorter expressions have less entropy than longer expressions 
- All other things being equal, expressions with lower entropy on the left side have less entropy  
## Inference Rules
System T has the following inference rules...
### Ordering. 
Example (2 1) => (1 2).   Orders terms in an expression according to their entropy.
### empty context elimination.  
(T T) => F
. Same as empty cut elimination in existential graph
### double negation elimination. 
(T (T x)) => x
### erasure.  
(F Q) => T 
### insertion.  
T => (F Q)
### iteration, aka weakening.  
Example: (T x) => (x x)
Iteration makes copies of a term within an expression without changing the truth function of the expression.
Given a subterm S of an expression E of the form (L R),  where S is a left or right, F-grounding F-cofactor of E then any or all copies of T in the other side of the expression may be replaced with S.   						
	Iteration expressed as rewrite rules...  
	1. (L[T] R[S]) => (L[T=>S] R[S]), or  
	2. (L[S] R[T]) => (L[S] R[T=>S])
	Note that when R[S] == S, or L[S] == S then generalized iteration is similar to classic EG iteration.
	Note that when R[S] == F, or L[S] == F then generalized iteration is similar to classic EG insertion  
### deiteration, aka contraction.  
Example: (x x) => (T x)
Deiteration removes unnecessary terms from an expression without changing the truth function of the expression.
Given a subterm S of an expression E of the form (L R),  where S is an f-grounding f-cofactor of either L or R	then any or all instances of S in the other side of the expression may be replaced with T.  
Deiteration expressed as rewrite rules...  
1. (L[S] R[S]) => (L[S=>T] R[S]), or  
2. (L[S] R[S]) => (L[S] R[S=>T])
Note that when R[S] == S, or L[S] == S then generalized iteration is similar to classic EG deiteration.  
Note that when R[S] == F, or L[S] == F then generalized iteration is similar to classic EG erasure.  
### exchange aka permutation.  
Example: (T ((1 (1 2)) ((1 2) 2))) => ((1 2) ((1 T) (T 2)))
The exchange rule exchanges terms between the sides of a context in a way that doesn't change the truth function of the expression.  
Let E be an expression E of the form (L R).  
If there is an F-grounding cofactor X of L,  
and if there is an F-grounding cofactor Y of R,  
then an expression E' may be produced from E by...
- replacing X.S in L with !Y.S and 
- replacing Y.S in R with !X.S
Expressed as a rewrite rule...  
1. (L[X.S] R[Y.S]) => (L[X.S <- !Y.S]  R[Y.S <- !X.S]) 
This rule produces a reduction or a deduction depending on the entropies of the cofactor subterms 	and the # of times each term is iterated.  

## System T is equivalent to Existential Graphs.
> This can be proved by translating expressions to/from both systems.  
> In such translation, nand operators become cuts, T's become empty spaces, F represents an empty cut.  
> Dau provides a similar proof for a system used to prove completeness of existential graphs.
> Complete proof upon request :-)
Existential Graphs are known to be complete and consistent, see Dau.  
Therefore T is complete and consistent.  
----------------------------
# RULES
Now, a set of reduction rules for the system will be constructed, using a method that is a custom version of the Knuth-Bendix Completion method.  
Let RULES be a prefix tree of every known reduction rule, initially empty.
The prefix tree itself encodes the left-hand sides of rules, and the leaf nodes of tree include the RHS of a rule.  Thus, the tree implements a mapping from the lhs of a rule to the rhs of the rule.  
To find a rule that can be applied to a given expression, the tree and the expression are simultaneously navigated, and unification is used to match the lhs of a rule to the expression.  
A match is found by navigating all the way down to a leaf node that provides the rhs of the corresponding reduction rule.
Its already known that prefix tree lookup has a linear time complexity, even for an infinite list, the use of unification in this procedure doesn't change that.
Let CANONICAL be a dictionary of all known canonical expressions, indexed by a string that represents the expression's truth function.
Let TRUTH(e) be a function that returns a string representation of an expressions truth function.
The rule generation process works by enumerating all possible expressions, from the simplest possible expressions to more complex expressions and discovering new reduction rules.  
> The workings of this enumeration will take a lot of space to document and is not included here.  
Let E be an expression of the form (L R) in the enumeration.
E is always a mostly-canonical expression, that is, both L and R are canonical.
If E can already be reduced by any rule in RULES then 
	Continue to the next expression in the enumeration.  
If E can't be reduced by any rule in RULES then 
	Let F = TRUTH(E).
	If CANONICAL already contains a value for F then
		LET C = CANONICAL[F].
		Let R be a new reduction rule of the form E => C.  
		Add R to RULES.  
	else		
		E is canonical, the simplest expression of a truth function.
		SET CANONICAL[F] = E
		Continue to next expression in the enumeration
This procedure will run forever, producing an infinite list of reduction rules.
We are interested in the infinite list of RULES generated by this procedure because the rules in the RULES list are easily seen to be globally confluent.  
And its not obvious, but the generated rules are individual applications of the inference rules.
> Proof provided upon request :-).  
> We can prove this by showing that if we start the traditional Knuth-Bendix method with a completed RULES list then the traditional method will never produce a new rule.  
> In other words, the starting set of rules was already complete and therefore the set of rules generated by the above method is complete.
> Interesting Note: If you try this with a different logic system you wont get reduction rules that are applications of known inference rules.  
> This works for system T because it seems that the inference rules are emergent properties of the structure of the expressions.  

----------------------------
# MINIMIZATION
Now assume that Apollo, the god of logic and reason, has run the rule discovery procedure to completion and made the RULES tree available to us.  
If a given expression can be reduced then we will be able to find a matching reduction rule in RULES in linear time!

The procedure to minimize an expression then works like this...
Given an expression E
While there is a term S in E that can be reduced by a rule in the RULES tree
	Let R = the result of apply the rule to S
	Let E' = E[S=>R], that is, replace all instances of S in E with R.
	Set E to E' and repeat.
When E is no longer reducible then E is canonical

# COMPLEXITY
The question is... how many reductions can it take to reduce an expression to its canonical form?
It takes at most O(N^2) reductions, here's why... 
Almost every reduction reduces the length of the expression.  
The only time that an expression is not reduced in length by a reduction rule 
is when the canonical form of the expression is the same length as the non-canonical expression.

So... if a reduction rule is applied to an expression, and the result is the same length as the expression, 
then you know the result is canonical and cannot be further reduced.  
Thus, the most reductions required to reduce the length of an expression is N, the # of terms in the expression, also the length of the expression.  
And at most you could repeat this process N-1 times before the expression has a length of 1.
Therefore it can take at most N * (N -1) reductions to reduce an expression to it canonical form.
QED


















# System T

Let system T be a system of propositional logic. In system T the constants T and F (represented by the numbers 1 and 2) are expressions.  
Variables are also expressions (represented as numbers, starting with 3).  
An expression may also be composed of nand operator and two expressions, the lhs (left hand side) and the rhs (right hand side).  
There are two things that make system T different than other logic systems...

* Expressions are ordered
* The structural inference rules are only applied to special terms called 'cofactors'

# Contexts

Instead of writing nand operations with a '|', nand operations are denoted with parentheses, like so... (x y). A pair of parentheses is called a *context*.

# Canonical and Mostly-Canonical

- *Canonical*, an expression where there is no expression X such that X is equivalent to E and X is simpler than than E.
- *Mostly-Canonical*, an expression E of the form (L R) where L and R are canonical but E is not.

>Note that the concept of *simple* has not been defined yet, *entropy* will be defined later.  
What's important in these definitions is the idea that canonical expressions are the *simplest* expressions and that mostly-canonical expressions are the *next simplest* kind of expressions.  
That is, mostly-canonical expressions are one proof step away from canonical expressions. 

# Cofactors

The expression E\[S=>R\] |- C means that if you replace S in E with R then the result is C. A *cofactor* is an implication between terms in an expression, such that E\[S=>R\] |- C, where...

- E is a mostly-canonical expression
- S is a subterm in E, S is canonical since E is mostly-canonical
- R (aka replacement), is a minimized expression, aka axiomatic
- C (aka conclusion), is a minimized expression, aka axiomatic The structural inference rules in system T are only applied to subterms of an expression that are cofactors. Doing so makes it possible to use cofactors to discover and reverse applications of the structural inference rules.
- In a general sense, a cofactor records how the terms of an expression are related to each other.

# F and T cofactors

An *F-cofactor* is a cofactor of the form E\[S=>F\] |- C, that is, a cofactor where all instances of S are replaced by F. A *T-cofactor* is a cofactor of the form E\[S=>T\] |- C, that is, a cofactor where all instances of S are replaced by T.

# F-grounding and T-grounding cofactors

An *F-grounding* cofactor is a cofactor where the conclusion is F. A *T-grounding* cofactor is a cofactor where the conclusion is T.

# Ordering

System T also includes an ordering of expressions based on entropy/simplicity...

- Constants have the least entropy, T has the least, then F
- All variables have higher entropy than constants.
- A variable represented by a higher number has more entropy than a variable with a lower number.
- All other things being equal, expressions with the lowest numbered variables have less entropy
- All other things being equal, expressions with the highest numbered variables have more entropy
- All other things being equal, shorter expressions have less entropy than longer expressions
- All other things being equal, expressions with lower entropy on the left side have less entropy

# Inference Rules

System T has the following inference rules...

# Ordering.

Example (2 1) => (1 2).   Orders terms in an expression according to their entropy.

# empty context elimination.

(T T) => F . Same as empty cut elimination in existential graph

# double negation elimination.

(T (T x)) => x

# erasure.

(F Q) => T

# insertion.

T => (F Q)

# iteration, aka weakening.

Example: (T x) => (x x)   
Iteration makes copies of a term within an expression without changing the truth function of the expression. Given a subterm S of an expression E of the form (L R),  where S is a left or right, F-grounding F-cofactor of E then any or all copies of T in the other side of the expression may be replaced with S.   
Iteration expressed as rewrite rules...  
1. (L\[T\] R\[S\]) => (L\[T=>S\] R\[S\]), or  
2. (L\[S\] R\[T\]) => (L\[S\] R\[T=>S\]) Note that when R\[S\] == S, or L\[S\] == S then generalized iteration is similar to classic EG iteration. Note that when R\[S\] == F, or L\[S\] == F then generalized iteration is similar to classic EG insertion

# deiteration, aka contraction.

Example: (x x) => (T x)   
Deiteration removes unnecessary terms from an expression without changing the truth function of the expression. Given a subterm S of an expression E of the form (L R),  where S is an f-grounding f-cofactor of either L or R then any or all instances of S in the other side of the expression may be replaced with T.  
Deiteration expressed as rewrite rules...

1. (L\[S\] R\[S\]) => (L\[S=>T\] R\[S\]), or
2. (L\[S\] R\[S\]) => (L\[S\] R\[S=>T\]) Note that when R\[S\] == S, or L\[S\] == S then generalized iteration is similar to classic EG deiteration.
3. Note that when R\[S\] == F, or L\[S\] == F then generalized iteration is similar to classic EG erasure.

# exchange aka permutation.

Example: (T ((1 (1 2)) ((1 2) 2))) => ((1 2) ((1 T) (T 2)))   
The exchange rule exchanges terms between the sides of a context in a way that doesn't change the truth function of the expression.  
Let E be an expression E of the form (L R).  
If there is an F-grounding cofactor X of L,  
and if there is an F-grounding cofactor Y of R,  
then an expression E' may be produced from E by replacing X.S in L with !Y.S and replacing Y.S in R with !X.S

Expressed as a rewrite rule...

1. (L\[X.S\] R\[Y.S\]) => (L\[X.S <- !Y.S\]  R\[Y.S <- !X.S\]) This rule produces a reduction or a deduction depending on the entropies of the cofactor subterms 	and the # of times each term is iterated.

# System T is equivalent to Existential Graphs.

>This can be proved by translating expressions to/from both systems.  
In such translation, nand operators become cuts, T's become empty spaces, F represents an empty cut.  
Dau provides a similar proof for a system used to prove completeness of existential graphs. Complete proof upon request :-) Existential Graphs are known to be complete and consistent, see Dau.  
Therefore T is complete and consistent.