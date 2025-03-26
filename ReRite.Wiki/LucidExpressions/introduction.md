## Introduction 

### Logic and Reduction Systems

Logic systems can be viewed as a type of abstract reduction system 
where logical formulas are transformed using rewrite rules based on logical equivalences. 

For instance, [Modus Ponens](https://en.wikipedia.org/wiki/Modus_ponens) is famous logical rule of inference, but it can also be viewed as a brain-dead rewrite rule that can perform automated reasoning.  
An example of a modus ponens rewrite rule is;  if you have a list of proven expressions that are known to be true, and that list contains the expressions *of the form* A and A->B, 
then you can add B to the list.  
Using this rule, one does not need to know anything about logic to do logical reasoning, 
reasoning is just a matter of mechanically and repeatedly applying this rule to a set of expressions.  

The Modus ponens rule is all you need to know to do automated reasoning, but [knowing additional rewrite rules can make things easier](https://en.wikipedia.org/wiki/List_of_rules_of_inference).  
For instance, there's Double Negation Elimination, Conjunction and Disjunction Introduction, Absorption, and Resolution.  
All of those well-known logical inference rules are also rewrite rules.  
However, some of these inference rules have properties that other rules do not.  
Confluence for example, not all rules are confluent.  

A good reduction system is sound, complete, confluent, and guaranteed to terminate.  

If your reduction system has rules that are reversible then your system is locally confluent.  
If your systems' rules are also guaranteed to simplify expressions then your system is guaranteed to terminate and is globally confluent.  
Therefore, a reduction system with rules that are sound, complete, reversible, and guaranteed to simplify expressions 
is also globally confluent and guaranteed to terminate.  

LE's, the logic system presented in this document, 
are built with only reversible rules that are complete and guaranteed to reduce expressions, 
and are therefore guaranteed to be confluent and terminate.

### Existential Graphs 

EG is a system of logic for automated reasoning *and* also a reduction system.  
The inference rules for EG are [sound and complete](#Existential_Graphs_of_Peirce), but not confluent.

The FE system's textual notation attempts to mimic a *cut* in an EG.  
The notation (a b) is equivalent to a 'cut' in an EG with two symbols within the cut.  
In the FE system, the constant T represents an empty space, 
so the notation (T a) represents a cut with one symbol in it, 
and a space that can be filled at some time in the future.

The EG system has 5 inference rules; insertion/erasure, double cut elimination, and iteration/de-iteration.  

Insertion/erasure, and iteration/de-iteration are *reversible, symmetric pairs of operations*, double cut elimination is not.  

The fact that the rules are not all reversible is one reason why the EG system is not confluent.  
FE *fixes* confluence by adding a path ordering and making the EG system rules all reversible while preserving completeness.  
