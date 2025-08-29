# Introduction 

## Logic Systems and Abstract Reduction Systems

Logic systems can be understood as abstract reduction systems, here's why...

- An abstract reduction system is fundamentally a set of objects and a binary relation (the "reduction relation" or "rewrite relation") 
	that dictates how one object can be transformed into another.  

- In logic systems, the "objects" are logical expressions (e.g., formulas, propositions), 
	and the "reduction relation" corresponds to the inference rules or axioms that allow you 
	to derive new logical expressions from existing ones. 

Example...  
Consider a proof system for propositional logic.  
The objects could be propositional formulas like "P ? Q" or "¬R".  
The reduction relation could be the inference rules, such as modus ponens (if you have "A" and "A ? B", you can reduce it to "B").  
In this context, applying an inference rule to a set of premises is essentially "reducing" those premises to a conclusion. 

This perspective allows us to analyze the properties of logic systems using the mathematical tools developed for abstract reduction systems, 
such as studying concepts like...
- Normal forms: the simplest form an expression can be reduced to.
- Termination: whether every reduction sequence eventually leads to a normal form.
- Confluence: if multiple reduction paths from a starting expression lead to the same final result. 

A good logic system, like a good reduction system, is sound, complete, confluent, and guaranteed to terminate.  

Lucid Expressions is a logic system with an infinite set of rules that are guaranteed to reduce 
expressions to their normal form

If your logic system has rules that are reversible then your system will be locally confluent.  
If your systems' rules are also guaranteed to simplify expressions to their normal form 
then your system is also globally confluent and guaranteed to terminate.  
Lucid Expressions is a logic system with an infinite set of rules that are sound, complete, 
and guaranteed to reduce expressions to their normal form.   
Therefore the LE system is also globally confluent and guaranteed to terminate.  

We know that the rules of the LE system are complete automatically generated using the Knuth-Bendix completion 

LE's, the logic system presented in this document, 
are built with only reversible rules that are complete and guaranteed to reduce expressions, 
and are therefore guaranteed to be confluent and terminate.

