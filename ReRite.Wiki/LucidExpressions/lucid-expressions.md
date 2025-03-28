## Lucid Expressions  

The LE system is a reduction system and a logic system, based on existential graphs.  
Unlike the EG system, which is designed to be a handy reasoning system for humans, LE is designed for computers.  
Specifically, LE makes expressions easier to understand by reducing the number of ways to express the same thing.  

*Reducing an expression* means repeatedly applying reduction rules to an expression until no more rules may be applied, at which point the expression is canonical.  

LEs are reducible because they are *ordered* and because the LE system's inference rules are *reversible*.  

### Syntax

The basic syntax is simple...
- The symbol T is an expression that represents an empty space, that can possibly be filled in later.
	> Semantically, T means "true", ie T always has the truth value 1.  
- The symbol F is an expression that represents an empty cut.
	> Semantically, F means "false", ie F always has the truth value 0.  
- Variables are expressions represented by hexadecimal numbers, but always using lower case letters.
	> By convention, hyphens in a variable name are converted to 0's.
- Two expressions may be bounded by parentheses (NAND operations) and separated by a space  
Examples; (T a), (a b), and (a (T (b a)))

LE uses parentheses to represent a cut in an EG.  
The parentheses visually group elements together in a way that's similar to existential graphs.  

Peirce's EG system treats cuts as NOT operators, and all the items within a cut as a conjunction.  
So EG interprets (a b) as NOT(AND(a,b)) because this makes things easier for humans to understand.  
However, LE interprets (a b) as NAND(a,b) because this makes things easier for computers to understand.  
In the LE system the NOT(a) relation is expressed as (T a).  

### Flatterm

A flatterm is an array of an expressions' subterms, enumerated in a depth-first fashion, starting with the expression itself.  
This structure is an extremely useful way to represent expressions.  

#### Length
The *length* of an LE expression is defined as the length of the expressions' flatterm.  
Note: Expressions always have an odd length.  

#### Level 
Level is a property of an expressions' subgraphs.  
Simply, a subgraphs' level is the number of parens that you need to cross to get to that subgraph.  

#### Example
(a (b (b c)))

Flatterm...  
subterm 		level
------------	-------
(a (b (a c))) 	0
a (leftmost)	1
(b (a c))		1
b (leftmost)    2
(b c)			2
b				3
c				3

Length	= (literal count * 2) - 1  
		= (operator count * 2) + 1  
		= flatterm.Count()

### Reduction is Proof

The LE logic system has a single axiom: T.  

In LE, proofs are reversible.  
You can prove an expression S is true by...
- starting with T and deriving S from it, or 
- by reducing S to T.  

In other words, reducing an expression to another expression is proof that the expressions are equivalent.  
And reducing S to T is proof that S is a tautology, or 'true'.  

Reducing an expression to something other than F is proof that the expression is satisfiable.


### Inference Rules

LE has eight rewrite rules...
- two rules for insertion/erasure.

	> Insertion: insert ANY expression as a subgraph at any odd level 
	> Erasure: erase any subgraph at an even level

- two rules for double-cut introduction/reduction.

	> These rules introduce and remove double-cuts.  
	> Examples; (T (T a)) => a, and a => (T (T a))

- four rules for iteration/deiteration.

	> These rules introduce and remove terms.  
	> Examples; (a (T b)) => (a (a b)), and (a (a b)) => (a (T b))

The LE system creates a completely reversible set of inference rules from the EG system's rules by...
- dropping the EG insertion and erasure rules, 
- adding a double-cut *introduction* rule to make the double-cut elimination rule reversible.
- generalizing the iteration and deiteration rules using cofactors.  
- dropping AND semantics and adopting NOR semantics in order to preserve completeness.

The inference rules for LEs are sound, complete, and reversible.  

#### Order Reduction
These rules rearrange terms in an expression to respect LE expression ordering.  
These rules are bidirectional.

0. (P Q) <=> (T Q), if Compare(P,Q) == 0
1. (P Q) <=> (Q P), if Compare(P,Q) > 0
2. (Q P) <=> (P Q), if Compare(P,Q) < 0

#### Cut Introduction/Elimination
These rules introduce or eliminate cuts from expressions.  
In LE, cuts have two syntactic forms: parentheses that bind subgraphs ```(P Q)```, and the constant ```F```.  
F is syntactic sugar for an empty cut ().  
F is the only way to represent an empty cut.  

These rules introduce/elimination empty-cuts and double-cuts to/from expressions.  
These rules are bidirectional.

1. (T T) <=> F		; empty-space elimination/introduction
2. (F Q) <=> T		; empty-cut elimination/introduction
3. (T (T Q)) <=> Q	; double-negation elimination/introduction

4. (F Q) <=> Q		; top-level empty-cut elimination/introduction 
	> For top-level graphs only.  
	> Valid because when Q is top-level then Q is asserted to be valid, meaning Q == T. 
	> Really only useful in top-level proofs
	> todo: make this a derived rule somewhere

Note that these rules include the double-cut introduction/elimination rule from classic EGs.  

#### Insertion

Insertion is a special case of the Cut-Elimination rules where, instead of removing cuts that contain empty cuts, 
a random expression can be *injected* into the expression opposite the empty cut.  
For example, instead of reducing an expression using the cut elimination rule (F T) => T, 
one could instead expand the expression using the insertion rule (F T) => (F S).  
Either way, the F ensures that the expression will always be true.  
Cut-Introduction is normally used to build the *frame* of an expression, with empty spaces (Ts).  
And then Insertion is used to populate the empty spaces (Ts) in such a frame.

