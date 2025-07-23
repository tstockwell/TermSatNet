# todo

Divide proof into the following sections...
- Introduction to Lucid Expressions
	- LEs are inspired by (Existential Graphs)[https://en.wikipedia.org/wiki/Existential_graph]
	- Unlike EGs, LEs have a textual notation.
	- LEs are basically nand-based propositional expressions, with the constants T and F, numbers for variables, and parentheses instead of operators.   
		- Example: (1 2) is the same as NAND(1,2) and can read as 'either 1 or 2 but not both' 
		- Example: (1 (T 2)) can read as 'if 1 then 2'.
		- Charles Pierce would like how LEs use parentheses instead of operators but would hate the use of numbers for variables. 
	- Its also important to know that, unlike EGs, LEs are *ordered*.
		-  For example F < T < 1 < (T 1) < (1 1) < 2 < (1 2) < (2 1) < (2 2) < (1 (1 2)) < 3
	- LE uses inference rules that are similar to EG, plus an ordering rule, namely...
		- Ordering.  
			- (1 2) => (2 1) when 2 < 1
		- Cut Elimination/Introduction.  
			- (T T) => F
			- F => (T T)
		- Insertion/Erasure
			- Insertion: T => (F 1)
			- Erasure: (F 1) => T
		- Iteration/Deiteration
			- Iteration: (1 2) => (1 2[T<-3]) or (1 2) => (1[T<-3] 2), where 3 is an f-grounding, f-cofactor of 1/2.
			- Deiteration: (1 2) => (1[3<-T] 2), or (1 2) => (1 2[3<-T]), where 3 is an f-grounding, f-cofactor of 2/1.
	- Note that the inference rules come in pairs that are reversible.  
- Introduction to a Knowledge Compilation System that can normalize LEs in polytime. 
	- Compiles an infinite sequence of expressions to an infinite number of rewrite rules that are saved in an infinite database.  
	- The System also includes a routine that can find a rule to apply a given expression in polytime (using a prefix-tree).  
	- Working from the 'bottom' of expression tree to the top, the rewrite rules can normalize any given expression in a provably polynomial number of applications.  
	- Its also important to know that the rewrite rules are globally confluent, therefore the rules can be applied in any order.  
	- Not a practical system, but an abstract system that demonstrates one way to provably normalize expressions in polytime.  
		- It would take a huge amount of time and space to generate the rules for a practical system.
	- Because LEs' inference rules are complete, every rewrite rule in this system can be replaced by a set of inference rule applications, or de-inferences.  
		- The vast majority of expressions are reducible to a simpler expression with just a single deiteration.  
			That is, if you can figure out where to apply the deiteration.
	- The number of de-inferences applied by a rule is proportional to the number of cuts removed from an expression.
		> For example..
			- the rule (1 1) => (T 1) is a single application of deiteration
			- the rule (1 (T 1)) => T is an application of deiteration to get (1 (T T)), 
				followed by two applications of Cut Elimination to get (1 F) and then T.
	- Therefore, expressions can also be normalized in polytime using the inference rules (in reverse) instead of rewrite rules.   
		- However, while the prefix-tree can be used to quickly find a rule to apply to an expression this system would need 
			some kind of index or method for discovering revisible applications of inference rules.  
- Introduction to the LE Compilation System
	- The LE Compilation System uses 'cofactors' to detect opportunities to undo inferences.  
	- A cofactor is an expression produced by replacing a subterm S of an expression E with a constant (T or F).
	- Simple De-Iteration 
		> Cofactors work based on this principle: When assigning a T or F to a term S in one side of E causes that side to 
			to reduce to F then S may be replaced with T in the other side.
			an expressions that doesn't - a constant then any occurence of 
			- Replacing 1 with F in the rhs of (1 1) => (T 1) is a single application of deiteration
			- the rule (1 (T 1)) => T is an application of deiteration to get (1 (T T)), 
				followed by two applications of Cut Elimination to get (1 F) and then T.
	- We can build an equivalent system that replaces the infinite number of rewrite rules with a finite number of decision algorithms.  
	- Cofactors are only computed for mostly-canonical expressions 
		- Mostly-canonical expressions are expressions where both sides are canonical.
	- These decision algorithms are designed to detect and undo applications of LEs inference rules.
	- .
	- Extends the knowledge compilation system by adding decision algorithms that are equivalent to classes of rewrite rules.  
    - The decision algorithms are equivalent to the rewrite rules that they replace in terms of how expressions are transformed.  
	- The number of rewrite rules in the database are dramatically reduced by replacing them with generalized decision algorithms.  
