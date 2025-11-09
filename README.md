## UNDER CONSTRUCTION

Until further notice, be advised that this repository is under construction.  
There's a ton of docs to complete, code to complete, etc.  
But I always work from the top down so, when my plans for this project change, 
I change this page before I do anything else.

Eventually you will find a complete SAT solver in the code and docs that show that it minimizes boolean expressions in polytime.  At that time I'll remove this notice.  

## Overview 

I have convinced myself that it's possible to minimize boolean expressions in polynomial time.  
I would like to have my work reviewed but I have no academic credentials nor acquaintances I can press into service.  
I have to start somewhere, so even though it's embarrassing as hell to show people what I do in my free time :-),  
I invite you to take a look at the documentation and code I wrote for it.  

The documentation presents two systems of logic that work by minimizing expressions, 
and shows that proofs in the latter system have a maximum length that is a polynomial function of the length of the axiom.  

## [System C](https://github.com/tstockwell/TermSatNet/wiki/system-c)
System C (for cofactors) is a system of propositional logic,
inspired by [existential graphs](https://en.wikipedia.org/wiki/Existential_graph), 
but unlike existential graphs is designed for machines instead of humans.  

Basic expressions are composed of the constant T, variables, and nand operators.  

C defines a set of expression orderings (aka path orderings) that define what makes one expression simpler than another.

C uses 5 inference rules; commutivity, double negation elimination, erasure, deiteration, and iteration.  

The use of the structural rules, iteration and deiteration, is guided by logical constraints called *cofactors* that must be present in the expression in order to apply the rule.  

Proofs in C are based on proving equivalence, and work by rewriting/reducing expressions to simpler/reduced expressions.  
When constructing a proof the most effort goes into discovering or computing cofactors, 
this is more easily done by machine than by a human.  
Proofs in C are heuristic, requiring inferences to be combined in just the right way to reach a conclusion.  
However, its easier to construct proofs in C because...
- there are far fewer ways to construct proofs, the expression ordering is highly constraining
- and when more than one rule is applicable, it easy to identify applications that move the proof closer to the goal.  

It is shown that the inference rules are sound.  
It is shown that C is complete, by demonstrating an equivalence to classic propositional calculus.  

## [System X](https://github.com/tstockwell/TermSatNet/wiki/system-x)
System X is an extended version of System C.  


X includes all the inference rules from System C and adds the rules of commutivity and exchange.  

X represents expressions as e-graphs that represent the *congruence closure* of an expression.  
E-graphs make it possible to efficiently find cofactors in the *congruence closure* of an expression,  
which is required to find exchanges.    

X includes an algorithm that guides the proof process, thus the proof process is automatic.  

## Proof Complexity in System X
Expressions are reduced from the bottom up,  
and thus only *standard* or *minimal* expressions are ever included in an e-graph.  
A standard expression is a non-minimal expression where every subterm is minimal.  

It is shown that even though exchange is admissible, 
including it makes proofs shorter because a single exchange step represents many iteration/deiteration steps.  

It is shown that there's never a need to use iteration in a proof 
because there's a shorter proof that uses exchange.  

It is shown that the size of e-graphs grows polynomially in relation to the size of the e-graph's root expression.  
In other words, to find the cofactors required to reduce an expression you only need to look at a polynomial number of other expressions.  

It is shown that standard expressions can be reduced in length in no more than three steps.  

Finally, it is shown that any expression can always be fully reduced in a polynomial # of steps.  

## Summary
Adding a path ordering to a logic system makes it possible to create proofs 
where every step in the proof gets you closer to the goal, the minimal form of the expression.  

The exchange rule acts as a shortcut in a proof that would otherwise would require iteration, a step away from the goal.  
But finding exchanges has a cost, it may require looking through the entire confluence closure of an expression for the right cofactors.  
By using e-graphs, exchanges can be found efficiently.  

Finally, proofs in System X use all forms of reasoning (deductive, inductive, and abductive), and I don't think that's a coincidence.  