Insertion is also like a special case of generalized iteration where one side of an expression E is F.  
In this special case, *any* expression can be iterated into the other side of E.  
When one side of an expression is F then the expression will always evaluate to true,  
so you can change the other side however you want. 

Expressed as a rewrite rule...  
	- (F R[T]) => (F R[T=>S])
	
#### Erasure

Erasure is a special case of the Cut-Elimination rules where, instead of removing cuts that contain empty cuts, 
any expression P opposite the empty cut is reduced instead, by removing all instances of any term S that occurs in P.

Erasure is like a special case of generalized deiteration where one side of an expression E is F.  
When one side of an expression is F then the expression will always evaluate to true,  
so you can change the other side however you want. 

Expressed as rewrite rules...  
	- (L[S] F) => (L[S=>T] F), or  
	- (F R[S]) => (F R[S=>T])

#### Generalized Iteration

Rule: Given a subterm S of an expression E of the form (L R),  
	where S is a left or right, F-grounding F-cofactor of E then 
	all copies of T in the other side of the expression may be replaced with S.  
	 						
Expressed as rewrite rules...  
	- (L[T] R[S]) => (L[T=>S] R[S]), or  
	- (L[S] R[T]) => (L[S] R[T=>S])
	
Note that when S == L, or S == R, then generalized iteration is equivalent to classic iteration.  

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

#### Theorem: Insertion/Erasure rules are a special cases of Cut-Elimination rules
Theorem: The Insertion/Erasure Rules are special cases of the Cut-Elimination rules
	
### Ordering

Unlike EGs, LEs are ordered.  

Defining a [path ordering](https://en.wikipedia.org/wiki/Path_ordering_(term_rewriting)) makes it possible to...
- guarantee that rules only simplify expressions, and 
- guarantee that reduction terminates.  

The ordering enumerates all expressions of 0 variables (the constants),  
then all expressions with just the variable 1,  
then all expressions with the variable 1 and the variable 2,  
then all expressions with the variables 1 and 2 and the variable 3, etc...  
Within each of those orders expressions are ordered by...  
- the length of their flatterm (in other words, shorter expressions come first), 
- and then by their left-hand sides (when expressions have the same length then expression with simple left-hand sides come first).


#### Common Definitions 

It's the ME convention to use capital letters, other than T or F, to represent expressions.  
Let Vars(F) be a function that returns an ordered list of all the unique variables in a given formula.  
Let HighVar(F) be a function that returns the highest numbered variable in a formula.  
Let Flatterm(F) be a function that enumerates the terms in an express in depth-first fashion.  
> Example: (a (T (2 1))) has 3*2+1=7 terms... { (a (T (2 1))), a, (T (2 1)), T, (2 1), 2, 
1 }.  

Let Length(F) => Flatterm(F).Count()

#### Compare Function

Let Compare(A,B) be a function that returns {-1, 0, 1} if {A < B, A == B, or A < B}.  
Here's a  specification for Compare...

1. Variable Ordering   
	if HighVar(A) < HighVar(B) then A comes before B and Compare(A,B) returns -1.  
	if HighVar(A) > HighVar(B) then B comes before A and Compare(A,B) returns 1.
	
2. Term Count  
	Expressions with fewer terms are simpler than formulas with more terms.  
	if Flatterm(A).Count < Flatterm(B).Count then A comes before B and Compare(A,B) returns -1.  
	if Flatterm(B).Count < Flatterm(A).Count then B comes before A and Compare(A,B) returns 1.  
	Note that the concept of term count defines an expressions *length*.
	
3. 	LHS <= RHS  
	Expressions that are the same length and have the same HighVar are ordered by the complexity of their LHS.  
	That is, expressions with less complex LHS come before those with more complex LHS.  
	Expressions that are the same length and same variable order and have equal LHS are ordered by the complexity of their RHS.  

4. F is before T

Examples..
Rule 1: T and F come before any other expressions
Rule 1: 1 comes before 2, comes before (1 2), comes before 3, comes before a
Rule 2: (a a) comes before (a (a a))
Rule 3: F before T, (1 (T 1)) comes before (1 (1 1)), comes before ((T 1) 1)
Rule 4: F comes before T

### Conversions From Propositional Calculus to Lucid expressions...
Conversions From boolean expressions to Lucid expressions...

    - NAND  :A ~& B   => (A B)
    - NOT   :~A       => (T A)
    - AND   :A && B   => (T (A B))
    - IMPL  :A -> B   => (A (T B))
    - OR    :A || B   => ((T A) (T B))


### Cofactors  
A cofactor is a tuple (Q,S,V,R) such that Q[S=>V] |- R, where...  
- Q is an expression of the form (L R),
- S is a subterm in Q
- V (aka test value) is an expression, usually a constant
- R is the fully reduced, canonical, ground version of Q[S=>V].  

A subterm S of an expression E may be said to be a cofactor of E if there exists a cofactor (Q,S,V,R) for some value of Q,V, and R.

An *F-cofactor* is a cofactor where V== F.
A *T-cofactor* is a cofactor where V == T.
A *grounding* cofactor is a cofactor where R is a constant.
An *F-grounding* cofactor is a grounding cofactor where R == F.
A *T-grounding* cofactor is a grounding cofactor where R == T.

A left cofactor occurs in the left-hand side of an expression.  
A right cofactor occurs in the right-hand side of an expression.  

