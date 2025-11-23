# Structured Expressions  

The Structured Expressions system (SE) is a formal system of structural logic inspired by [Existential Graphs](https://en.wikipedia.org/wiki/Existential_graph)(EG).  
The SE system is primarily designed to minimize any given expression to its canonical form in a polynomially bounded # of steps.  

Like the EG system, the SE system is equivalent to classical propositional calculus.  
A proof of such is included in an appendix, 
where a two-way translation between the formulas and derivations of both systems is presented.  

SE is presented by way of describing the differences between SE and EG...
- Syntax
	> The EG system is a system of logic that doesnt have a textual notation, it groups symbols on a page by drawing lines around them.  
	> SE has a textual notation that is designed to be used in modern programming languages and embedded in document formats like Markdown.  
	> In SE, a pair of parentheses define a *context*. Example: (a b).  A context is semantically the same as NAND(a,b).
	> SE uses parentheses instead of operators because SE is a structural logic system and not an algebraic system.   
	> Similar to EG, contexts may be nested.  
- Binary
	> SE restricts contexts to just two symbols.
	> Examples: (a b), (a (T b))
- Entropy
	> SE is designed to transform expressions to/from their canonical form, and one needs a path ordering for that. 
	> For instance, without an ordering these two expression are equivalent; (a b), (b a).
	> In SE, (a b) and (b a) are ordered such that (a b) < (b a).  
	> For expressions A and B, when A < B we say that B has a higher entropy than A, and A has lower entropy than B.
	> In the SE system, canonical expressions are the form with the lowest entropy.
- Exchange Rule
	> The EG system is a structural logic system that uses the rules of iteration (weakening) and deiteration (contraction).  
	> The EG system excludes the exchange rule.  
	> The SE system is also a structural logic system and uses all three rules.  
	> The exchange rule is not strictly required in SE, but it's crucial to building short proofs.  
- Cofactor directed exchange
	> *Cofactors* are used to efficiently find an exchange of terms that lowers the entropy of an expression.  
	> In SE, cofactors are modelled using Krom logic.
	> Could also use an implication graph, binary decision diagram, or hypergraph instead of Krom logic.
- Efficient Proofs 
	> Three aspects of the SE proof procedure contribute to achieving a polynomial bound on the size of proofs...
	> - Reducing expressions from the bottom up.  
	> - Always lowering the entropy of an expression in each step of a proof.  
	> - Using cofactors and Krom logic to efficiently find term exchanges that lower entropy.  

## Syntax

The basic syntax is simple...
- The symbol T is an expression that represents an empty space, a space that can possibly be filled in later.  
	> Semantically, T means "true"
- The symbol F is an expression that represents an empty context.  
	> Semantically, F means "not true"
- Variables use the [Base 32 Encoding with Extended Hex Alphabet](https://en.wikipedia.org/wiki/Base32#Base_32_Encoding_with_Extended_Hex_Alphabet_per_%C2%A77).  	
	> Variables are numbers and have the same ordering.  
	> Conventions; always use lower case instead of upper, embedded hyphens are translated to 0.  
	> Examples: one, 2, tvc15 (because Hex32 uses letters up to 'V'), cat-dog  
- Two expressions may be bounded by a *context* (a pair of parentheses) and separated by a space.	
	> Examples; (T a), (cat-dog (T (cat (T dog)))

### Conversions From natural expressions

This is a little off topic but, if you're going to play around with structured expressions 
then there is a trick that makes structures expressions easier to use.  
The trick is to annotate contexts with the boolean operators used in natural deduction, 
doing so makes it a lot easier to think with SEs.  

For example, this boolean expression....
```a -> b```  
...is interpreted as 'if a then b' which I would write in SE as...  
```(a -> (T b))```   
The trick is that if you strip the boolean operators out of the above expression 
then what's left will be a valid SE expression that is equivalent to the starting boolean expression...
```(a (T b))```   

It easy to write these expressions with just a little practice once you know the rules...

    - NAND: a | b    => (a b)
    - NOT:  ~a       => ~(T a)
    - IMPL: a -> b   => (a -> (T b))
    - AND:  a && b   => (T (a && b))
    - OR:   a || b   => ((T a) || (T b))

### Variable Ordering

Unlike EG, in SE the variables are extended hex numbers, and therefore also ordered.  
For instance; 0 < 9 < bee < supercalifragilisticekspialidocious 

## Flatterm

A flatterm is an array of an expressions' subterms, enumerated in a depth-first fashion, starting with the expression itself.  
This structure is an extremely useful way to represent expressions.  

### Example
(a (b (b c)))

Flatterm of (a (b (b c)))...  

subterm 		index	depth
------------	-----	-------
(a (b (a c))) 	0		0
a ;leftmost		1       1
(b (a c))		2		1
b ;leftmost		3		2
(b c)			4		2
b				5		3
c				6		3

Length = 7 = Sizeof(Flatterm( (a (b (b c))) ))

## Length

The *length* of an LE expression is defined as the length of the expressions' flatterm.  

Notes...
- Length = # of parentheses + 1.
- Length can be thought of as the # of nodes in an AST of an expression.	 
- Expressions always have an odd length.  


## Depth 

Depth is a property of a subterm in an expressions' flatterm.  

Simply, a subgraphs' depth is the number of left parentheses that you need to cross to get to that subgraph.  

0 <= Depth.
	

## Canonical and Mostly-Canonical

The SE concept of reduction, or proof, can be simplified right now, before we continue further, 
by realizing the consequences of nesting in expressions.  

Given an expression E of the form (L R), reducing an expression comes down either reducing L, R, or E.  
At some point L and R will not have any subterms and the only way to simplify E is to simply E.  
(1 1) is an example a reducible expression where the subterms are not reducible, it reduces to (T 1).  

By noticing this recursion we can simplify SE's proof procedure by making the procedure recursive.    

We will start by defining two important kinds of SE expressions...  

- *Canonical*, an expression where there is no expression X such that X is equivalent to E and X is simpler than than E.  

- *Mostly-Canonical*, an expression E of the form (L R) where L and R are canonical but E is not.  

> Note that the concept of *simple* has not been defined yet, *entropy* will be defined next.  

What's important in these definitions is the idea that canonical expressions are the *simplest* expressions 
and that mostly-canonical expressions are the *next simplest* kind of expressions.  
That is, mostly-canonical expressions are one proof step away from canonical expressions.

The name 'mostly-canonical' was inspired by the concept of being mostly dead, 
because there's a big difference between being mostly dead and all dead.  
*All* the real work in a proof is done by converting mostly-canonical expressions into canonical expressions.  
If an expression is canonical, well, then there's nothing left to do but look through it's pockets for loose change.  

### Summary

The entire SE proof process can be boiled down to reducing only mostly-canonical expressions to canonical expressions.  

## Entropy

Any abstract reduction system needs an ordering that defines what 'reduction' means.  

The SE system uses the term *entropy* to describe how reduced an expression is or is not.  

Axiomatic expressions, like constants, variables, negations of axioms, etc, 
obviously have less entropy than other expressions.  
For non-axiomatic expressions, entropy is defined as 
the minimum number of steps it takes to derive the expression from an axiomatic expression.  

Now, a trick is going to be pulled here.  
Entropy has just been defined in terms of the number of steps in a proof.  
Next, we are going to present an ordering that has nothing to do with steps in proofs.  
That's because later we are going to present inference rules that obey the ordering.  
In essence, the ordering is not designed to describe how the inference rules work.  
Instead, the inference rules in SE were specifically designed to obey the ordering.  
In that way we can know that these rules define the concept of entropy as defined above 
without ever having to mention proof sizes.

Here's the ordering...  

- Constants have the least entropy, T has the least, then F
- All variables have higher entropy than constants.
- A variable represented by a higher number has more entropy than a variable with a lower number.
- All other things being equal, expressions with the lowest numbered variable have less entropy 
- All other things being equal, expressions with fewer variables have less entropy 
- All other things being equal, expressions with fewer contexts have less entropy 
- All other things being equal, expressions with lower entropy on the left side have less entropy  
- All other things being equal, expressions with lower entropy on the right side have less entropy  

### Summary
Any abstract reduction system needs an ordering that defines what 'reduction' means.  
Entropy is a measure of how many steps it takes to prove an expression.
In SE, expressions are ordered according to their entropy.
The SE proof process is designed to transition expressions to a lower entropy in exactly the order presented here.

## Cofactors  

Cofactors are *very* important in the SE system.  

The structural inference rules in the SE system (1) are designed to use cofactors to guide the application of the rules 
in such a way that it's possible to use cofactors to discover and reverse the applications.  
> 1. the SE inference rules are presented later

A *cofactor* is an implication between terms in an expression, such that E[S=>R] |- C, where...  
- E is a mostly-canonical expression 
- S is a subterm in E, S is canonical since E is mostly-canonical 
- R (aka replacement), is a minimized expression, aka axiomatic
- C (aka conclusion), is a minimized expression, aka axiomatic

In a general sense, a cofactor records how facts are related to each other.

### Example
Consider this expression... (T ((1 (1 2)) ((1 2) 2))).  

Because the term (1 2) appears in both sides of the left-hand side,  
it's relatively easy for a human to identify a cofactor that reduces the expression.  
The presence of two instances of (1 2) on different sides of an equation is easy for a human to spot.  
It's not too difficult a human that knows the rules to deduce that (1 2) is a grounding cofactor and hence can be swapped with T.  
Like existential graphs, the structure of the expression takes advantage of a humans' pattern recognition abilities.  

Thus, a human can easily see that, by the exchange rule,  
T and (1 2) can be exchanged in (T ((1 (1 2)) ((1 2) 2))) to get ((1 2) ((1 T) (T 2))),  
which has lower entropy.

It's not so easy for a human to figure out that (T ((1 (T 2)) ((T 1) 2))) also reduces to ((1 2) ((1 T) (T 2))).  
That's because reducing this formula requires a human to perform logic in their head to 'unify' the contexts with T's in them.  
The needed deductions are relatively simple for a machine but very tedious and error prone for a human to execute.  
An implication graph can make the job simple, so can binary decision diagrams, and so can hypergraphs.  

The SE system uses 'cofactor models', a set of disjunctive clauses, to represent a implication graphs.  

### F and T cofactors

An *F-cofactor* is a cofactor of the form E[S=>F] |- C, that is, a cofactor where all instances of subterm are replaced by F.
A *T-cofactor* is a cofactor of the form E[S=>T] |- C, that is, a cofactor where all instances of a subterm are replaced by T.

### F-grounding and T-grounding cofactors

An *F-grounding* cofactor is a cofactor where the conclusion is F.
A *T-grounding* cofactor is a cofactor where the conclusion is T.

### Left and Right Cofactors

Given an expression E of the form (L R) then...  

- A left cofactor occurs in L, the left-hand side of an expression.  
- A right cofactor occurs in R, the right-hand side of an expression.  



## Inference Rules

SE has the following inference rules...
- ordering
- ground rules
- iteration/deiteration/exchange (the *structural* rules)

Rules should only ever applied to a mostly-canonical or canonical expression.  
The proof procedure minimizes expressions from the bottom up, so enforcing this rule is easy.  

These rules are ordered.  
That is, the rules should always be applied in the specified order in order to ensure that 
an expression is reduced as efficiently as possible.  

To reduce an expression, the proof procedure starts by repeatedly applies the ordering rules 
until there are no more opportunities to reduce the expression.  
At this point the expression is in *ordered normal form*, an expression where all its 
subterms are correctly ordered.  

The SE structural inference rules are based on cofactors...  
- The iteration rule makes clones of terms that are f-grounding cofactors.
- The deiteration rule removes clones of terms that are f-grounding cofactors.
- The exchange rule swaps terms in a context, terms that are f-grounding cofactors.

### SE Inference is Sound and Complete
A proof that the inference rules are sound is included in an appendix.  
There is also a proof in an appendix that SE is equivalent to classic propositional logic, 
therefore we also know that the rules are complete.

### Reductive/Deductive Rules 

The literature often divides inference rules into 'left' and 'right' rules.  
For our purposes it's more convenient to divide the rules into *reductive* and *deductive* rules.

Let E be an expression, and let E' be an expression produced from E in a single step.  

Reductive rules produce an expression where E > E'.  
Reductive rules *decrease* an expressions' entropy.

Deductive rules produce an expression where E < E'.  
Deductive rules *increase* an expressions' entropy.

### Axiom Rule  
The very first, and only the first, expression in a proof is an *axiom*.  
The axiom can be any expression.  

In SE, a proof is usually a *reduction* from a non-canonical expression to a canonical expression.  
But a proof can also be a *derivation* from a canonical expression to a non-canonical expression.  

### Ordering Rules  
These rules order the terms in a context.  
Expression ordering is documented in another section.  

#### Reductive
1. (P Q) => (Q P), when Q < P  

#### Deductive
1. (Q P) => (P Q), when Q < P  ; i suspect this rule is admissible, it definitely seems useless


### Ground Rules
These rules are used to introduce/eliminate empty spaces and empty contexts to/from expressions.  
I suspect, but can't prove, that all these rules are required for completeness.  

#### Reductive
- (F Q) => T 
	> erasure
- (T T) => F 
	> context elimination
	> aka cut elimination in EG.  
- (T (T Q)) => Q 
	> double negation elimination
	> excluded from intuitionistic logic  

#### Deductive
- T => (F Q) 
	> insertion
- F => (T T) 
	> context introduction
	> aka cut introduction in EG.  
- Q => (T (T Q)) 
	> double negation introduction

### Iterative/Deiterative Rules

These rules copy/erase terms to/from sub-contexts.  

#### Deiteration

Deiteration is a reductive rule.

Rule: Given a subterm S of an expression E of the form (L R),  
	where S is an f-grounding f-cofactor of either L or R
	then any or all instances of S in the other side of the expression may be replaced with T.  
	 						
Expressed as rewrite rules...  

1. (L[S] R[S]) => (L[S=>T] R[S]), or  
2. (L[S] R[S]) => (L[S] R[S=>T])

The rules are ordered.  
Since it reduces the left-hand side rather than the right-hand side, rule 1 is preferred over rule 2.  
	
Note that when R[S] == S, or L[S] == S then generalized iteration is similar to classic EG deiteration.  
Note that when R[S] == F, or L[S] == F then generalized iteration is similar to classic EG erasure.  

#### Iteration

Iteration is a deductive rule, it makes copies of terms.

Rule: Given a subterm S of an expression E of the form (L R),  
	where S is a left or right, F-grounding F-cofactor of E then 
	any or all copies of T in the other side of the expression may be replaced with S.  
	 						
Expressed as rewrite rules...  

1. (L[T] R[S]) => (L[T=>S] R[S]), or  
2. (L[S] R[T]) => (L[S] R[T=>S])

Since it reduces the left-hand side rather than the right-hand side,  
rule 1. is guaranteed to reduce an expression more than rule 2.  
Therefore, if an expression can be reduced using either one of these rules then rule 1 is preferred.  
In this sense, the rules are ordered.  
	
Note that when R[S] == S, or L[S] == S then generalized iteration is similar to classic EG iteration.  
Note that when R[S] == F, or L[S] == F then generalized iteration is similar to classic EG insertion  


### Exchange Rules

The SE exchange rule exchanges terms between the sides of a context.  

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

### Join Points

A *join point* is any term in an expression that could be the target of iteration or deiteration.  

A join point is like a placeholder that can be filled with a clone of another term, via iteration.  
The *domain* of any particular join point is the set of unifying terms that may be validly cloned to the join point via iteration.  

A join point can be emptied of any such clone via deiteration.  
An empty join point is an empty space (T).  

Every instance of T in an expression is a join point, 
because every instance of T can be a target of the iteration rule.  

Later in this document a normal form is introduced, 
called the *disjoint* normal form, 
where any and all join points are identified by T.

Join points are important because *unifying* joint points can identify 
equivalent, unified expressions with grounding cofactors that identify opportunities for iteration, deiteration, or exchange.  
*Join point unification* is a sound and complete way to identify any and all opportunities for iteration, deiteration, or exchange.  


#### Identifying Join Points

Every T in a context is a join point, because the T can be replaced by its sibling via iteration.  

Given an expression E of the form [a b].  
Let LC be any f-grounding f-cofactor of a.  
Let RC be any f-grounding f-cofactor of b.  

Any instance l of LC in b is a join point since l can be replaced with T via deiteration.

If RC is not also an f-grounding f-cofactor of a then   
any instance r of RC in a is a join point since r can be replaced with T via deiteration.

There are no other join points other than those that are already represented by a T 
and those that can be replaced by a T via deiteration.  

All of the join points in an expression can be identified in linear time, in two sweeps of the expression, from left to right and back again.  
On each pass, terms are collected, and when a term is encountered 
that matches an existing term in the collection, 
then the encountered term is actually a join point and be replaced with T.  


#### Example
Consider the expression [1 [1 2]].  
The rightmost 1 is aan iteration target of the leftmost 1, therefore [1 [1 2]] is equivalent to [1 [T 2]].  
All the expressions that can be generated via iteration from [1 [T 2]] are...
- [1 [T 2]]
- [1 [1 2]]
- [1 [2 2]]
These are all equivalent expressions but [1 [T 2]] makes it clear where the join point is.  
Cloning the 1 in [1 [T 2]] produces [1 [1 2]], and cloning the 2 produces [1 [2 2]].  
{1,2} is the set of all unifiers for the join point, aka the *domain* of the join point.  
The set of terms that can be constructed from a join point and its unifiers is called the join point's unification, or set of unified terms.  
In the above, the terms [T 2], [1 2], and [2 2] are the join point's unified terms.


## Normal Forms

A normal form is a category of expressions that exhibits a given property.  
For instance, one normal form has already been presented...  
the canonical form, which are expressions with the lowest entropy.

In the SE system the inference rules form a hierarchy of normal forms 
that define categories of expressions with less and less entropy.  
In this section we'll define a normal form for each inference rule.  

It's very useful to present these forms here, 
because it's very difficult to describe the SE proof procedure without referring to them. 

The SE system defines these normal forms...

- Unnormalized
Any given expression.

- Ordered    
An ordered expression is an expression E of the form [l r] where...
- l and r are also ordered expressions 
- l <= r, ie l has the same or less entropy than r.  
> See the Entropy section for the details of how expressions are ordered.

Ordering is admissible/redundant.
That is, expressions don't need to be ordered to fully normalize them,  
but it's much easier for a human to grok expressions when terms are ordered,  
and ordering makes the definition of the inference rules much less tedious.  

This documentation, and the inference rules defined here, assume that expressions are ordered.  

- Compressed
A compressed expression is an ordered expression where all instances of ground rules have been removed.    
Similar to compressing a BDD (Binary Decision Diagram), compression reduces the size of the expression.  
Ground rules *always* reduce the length of an expression, and can potentially reduce an expression to ground in linear time.  
Therefore, ground rules are the fastest way to reduce an expression, 
and every opportunity to apply a ground rule should be taken at every opportunity.  

- Disjoint
A disjoint expression is a compressed expression where any and all join points are identified by T.
In a disjoint expression all the join points in an expression are identified by T, and all T's in an expression represent join points.  
Disjoint expressions have a single instance of any given term in any branch of an expression.  

- Canonical  
Given an expression E, repeatedly make E disjoint and perform exchange until the expression is canonical.  
When no opportunities to apply any inference rules are left the expression must be canonical.

In the next sections it will be shown that...  
given a non-canonical, compressed expression E...  
that an equivalent disjoint expression of E, E', can be created in linear time.  

Discovering reductions to disjoint expressions is done by 
modeling the structural logic in an expression using disjunctive clauses, 
and then computing the transitive closure of the model via resolution.  
After the model is complete, reductions to the disjoint expression 
are easily identified by looking for clauses that represent grounding cofactors.  
Such cofactors identify opportunities for applying iteration, deiteration, and/or exchange.

The process of modeling an expression is called *cofactor modeling*.  
The process of computing the transitive completion of a model called *cofactor identification*.  

## Cofactor Models

A *cofactor model* M is a set of disjunctive clauses that model the relationships between the terms a disjoint expression E such that,  
if there exists an expression E' in the congruence closure of E that has a grounding cofactor S 
then one of the clauses S->E, S->!E, !S->E, or !S->!E is derivable from the model.  

Expressing the problem of finding a reduction to an expression as a cofactor model 
makes implementing a solution to the problem a lot easier.  
It also makes it a lot easier to prove properties about the solution.  

After building a cofactor model M of an expression E, [propositional resolution](https://en.wikipedia.org/wiki/Resolution_(logic)) is used to compute all the cofactors in the congruence closure of E.  

To build a cofactor model for an expression E, unique identifiers must be assigned to all the subterms in E.  
Nothing is relevant about identifiers except that they're unique.  

### Modeling an Expression

A cofactor model is built from the bottom up, 
building a cofactor model from other completed cofactor models.
Expressions are minimized as cofactors are discovered, 
so that only mostly-minimized expressions are ever modeled.  

To build a cofactor model for a disjoint expression E...

1. Unique Ids: Assign unique ids to every join point, and to every other unique subterm of E.  
	> Assign an id to every term and use hash consing to avoid duplication.
2. Model Implications: Add clauses that represent the implications/cofactors between the terms in every implication.
3. Model Join Points: Add clauses that represent all the join points, and thus, 
the congruence closure of the expression.  

### Modeling Implications
Let E be an implication of the form [l r]
subterm 		index	
------------	-----	
l				1       
r				2		
[l r]		 	3		

The cofactor relationships in [l r] can be expressed with these clauses...  

- !1 -> 0; when the term at index 1, l, has the value false then the term at index 0 must have the value true.
	> Equivalent to: !l -> [l r].
	> Expressed as a disjunction: (1 || 0).
- 2 -> 0; when the term at index 2, r, has the value true then the term at index 0 must have the value true.
	> Equivalent to: r -> [l r]
	> Expressed as a disjunction: (!2 || 0).  
- (1 && !2) -> !0; 
	> Equivalent to: (l && !r) -> [T [l r]]
	> Expressed as a disjunction: (!1 || 2 || !0).

A set of clauses that model an implication are called a *context model*.  
The expression ```((1 || 0) && (!2 || 0) && (!1 || 2 || !0))``` is a context model of the expression E = [l r].  

Note that...  
If M is a cofactor model of an expression E, and 0 is E's index in M then...  
- if we want to test E for satisfiability then we make truth the goal by asserting 0 in the model and resolving the remaining clauses.
- if we want to test E for un-satisfiability then we make contradiction the goal by asserting !0 in the model and resolving the remaining clauses.

Note that, once a goal is chosen for a context then the context model reduces to an instance of 2-SAT, clauses with just 2 or fewer variables.  
That is, when 0 is asserted (that is, when we want to prove that E can be true) then the cofactor model collapses to ```0 && (!1 || 2))```.  
Similarly, when !0 is asserted (when we want to prove that E can be false) then the cofactor model collapses to ```!0 && (1 && !2)```.  

### Modeling Join Points

Join points are modeled by extending clauses with additonal literals 
that represent the terms that can be copied to a join point from an iteration source.

#### Example
Consider the expression [a [T b]...  
subterm 		index	
------------	-----	
a				1       
T(2)   			2
b				3		
[T(2) b]		4		
[a [T(2) b]]	5		


First, context models are added for each term.  
The join-point model for the join point at index 2 is equivalent to saying that  
the term with the index 2 is equivalent to one or more of T, b, or a.  
Put another way, if a is true then 

As a clause...
```(2 || !3 || !1)``` 
> Same as 2 == T || 2 == 3 || 2 == 1




### Example
Find the f-grounding f-cofactor in ((1 (T 2)) ((T 1) 2)).  BTW its (1 2).

a horizontal flatterm...
```
term						id
----						--
(T ((1 (T 2)) ((1 T) 2)))   0
T							1
((1 (T 2)) ((1 T) 2))		2
(1 (T 2))					3
1							4
(T 2)						5
((1 T) 2)					6
(1 T)						7
2							8
(1 2)						9
(2 2)						10
(1 1)						11
```

The proof basically goes like this....  
!9 -> 3, !9 -> 6, (!3 || !6 || !2) |- !9 -> !2

Also can show satisfiability....
0 (goal), 2 -> !0 |- !2, 3, 6, -4 (1 == F), 8 (2 == T)

### Resolving Cofactor Models

Theorem: Given a disjoint expression E of the form [L R], E is canonical iff 
there is no unified version of E, E' of the form [L' R'], 
where 
- at least one of L' or R' has an an f-grounding f-cofactor C, and  
- C occurs in the other side of E'

Proof...
By induction of the # of contexts.

The theorem is true for a single context.   
This is shown by enumerating the forms of all possible single context expressions, creating their cofactor models,  
and demonstrating that non-canonical expressions have f-grounding f-cofactors while canonical expression do not.  
> Reminder: Disjoint expressions are not reducible via ground rules, therefore single context expressions composed of just constants may be ignored.  

- [T a]
> term	id
> ----	--
> [T a]	0
> a		1
> T		2
> [a a] 3
> ------- cofactor model --------
> (0 || 1), (0 || 2), (!1 || !2 || !0) ; cofactor model of [T a]
> (!3 || 1), (!1 || 3) ; 1 == !3; cofactor model of [a a]
> (0 || 3), (!3 || !0) ; 0 == 3; iteration of [T a] 
> No f-grounding cofactors of the starting expression may be derived.


- [a a]
> term	id
> ----	--
> [T a]	0
> a		1
> T		2
> [a a] 3
> ------- cofactor model --------
> (0 || 1), (0 || 2), (!1 || !2 || !0) ; cofactor model of [T a]
> (!3 || 1), (!1 || 3) ; 1 == !3; cofactor model of [a a]
> (0 || 3), (!3 || !0) ; 0 == 3; iteration of [T a] 
> Since !1 -> !1 (a is an f-grounding f-cofactor of a), and a occurs in the other side of [a a] then [a a] may be reduced to [T a]



--------------crappola begin--------------------


### Groundable Expressions
A *groundable* expression 
Let E be an axiom.
Begin a proof by applying ground rules until there are no more opportunities to apply any of the ground rules.  
The resulting expression is called a *non-obvious* expression.  

Theorem: *Every instance of T in a non-obvious expression is a wildcard.*  
> Just accept that the T in the expression T is a wildcard, and that there's just nothing to replace it with.  
> Otherwise, every T exists in a context and has a sibling, and this can be the target of iteration, 
> and is therefore a wildcard.  


Now that the inference rules have been introduced we can talk about wildcards and linear normal form.  

These concepts are central to the SE proof procedure, 
and they make it a lot easier to understand how and why the proof procedure works.  



- - (1 (T 2)), (1 (1 2)), and (1 (2 2)), the T marks a wildcard

> Note that the exchange rule is excluded from the definition of a wildcard.  
> That's because the exchange rule can be implemented using iteration and deiteration,  
> thus can be eliminated from the SE system,  
> and therefore we don't have to consider the exchange rule.  

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




idea: create a set of clauses K that enumerate all the structural logic embedded 
in a mostly-canonical expression E of the form (L R).
K will mostly contain Krom formulas but will also have some clauses with three literals.
todo: show that if there is an f-grounding cofactor that is common to both L and R 

The SE proof method transforms expressions from one normal form to another, 
so we need to discuss those normal forms first.

The proof method first reduces an expression to a 

