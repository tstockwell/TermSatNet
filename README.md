## UNDER CONSTRUCTION

Until further notice, be advised that this repository is under construction.  
I always work from the top down so, when my plans for this project change, 
I change this page before I do anything else.

When the code and docs are complete I'll remove this notice.  

## Overview 

I have convinced myself that it's possible to minimize boolean expressions in polynomial time.  
I am currently writing documentation and building a SAT solver based on the automatic theorem prover I've designed.  
I'm going to do this in public because I could definitely use some feedback.  

What follows is an overview of the documentation so far.  
It presents two systems of logic that work by reducing expressions, 
and shows that proofs in the latter system have a maximum length that is a polynomial function of the length of the axiom.  

## [System C](https://github.com/tstockwell/TermSatNet/wiki/system-c)
System C (for cofactors) is a system of propositional logic,
inspired by [Existential Graphs](https://en.wikipedia.org/wiki/Existential_graph) and the [Laws Of Form](https://en.wikipedia.org/wiki/Laws_of_Form).  
> But it's designed for machines instead of humans, and it's designed to produce shorter proofs.  

Basic expressions are composed of the constant T, variables, and nand operators.  

C defines a set of expression orderings (aka [path orderings](https://en.wikipedia.org/wiki/Path_ordering_(term_rewriting))) that define what makes one expression simpler than another.

C uses the inference rules from existential graphs; [double negation elimination, erasure, deiteration, and iteration](https://en.wikipedia.org/wiki/Existential_graph#Alpha), and adds [commutivity](https://www.philosophypages.com/lg/e11b.htm#:~:text=Commutation,any%20of%20the%20possible%20conditions.).  

The application of the structural rules, iteration and deiteration, is guided by logical constraints called *cofactors* that must be present in the expression in order to apply the rule.  
> A cofactor is a subterm of an expression that [entails](https://en.wikipedia.org/wiki/Logical_consequence) the expression.  
> Specifically, a cofactor is a subterm, that when replaced with constant, creates an expresssion that is equivalent to a constant.  

The application of the commutivity rules is constrained by the expression ordering.  

Proofs in C are based on proving equivalence, and work by rewriting/reducing expressions to thier simplest form.  
Proofs in C are heuristic, requiring inferences to be combined in just the right way to reach a conclusion.  
The expression ordering helps makes proofs shorter because, when more than one rule may be applied, 
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

It is shown that the number of expressions in a saturated e-graph is limited to a quadradic function of the size of the root expression. 
> In other words, to find the cofactors required to reduce an expression you only need to look at a polynomial number of other expressions.  

It is shown that standard expressions can be reduced in length in no more than three steps.  
> The fact that this is possible is a measure of how powerful the structural rules are, especially exchange.

Finally, it is shown that any expression can be minimized in a # of steps that is limited to a polynomial function of the size of the expression. 
> It's hard to believe that this is correct.  But even if it's not, I'm convinced that System X is worth building.  

## Summary

Even if this system turns out to not be as efficient as I think, 
I've already written enough code and tests to know that it's going to serve my purposes as the core of a rule-based programming language.

Here's why...  
System X itself is not something that I invented or created, 
it's actually a kind of compiled program.  
I *derived* it.  

I essentially wrote a specification of a program that can reduce boolean expressions and 'compiled' that specification into a set of logically constrained rewrite rules that became the inference rules in X.  
The expression syntax and the path ordering together are that specification.  
The inference rules in System X are a generalized version of the derived rules.  
> That's right, the inference rules for System X were not *invented* by me,  
they were *derived* from the structure of the expressions and the path ordering.  

One reason I am feeling confident in the work I've done here is precisely because it's not something that I invented, it's something that I computed.  
*Explaining* what you have computed is a whole other problem though :-).  
I had to come up with a way to prove properties about the generated rules.  
That's what the documentation is about.

I am obsessed with rewriting because programming with rules is *way way way* more modular, reusable, customizable, scalable, and extensible than the languages and tools we have now.  

And an engine that can do automated reasoning is powerful enough for generating web pages.  
When I'm done here I'm going to use this same system to build an engine for composing web pages in ways that will be revolutionary.  



