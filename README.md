## UNDER CONSTRUCTION

Until further notice, be advised that this repository is under construction.  
I always work from the top down so, when my plans for this project change, 
I change this page before I do anything else. 
When the code and docs are complete I'll remove this notice.  

## Preface 

I'm a retired, un-degreed, computer programmer.  
I have had a long-time interest in rewrite systems, and I use this repository for experimenting with rewriting and logic.  
I intended to someday build a new kind of template processor for rendering html.  
Instead, I have produced a logic system. 

System X is a rewrite system that reduces boolean expressions.  
I have convinced myself that this system can minimize boolean expressions in polynomial time.  

Since I started taking this idea seriously I have been putting more work into this repository.  
I am currently rewriting documentation and building a SAT solver based on this system.  
I've made this repository public because I hope to connect with folks that can provide feedback.  

What follows is an overview of what's in the documentation so far...
- Some background on deriving inference rules.
- Two systems of logic are presente; a simple system, and one designed to produce short proofs.
- Finally, a section that proves various theorems of the latter system.  

## Syntax-Guided Rule Synthesis

Until recently I didn't know that [syntax-guided rule synthesis](https://www.cis.upenn.edu/~alur/SyGuS13.pdf) was a thing.  
But essentially that's how the inference rules for System X were developed.

In other words, the inference rules for System X were not invented, designed, nor selected by me.  
The rules were derived.  
My homegrown version of rule synthesis derives rules from an ordered enumeration of expressions, 
and is designed to produce a minimal set of rules.  
My process was inspired by the [Knuth-Bendix Completion](https://en.wikipedia.org/wiki/Knuth%E2%80%93Bendix_completion_algorithm) method.  

The synthesized rules in thier raw form are simple unconstrained rewrite rules.  
The rules exhibit patterns that are obviously the result of some intelligence embedded in them.  
Also, the number of raw, unconstrained rules grows exponentially as the number of variables increase, and a complete set of rules is infinite.  
So the rules in thier raw form are not very useful.  
But I've managed to boil the raw rules down to a handful of logically constrained rules, 
and those rules are now the inference rules in System X.  

One reason I feel confident in the claims I make about System X is precisely because it's not something that I invented, it's a description of something that I computed.  
*Explaining* what you have computed is a whole other problem though :-).  
It took a lot of effort to translate the raw generated rewrite rules into inference rules, AI has helped.  
Other than showing that the inference rules in System X do in fact respect the path order, 
the complexity theorems are straight-forward to prove (the only kind theorem that I am capable of proving :-)).  

## [System C](https://github.com/tstockwell/TermSatNet/wiki/system-c)

System C (for cofactors) is a system of propositional logic based on rewriting, 
similar to [Existential Graphs](https://en.wikipedia.org/wiki/Existential_graph) or the [Laws Of Form](https://en.wikipedia.org/wiki/Laws_of_Form).  
> But it's designed for machines instead of humans, and it's designed to produce shorter proofs.  

Basic expressions are composed of the constant T, variables, and nand operators.  

C defines a set of expression orderings (aka [path orderings](https://en.wikipedia.org/wiki/Path_ordering_(term_rewriting))) that define what makes one expression simpler than another.

C uses the inference rules from existential graphs; [double negation elimination, erasure, deiteration, and iteration](https://en.wikipedia.org/wiki/Existential_graph#Alpha), and adds [commutivity](https://www.philosophypages.com/lg/e11b.htm#:~:text=Commutation,any%20of%20the%20possible%20conditions.).  

The application of the structural rules, iteration and deiteration, is guided by logical constraints called *cofactors* that must be present in the expression in order to apply the rule.  
> A cofactor is a subterm of an expression that has an implication relationship with the expression.  
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

E-graphs in X represent a set, called the [*congruence closure*](https://www.bodunhu.com/blog/posts/congruence-closure/).  
In X the congruence closure is the set of all equivalent expressions with the exact same set of variables as the root expression.  

X includes all the inference rules from System C and adds the structural rule of [exchange](https://en.wikipedia.org/wiki/Structural_rule).  
Since exchange is admissible, an exchange step is equivalent to many proof steps using the other rules.  
> In X, exchange is a kind of shortcut in a proof that can make proofs much shorter.
Exchange is also guided by cofactor constraints, similar to iteration and deiteration.  
However, the cofactors can be anywhere in the congruence closure of an expression, 
not just in a single expression.  

X includes an algorithm that guides the proof process, thus the proof process is automatic. 

## [Proof Complexity in System X](https://github.com/tstockwell/TermSatNet/wiki/complexity)

It is shown that any reductive proof with an iteration may be written as a shorter proof that uses exchange.

Corollary: Every expression has a minimization proof where every step in the proof is a reduction.

It is shown that, when a standard expression is reduced by an inference rule, 
the resulting expression comes before the axiom in the path ordering.  

Corollary: The inference rules in X are equivalent to the rules generated by the rule synthesis process.

Corollary: Every step in a proof either...  
- increases the number of constants in the expression,  
- reduces the length of the expression,  
- or reduces the # of variables in the expression.  

Corollary: Expressions can be minimized in a number of steps that is a quadradic.

QED

