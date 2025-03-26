## Lucid Expressions  

LE is a system of logic, and a reduction system, based on existential graphs.  
Unlike the EG system, which prioritize making graphs easy for humans to understand,  
LE prioritizes making it easy for computers to understand expressions by having just one  
*canonical* way to express any particular boolean function, 
and by making it easy to reduce any given expression to its canonical form.  


*Reducing an expression* means repeatedly applying reduction rules to an expression until no more rules may be applied, at which point the expression is canonical.  

LEs are reducable because they are *ordered* and because the LE system's inference rules are *reversible*.  

### Syntax

The basic syntax is simple...
- The symbol T is an expression that represents an empty space, that can possibly be filled in later.
- The symbol F is an expression that represents an empty cut.
- Variables are expressions represented by hexadecimal numbers greater than 0, but always using lower case letters.
- Two expressions may be bounded by parentheses (NAND operations) and separated by a space  
Examples; (T a), (a b), and (a (T (b a)))

LE uses parentheses to represent a cut in an EG.  
The parentheses visually group elements together in a way that's similar to existential graphs.  

Peirce's EG system treats cuts as NOT operators, and all the items within a cut as a conjunction.  
So EG interprets (a b) as NOT(AND(a,b)) because this makes things easier for hummans to understand.  
However, LE interprets (a b) as NAND(a,b) because this makes things easier for computers to understand.  
In the LE system the NOT(a) relation is expressed as (T a).  

### Level 
Level is a property of an expressions' subgraphs.  
Simply, a subgraphs' level is the number of parens that you need to cross to get to that subgraph.

Example: (a (b (b c)))
subgraph		level
------------	-------
(a (b (a c))) 	0
a (leftmost)	1
(b (a c))		1
b (leftmost)    2
(b c)			2
b				3
c				3

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
These rules rearrange terms in an expression to respect LE path ordering.  
These rules are bidirectional.

0. (P Q) <=> (T Q), if P == Q
1. (P Q) <=> (Q P), if Q < P
2. (Q P) <=> (P Q), if P < Q

#### Constant Introduction/Elimination
These rules introduce or eliminate constants from expressions.  
These rules are bidirectional.

1. (T T) <=> F
2. (Q F) <=> T
3. (F Q) <=> T
4. (T (T Q)) <=> Q

Note that these rules include the double-cut introduction/elimination rule from classic EGs.  

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


#### Generalized Insertion

Generalized insertion is like a special case of generalized iteration where one side of an expression E is F.  
In this special case, *any* expression can be iterated into the other side of E.  

Rule: Given a subterm S and an expression E of the form (L R),  
	where either L or R is F then 
	all copies of T in the other side of the expression may be replaced with S.  
	 						
Expressed as rewrite rules...  
	- (L[T] F) => (L[T=>S] F), or  
	- (F R[T]) => (F R[T=>S])
	
Note that when L == T (or R == T), then generalized insertion is equivalent to classic insertion.  

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

## Conversions From Propositional Calculus to Lucid expressions...

    - NAND  :A ~& B   => (A B)
    - NOT   :~A       => (T A)
    - AND   :A && B   => (T(A B))
    - IMPL  :A -> B   => (A (T B))
    - OR    :A || B   => ((T A) (T B))
