## UNDER CONSTRUCTION

Until further notice, all of this stuff is completely worthless and unfit for any purpose other than wasting time.  
I use this repository for doing experiments in rewriting and logic.  
I'm mostly interested in teaching myself how to build rewrite-based systems.  

I believe that I've designed a rewrite system that can super-efficiently minimize implicational expressions, 
and I want to get feedback.  
But this stuff is not ready for primetime, I'm working on docs and code.

This repository contains System C (for cofactors), a system of [implicational propositional logic](https://en.wikipedia.org/wiki/Implicational_propositional_calculus) that...  

- Includes a [path ordering](https://en.wikipedia.org/wiki/Path_ordering_(term_rewriting)).  An ordering defines what makes one expression simpler than another.  
> It is shown, for any *valid* implication in SystemC, that the consequent comes before the antecedent in the path ordering.  
> In other words, in SystemC, logical implication is the act of deriving simpler statements from more complex statements.  

- Uses the [structural inference rules](https://en.wikipedia.org/wiki/Structural_rule); iteration(weakening), deiteration(contraction), and exchange.  
> From a rewriting perspective, the structural rules of logic are rewrite rules that copy terms from one side of an expression to the other.

- Uses logical constraints called *cofactors* (aka substitution instances, aka advice) to identify terms 
that can be copied to, or removed from, one side of an implication, while maintaining equivalence.    
> From a rewriting perspective, cofactors are the terms in an expression (aka substitution instances)
that may be copied to, or removef from, the join points (aka *[critical terms](https://en.wikipedia.org/wiki/Critical_pair_(term_rewriting))*) of another expression.  

Exchange is admissible in SystemC but it makes proofs shorter.

I have also created an automated, bottom-up proof procedure for SystemC, called MiniC.  
MiniC breaks the task of minimizing an expression into a linear # of smaller tasks,  
where each expression is *compact*, ie already mostly minimized.  
And each one of those compact expressions is no more than one structural step away from being minimal.  

And thus it's easily shown that it's always *possible* to minimize expressions in a linear number of steps.
That is, *if* you happen to know the right set of cofactors, or get lucky when looking for cofactors.  

Therefore, in order to make it easy to track and compute cofactors, 
MiniC also recursively builds extended [e-graphs](https://en.wikipedia.org/wiki/E-graph) of expressions used to track and compute cofactors.  
It is shown that MiniC can efficiently compute the all cofactors in an expression by...    
- Using an e-graph to efficiently represent expressions and track cofactors.  
- Creating an e-graph for a new axiom by *resolving* the graphs of its antecedent and consequent.
> Must discover new join points in a compact axiom that don't exist in its subterms.
> Must extend join points in antecedent and consequent with cofactors from each
- Discovering structural reductions by propagating cofactors, newly added to a join point, throughout the entire axiom.

I've convinced myself that MiniC is O(B * (2 * (D + E)) * P) where...
- B = the # of compact expressions produced by the bottom-up proof procedure
> linear, because there must be L or fewer terms in the final conclusion, where L is length of the axiom.

- D = steps required to discover new join points (aka critical terms) in a compact axiom that don't exist in its subterms.
> linear, by sweeping the expression in both directions, all terms can be checked to see if they're a join point in linear steps.  

- E = steps required to extend join points in antecedent and consequent with cofactors (aka substitution instances) from each other.  
> linear, by sweeping the expression in both directions, each join point can be extended with any missing cofactors.
> This could be done at the same time as discovering new join points.  

- P = Steps required to propagate a cofactor to other expressions when its added to a join point
> Linear complexity, because the new cofactor is not propagated throughout all expressions in the graph, 
> it's only propagated throughout the subterms of the axiom, 
> and there are only L of those.

QED  
If you find yourself interested for some strange reason then [have a look](https://github.com/tstockwell/TermSatNet/wiki).

