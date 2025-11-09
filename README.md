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

C defines a set of expression orderings (aka [path orderings](https://en.wikipedia.org/wiki/Path_ordering_(term_rewriting))) that define what makes one expression simpler than another.

C uses the inference rules from existemtial graphs; [double negation elimination, erasure, deiteration, and iteration](https://en.wikipedia.org/wiki/Existential_graph#Alpha), and [commutivity](https://www.philosophypages.com/lg/e11b.htm#:~:text=Commutation,any%20of%20the%20possible%20conditions.).  

The use of the structural rules, iteration and deiteration, is guided by logical constraints called *cofactors* that must be present in the expression in order to apply the rule.  
> A cofactor is a subterm of an expression that entails the expression.  
> That is, replacing the subterm with a constant creates an expresssion that is equivalent to a constant.
The use of the commutivity rules is constrained by the expression ordering.  

Proofs in C are based on proving equivalence, and work by rewriting/reducing expressions to thier simplest form.  
Proofs in C are heuristic, requiring inferences to be combined in just the right way to reach a conclusion.  

When constructing a proof the most effort goes into discovering or computing cofactors, 
which is more easily done by machine than by a human.  
It's also easier for machines to recognize the order of expressions.  
Thus, when more than one rule is applicable,  
it's easier for machines to identify choices that actually make an expression simpler.  

It is shown that the inference rules are sound.  
It is shown that C is complete, by demonstrating an equivalence to classic propositional calculus.  

## [System X](https://github.com/tstockwell/TermSatNet/wiki/system-x)
System X is an extended version of System C that uses the structural rule of exchange to make proofs even shorter.  

X represents expressions as saturated [e-graphs](https://en.wikipedia.org/wiki/E-graph).  
X uses equality saturation to build an e-graph from a *root expression*.  
E-graphs in X represent all the ways an expression can be written using the exact same number of variables, called the *congruence closure*.  

X includes all the inference rules from System C and adds the structural rule of exchange.  
Exchange is also guided by cofactors constraints, like iteration and deiteration, but 
exchange looks for cofactors in an expressions congruence closure, not just an expression.  
E-graphs make it possible to efficiently find cofactors in the congruence closure of an expression.    

X includes an algorithm that guides the proof process, thus the proof process is automatic.  

## [Proof Complexity in System X](https://github.com/tstockwell/TermSatNet/wiki/complexity)
Expressions are reduced from the bottom up,  
and thus only *standard* or *minimal* expressions are ever included in an e-graph.  
A standard expression is a non-minimal expression where every subterm is minimal.  

It is shown that even though exchange is admissible, 
including it makes proofs shorter because a single exchange step represents many iteration/deiteration steps.  

It is shown that there's never a need to use iteration in a proof 
because there's a shorter proof that uses exchange.  
Note... that's not the same as saying that iteration is admissible, 
iterations are used to build e-graphs but not used in proofs.

It is shown that the number of expressions in a saturated e-graph is limited to a quadradic function of the size of the root expression.  
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

