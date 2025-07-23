## Lucid Expressions  

The LE system is a logic and reduction system that's inspired by existential graphs.  
Unlike the EG system, which is designed to be a handy reasoning system for humans, LE is designed for computers.  
That is, LEs' proof method is fairly simple but extremely tedious and definitely not convenient for humans.  

One thing LE does is make expressions easier to understand by reducing the number of ways to express the same thing.  
So LE is *ordered*, so that to the LE system (a b) < (b a).   

Another thing about LE is that its' inference rules are designed to be symmetric.  
That is, if expression Q can be derived from expression P in one step 
then P can be derived from Q in one step.  

*Reducing an expression* means repeatedly applying reduction rules, or rules of inference, 
to an expression until no more rules may be applied, 
at which point the expression is canonical.  
LEs are reducible because they are *ordered* and because the LE system's inference rules are *symmetric* and *reversible*.  

LE is a logic system where reduction is proof that two expressions are equal.

### Syntax

The LE syntax is an extension of the [OpenEG](the-EG-system.md) syntax.  

The basic syntax is simple...
- The symbol T is an expression that represents an empty space, that can possibly be filled in later.  
	> Semantically, T means "true", ie T always has the truth value 1.  
- The symbol F is an expression that represents an empty cut.  
	> Semantically, F means "false", ie F always has the truth value 0.  
- Variables use the [Base 32 Encoding with Extended Hex Alphabet](https://en.wikipedia.org/wiki/Base32#Base_32_Encoding_with_Extended_Hex_Alphabet_per_%C2%A77).  	
	> Variable names are more constrained than in [OEG](existential-graphs.md).  
	> Variables are numbers and have the same ordering.  
	> Conventions; always use lower case instead of upper, embedded hyphens are translated to 0.  
	> Examples: one, 2, tvc15 (because Hex32 uses letters up th 'V'), cat-dog  
- Two expressions may be bounded by parentheses (NAND operations) and separated by a space  
Examples; (T a), (a b), and (a (T (b a)))

### Conversions From Propositional Calculus to Lucid expressions...
Conversions From boolean expressions to Lucid expressions...

    - NAND: a ~& b   => (A b)
    - NOT:  ~a       => (T a)
    - AND:  a && b   => (T (a b))
    - IMPL: a -> b   => (a (T b))
    - OR:   a || b   => ((T a) (T b))

### Variable Ordering

Unlike EG, in LE variables are numbers, and ordered.  
Examples: 0 < 9 < bee < adder < supercalifragilisticekspialidocious 

### Flatterm

A flatterm is an array of an expressions' subterms, enumerated in a depth-first fashion, starting with the expression itself.  
This structure is an extremely useful way to represent expressions.  

#### Example
(a (b (b c)))

Flatterm of (a (b (b c)))...  

subterm 		index	level
------------	-----	-------
(a (b (a c))) 	0		0
a (leftmost)	1       1
(b (a c))		2		1
b (leftmost)	3		2
(b c)			4		2
b				5		3
c				6		3

Length = 7 = Sizeof(Flatterm((a (b (b c)))))

Length	= (literal count * 2) - 1  
		= (operator count * 2) + 1  
		= flatterm.Count()

#### Length
The *length* of an LE expression is defined as the length of the expressions' flatterm.  
Note: Expressions always have an odd length.  

#### Level 
Level is a property of an expressions' subgraphs.  
Simply, a subgraphs' level is the number of parens that you need to cross to get to that subgraph.  
	
### Ordering

Unlike EGs, LEs are ordered.  

[Ordering expressions](https://en.wikipedia.org/wiki/Path_ordering_(term_rewriting)) makes it possible to...
- guarantee that rules only simplify expressions, and 
- guarantee that reduction terminates.  

The first rule of ordering is, expressions with less variables come before expression with more variables.
The ordering enumerates all expressions of 0 variables (the constants),  
then all expressions with just the variable '0',  
then all expressions with the variable '0' and the variable '1',  
then all expressions with the variables '0', '1', and '2', etc...  
This ordering is called the variable hierarchy.  

Within each variable-count ordering expressions are ordered by...  
- the length of their flatterm (in other words, shorter expressions come first), 
- and then by their left-hand sides (when expressions have the same length then expression with simpler left-hand sides come first).

#### Common Definitions 

The section defines common functions over expressions.  
These function are often referenced in the LE documentation.  

### Cofactors  (E,S,R,C)

Cofactors are *very* important in LE.  
Cofactors guide both the deduction and reduction methods in the LE system.

A **cofactor** is a tuple (E,S,R,C) such that E[S=>R] |- C, where...  
- E is an expression of the form (L R),
- S is a subterm in E
- R (aka replacement) is an expression, usually a constant
- C (aka conclusion) is the fully reduced, canonical, ground version of E[S=>R].  

A cofactor can be represented by a boolean expression: (S == R) -> (E == C)

A subterm S of an expression E is called a cofactor of E if there exists a cofactor (E,S,R,C) for some value of Q,V, and R.


#### F and T Cofactors

An *F-cofactor* is a cofactor where R == F.
Put another way, an F-cofactor is cofactor produced by replacing all instances of a subterm with F.

A *T-cofactor* is a cofactor where R == T.
Put another way, an T-cofactor is cofactor produced by replacing all instances of a subterm with T.

#### Grounding Cofactors

A *grounding* cofactor is a cofactor where C is a constant.

Put another way, a grounding cofactor is a subterm that can *force* an expression to a ground value (T or F).  

An *F-grounding* cofactor is a grounding cofactor where C == F.
Put another way, an F-grounding cofactor is a cofactor than can force an expression to false (F).

A *T-grounding* cofactor is a grounding cofactor where C == T.
Put another way, an T-grounding cofactor is cofactor than can force an expression to true (T).

#### Left and Right Cofactors

Given an expression E of the form (A B) then...  

- A left cofactor occurs in A, the left-hand side of an expression.  
- A right cofactor occurs in B, the right-hand side of an expression.  


### Inference Rules

LE has the following inference/rewrite rules...
- ordering
- cut introduction/elimination
- iteration/deiteration
- insertion/erasure

The ordering and cut rules are straight-forward.  
The remaining rules are more elaborate than the inference rules used by other logic systems.  
LE's 

The inference rules for LEs are sound, complete, and reversible.  

#### Order
These rules rearrange terms in an expression to respect LE expression ordering.  
These rules are bidirectional.

0. (P Q) <=> (T Q), if Compare(P,Q) == 0
1. (P Q) <=> (Q P), if Compare(P,Q) > 0

#### Cut Elimination/Introduction
Eliminates/Introduces a pair of parentheses.

1. (T T) <=> F		
1. F => (T T) 

#### Erasure/Insertion
Erases/Inserts a subterm into an expression

1. (F Q) <=> T		
2. T => (F Q) 

#### Double Negation Elimination/Introduction
Removes/Introduces double negation

1. (T (T Q)) <=> Q	
2. Q => (T (T Q)) 

#### Generalized Iteration

The LE iteration rule is a generalization of EG iteration and insertion.

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

#### Generalized Deiteration

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

