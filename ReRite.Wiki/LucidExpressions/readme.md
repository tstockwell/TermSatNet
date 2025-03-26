# Lucid Expressions: Boolean Expressions That Reduce to Canonical Form in Polynomial Time

Lucid Expressions are a form of textual, binary, ordered, [existential graph](https://en.wikipedia.org/wiki/Existential_graph),  
with symmetric rules of inference, 
that can be reduced to their canonical form in polynomial time.  

This document describes Lucid Expressions and a method for reducing them to their simplest, canonical form.  
It is shown that the LE system's reduction method is sound, complete, confluent, and terminates.  
It is also shown that LEs can be completely reduced, and their satisfiability determined, in polynomial time.  

## Cheet Sheet

This section is an overview of, and guide to, the rest of this document.

- [Introduction](introduction.md)

	 This section gives an overview of [existential graphs](https://en.wikipedia.org/wiki/Existential_graph) (EG), [abstract reduction systems](https://en.wikipedia.org/wiki/Abstract_rewriting_system), and the shortcomings of the EG system as a reduction system.  
	
	- [Logic and Reduction Systems](introduction.md###Logic_and_Reduction_Systems)
		- Logic systems can be viewed as a type of abstract reduction system.  
			> A reduction system where boolean expressions are transformed using rewrite rules based on logical inference.  
		- A good reduction system is sound, complete, terminates, and is confluent.  
		- A good logic system is sound, complete, and... *terminates, and is confluent?*  
		- Reduction systems with rules that are reversible are locally confluent.  
		- Rules that are guaranteed to simplify ordered expressions are also globally confluent and guaranteed to terminate.  

		> Therefore, an ordered reduction system with reduction rules that are sound, complete, and reversible is also confluent and guaranteed to terminate.  
		LE is a reduction system designed with rewrite rules that implement EG-like rules of inference. 

	- [Existential Graphs](introduction.md###_Existential_Graphs)
	
		- EG is a system of logic for automated reasoning *and* also a reduction system.  
		- EG is sound and complete, but not confluent.  
		- EG has 5 inference rules; insertion/erasure, double cut elimination, and iteration/de-iteration.  

			> Insertion/erasure, and iteration/de-iteration are *reversible, symmetric pairs of operations*.  
			Double-cut elimination is not.  

		- The fact that the rules are not all reversible destroys confluence in EGs.  

- [Lucid Expressions](lucid-expressions.md)  
	This section explains LEs by way of describing the differences between LEs and EGs.  

	LE is a system of logic similar to existential graphs.  
	Unlike the EG system, which prioritize the readability of graphs, 
	the LE system is also designed to be a good reduction system.

	- [Notation/Syntax](lucid-expressions.md###_Syntax):  
		LEs uses a linear, textual notation, as opposed to EGs.  
		- LEs use the constants T and F to represent an empty 'sheet of assertion' and an empty cut.
		- Variable are lower-case hexadecimal numbers 
		- Expressions may be wrapped in parentheses and separated by a space: 
			> Examples: (a b), (T (a b))  
	        The parentheses are supposed to look like the edges of a cut in an EG.

			> EG interprets (a b) as NOT(AND(a,b)) because this makes things easier for humans.  
			> LE interprets (a b) as NAND(a,b) because doing so instantly reduces the number of operators in expressions while preserving functional comnpleteness.  


	- [Inference Rules](lucid-expressions.md###_Inference_Rules)
		- Insertion/Erasure: Insert and erase double cuts anywhere in an expression.

			> Put another way, these rewrite rules are valid for any subterm S in an expression E...  
			> E[S] => E[S->(T (T S))],  
			> E[(T (T S))] => E[(T (T S))->S

		- Iteration/Deiteration: Replicate/remove copies of a subterm to/from a sibling. 
			> E[(L X(T))] => E[(L X(T->L))],  
			> E[(L X(L))] => E[(L X(L->T))],  
			> E[(X(T) R)] => E[(X(T->R) R)],  
			> E[(X(R) R)] => E[(X(R->T) R)] 

		> These six rewrite rules are sound, complete, and reversible(symmetric).  

	- [LEs are ordered](lucid-expressions.md###_Ordering)  

		> For example, (T (a (b c)) is a simpler expression than (((c b) a) T).  

		> Defining a [rewrite order](https://en.wikipedia.org/wiki/Rewrite_order)) makes it possible to guarantee that rules only simplify expressions, and guarantees that reduction terminates.  


	
- [Reduction Algorithm](implementation.md)

	This section provides pseudo code for the reduction algorithm and some examples.


- [Conclusion](conclusion.md)

	It's been shown that LEs  provide a tractable form of automated reasoning.  

- [Appendix: Completeness](appendix-completness.md)

	This section proves that the ME rules of inference are complete.  

- [Appendix: Computational Complexity](appendix-complexity.md)

	This section proves that the computational complexity of reduction is O(n4) in the very worst case.


## References

### Ref1
[Native diagrammatic soundness and completeness proofs for Peirces Existential Graphs (Alpha); Caterina; Gangle; Tohme](https://philsci-archive.pitt.edu/21196/1/NativeAlphaFinal.pdf)

### Dau
[Mathematical Logic with Diagrams, Based on the Existential Graphs of Peirce;Dau](http://www.dr-dau.net/Papers/habil.pdf).

Includes proofs of soundness and completeness for EGs

### Linear Cofactors
[Linear Cofactor Relationships in Boolean Functions; Zhang, Chrzanowska-Jeske, Mishchenko, Burch](https://people.eecs.berkeley.edu/~alanmi/publications/2005/tcad05_lcr.pdf)

### Rewriting Logic 
[Rewriting Logic as a Logical and Semantic Framework; Mart�-Oliet, LEseguer](https://www.sciencedirect.com/science/article/pii/S1571066104000404)

### Cut Elimination
[Confluence as a cut elimination property; Dowek](https://inria.hal.science/hal-04103228v1/document)

I'm not the first person to notice that cut elimination is a problem.  

### Orderings
[Orderings for term-rewriting systems; Dershowitz](https://www.computer.org/csdl/proceedings-article/focs/1979/542800123/12OmNqBbI2S)

### Existential Graphs of Peirce
[Mathematical Logic with Diagrams, Based on the Existential Graphs of Peirce;Dau](http://www.dr-dau.net/Papers/habil.pdf).
