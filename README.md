## UNDER CONSTRUCTION

Until further notice, all of this stuff is completely worthless and unfit for any purpose other than wasting time.  
I use this repository for doing experiments in rewriting and logic.  
I'm mostly interested in teaching myself how to build rewrite-based systems.  

I believe that I've designed a rewrite system that can minimize implicational expressions in a polynomial # of steps, 
and I want to get feedback.  
But this stuff is not ready for primetime, I'm working on docs and code.

This repository contains System C (for cofactors), a system of [implicational propositional logic](https://en.wikipedia.org/wiki/Implicational_propositional_calculus) that...  
- Includes a path ordering that's based on the entropy of an expression.  
- Uses the [structural inference rules](https://en.wikipedia.org/wiki/Structural_rule), iteration(weakening), deiteration(contraction), and exchange.  
- Uses logical constraints called *cofactors* to constrain where rules may be applied.  
- Works by rewriting expressions into *simpler* expressions, thus proving equivalence.  

Exchange is admissible in SystemC but including it makes proofs shorter.

I have also created an automated, bottom-up proof procedure for SystemC, called MiniC.  
MiniC breaks the task of minimizing an expression into a linear # of smaller tasks,  
where each expression is *compact*, ie already mostly minimized.  
And each one of those compact expressions is no more than one structural step away from being minimal.  
And thus it's easily shown that *it's always possible* to minimize expressions in a linear number of steps.  
That is, *if* you happen to find the right set of cofactors.  

Therefore, in order to make it easy to track cofactors, 
MiniC also recursively builds extended e-graphs of expressions for that purpose.  
It is shown that MiniC can compute a single structural reduction in a polynomial # of steps by...  
- using an e-graph to represent expressions, thus easily tracking equivalence and avoiding infinite recursions.  
- 'Resolve' the e-graphs of the antecedent and consequent to create e-graph of a new axiom by 
discovering new join points (aka critical terms) and extend existing join points with new cofactors (aka substitution instances).
- discover a structural reduction by propagating cofactors, newly added to a join point, throughout the entire axiom.

I've convinced myself that MiniC is O(B * (2 * (D + E)) * P) where...
- B = the # of compact expressions produced by the bottom-up proof procedure
> linear, because there can't be more than L terms in the minimal expression, where L is length of axiom.

- D = steps required to discover new join points (aka critical terms) in a compact axiom that don't exist in its subterms.
> linear, by sweeping the expression in both direction, all terms can be checked to see if they're a join point in linear steps.  

- E = steps required to extend join points in antecedent and consequent with cofactors (aka substitution instances) from each.  
> linear, this can actually be done at the same time as discovering new join points.  

- P = Steps required to propagate a cofactor to other expressions when its added to a join point
> Linear complexity, because the new cofactor is not propagated throughout all expressions in the graph, 
> it's only propagated throughout the subterms of the axiom, 
> and there are only L of those.

QED
If you find yourself interested for some strange reason then [have a look](https://github.com/tstockwell/TermSatNet/wiki).

