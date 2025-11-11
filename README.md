## UNDER CONSTRUCTION

Until further notice, be advised that this repository is under construction.  
I always work from the top down so, when my plans for this project change, 
I change this page before I do anything else.  
When the code and docs are complete I'll remove this notice.  

## Overview 

I have been experimenting with rewrite systems as part of an effort to build a template processor.  
System X is a rewrite system that reduces boolean expressions.
I have convinced myself that this system can minimize boolean expressions in polynomial time.  
I am currently rewriting documentation and building a SAT solver based on this system.  
I've made this repository public because I hope to connect with folks that can provide feedback.  

What follows is an overview of the documentation so far...
- Some background on deriving inference rules is presented.
- Then two systems of logic are presented.
- Finally, a section that proves various theorems of the latter system.  

## Syntax-Guided Rule Synthesis

Until recently I didn't know that syntax-guided rule synthesis was a thing.  
But essentially that's how the rules for systems presented here were developed.

In other words, the inference rules for System X were not invented by, nor designed by me.  
The rules were derived.  The rules are derivable from an enumeration of expressions in the path order.  
The thing is though, the number of rules grow exponentially as the number of variables increase, 
and a complete set is infinite.  
So the rules in thier explicit form are not very useful.  
However, while exploring the derived rules I discovered that they had patterns, 
and eventually I was able to describe the entire, infinite set of exponentially growing rules 
as the inference rules in System X.  

One reason I am feeling confident in the work I've done here is precisely because it's not something that I invented, it's a description of something that I computed.  
*Explaining* what you have computed is a whole other problem though :-).  
Also, I had to come up with a way to prove properties about the generated rules.  
That's what this documentation is about.

I'm obsessed with rewriting because programming with rules is *way way way* more modular, reusable, customizable, scalable, and extensible than the languages and tools we have now.  

## [System C](https://github.com/tstockwell/TermSatNet/wiki/system-c)

System C (for cofactors) is a system of propositional logic,
similar to [Existential Graphs](https://en.wikipedia.org/wiki/Existential_graph) and the [Laws Of Form](https://en.wikipedia.org/wiki/Laws_of_Form).  
> But it's designed for machines instead of humans, and it's designed to produce shorter proofs.  

Basic expressions are composed of the constant T, variables, and nand operators.  

C defines a set of expression orderings (aka [path orderings](https://en.wikipedia.org/wiki/Path_ordering_(term_rewriting))) that define what makes one expression simpler than another.

C uses the inference rules from existential graphs; [double negation elimination, erasure, deiteration, and iteration](https://en.wikipedia.org/wiki/Existential_graph#Alpha), and adds [commutivity](https://www.philosophypages.com/lg/e11b.htm#:~:text=Commutation,any%20of%20the%20possible%20conditions.).  

The application of the structural rules, iteration and deiteration, is guided by logical constraints called *cofactors* that must be present in the expression in order to apply the rule.  
> A cofactor is a subterm of an expression that [entails](https://en.wikipedia.org/wiki/Logical_consequence) the expression.  
> In X, a cofactor is a subterm that, when replaced with constant, creates an expresssion that is equivalent to a constant.  

The application of the commutivity rules is constrained by the path ordering.  

Proofs in C are based on proving equivalence, and work by rewriting/reducing expressions to thier simplest form.  
Proofs in C are heuristic, requiring inferences to be combined in just the right way to reach a conclusion.  
The path ordering helps makes proofs shorter because, when more than one rule may be applied, 
having an ordering makes it possible to choose the rule that moves the proof closest to the final goal.  

It is shown that the inference rules are sound.  
It is shown that C is complete, by demonstrating an equivalence to classic propositional calculus.  

## [System X](https://github.com/tstockwell/TermSatNet/wiki/system-x)
System X (for eXchange) is an extended version of System C with even shorter proofs.  

X represents expressions as saturated [e-graphs](https://en.wikipedia.org/wiki/E-graph).  
X uses [equality saturation](https://en.wikipedia.org/wiki/E-graph#Equality_saturation) to build a saturated e-graph from a *root expression*.  
> A minimal expression is an expression that cannot be reduced further by any inference rule.  
> A standard expression is an expression where every subterm is minimal.  
> Because expressions ae reduced from the the bottom up, only standard and minimal expressions need ever be included in an e-graph.

E-graphs in X represent a set, called the [*congruence closure*](https://www.bodunhu.com/blog/posts/congruence-closure/), of all 
equivalent expressions with the exact same set of variables as the root expression.  

X includes all the inference rules from System C and adds the structural rule of [exchange](https://en.wikipedia.org/wiki/Structural_rule).  
Since exchange is admissible, an exchange step is equivalent to many steps using the other rules.  
Exchange is also guided by cofactor constraints, similar to iteration and deiteration, but 
exchange looks for cofactors in the congruence closure of an expression, not just in a single expression.  
> Exchange is a kind of shortcut that makes proofs much shorter.

X includes an algorithm that guides the proof process, thus the proof process is automatic. 

## [Proof Complexity in System X](https://github.com/tstockwell/TermSatNet/wiki/complexity)

It is shown that any reductive proof with an iteration may be written as a shorter proof that uses exchange.
> Therefore, thanks to the exchange rule, every step in a proof can be a reduction.

It is shown that, when a standard expression is reduced, 
that it's reduced according to the rules of the path ordering.  
> Corollary: Every step in a proof either...  
> - increases the number of constants in the expression, 
> - reduces the length of the expression, 
> - or reduces the # of variables in the expression.  

Finally, it is shown that, if an expression is reduced according to the path ordering 
then it takes at most a number of steps that is a quadradic 
function of the size of the expression.

QED

