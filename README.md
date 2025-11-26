## UNDER CONSTRUCTION

Until further notice, all of this stuff is completely worthless and unfit for any purpose other than wasting time.  
I use this repository for doing experiments in rewriting and logic.  
I'm mostly interested in teaching myself how to build rewrite-based systems.  

I've made this repository public because I would like to get feedback  
and because it provides a public record of my work here.

This repository contains...
- SystemC, a system of [implicational propositional logic](https://en.wikipedia.org/wiki/Implicational_propositional_calculus) built from rewrite rules.
- MiniC, an associated proof procedure.  

Proofs in SystemC work by transforming expressions into simpler, logically equivalent expressions using rewrite rules.  
Instead of randomly applying rewrite rules to an expression, 
MiniC reduces expressions from the bottom up, 
thereby producing a linear # of simpler, compact expressions.  
Each compact expression in a bottom-up reduction can be reduced in a cubic # of steps.  
Such a minimization procedure can be used to solve satisfiability problems in a polynomial # of steps.

This stuff is not ready for primetime, I'm working on docs and code.  
But you're invited to [have a look](https://github.com/tstockwell/TermSatNet/wiki).  

## SystemC

### Path Ordering
SystemC has a [path ordering](https://en.wikipedia.org/wiki/Path_ordering_(term_rewriting)).  
The path ordering relates expressions by thier *entropy*, from simpler to more complex.      

### Inference Rules

 SystemC uses the [structural inference rules](https://en.wikipedia.org/wiki/Structural_rule); iteration(weakening), deiteration(contraction), and exchange.  
    
> From a logical perspective...  
    - iteration weakens axioms and conclusions by replacing a constant with a redundant term.  
    - deiteration contracts axioms and conclusions by replacing a redundant term with a constant.  
    - an exchange is a chain of related weakenings and contractions.  

> From a rewriting perspective...  
    - iteration increases entropy by replacing a constant with a redundant term.  
    - deiteration decreases entropy by replacing a redundant term with a constant.  
    - an exchange is a chain of related iterations and deiteraions.  

Exchange is a form of [cut](https://en.wikipedia.org/wiki/Cut-elimination_theorem) that can significantly shorten proofs.
### Cofactors
In SystemC, the application of the structural rules is constrained to terms called *cofactors*.  

> From a logical perspective, cofactors identify redundant terms in an expression.    
> From a rewriting perspective, cofactors represent *[critical terms](https://en.wikipedia.org/wiki/Critical_pair_(term_rewriting))* in an expression.  

### Proofs

In SystemC, logical implication is the act of deriving simpler statements from more complex statements, and proofs work by reducing complex expressions to simpler expressions.

Proofs in SystemC are heuristic, requiring inferences to be combined in just the right way to reach a conclusion.  

It is shown, for any *valid* implication in SystemC, that the consequent comes before the antecedent in the path ordering.  

### Soundness  
> From a logical perspective, SystemC is sound because each rules maintains logical equivalence.  
> From a rewrite perspective, SystemC is sound because each rules maintains local confluence.  

It is shown that each rule maintains logical equivalence and local confluence.

### Completeness    
> From a logical perspective, SystemC is complete because all equivalent expressions reduce to the same expression.  
> From a rewrite perspective, SystemC is confluent because all expressions that can be derived from each other reduce to the same expression.   

It is shown that SystemC is complete by showing that SystemC is equivalant to classic propositional calculus.  

## MiniC
MiniC minimizes expressions from the bottom up, and breaks the task of minimizing an expression into a linear # of smaller tasks, where each expression is *compact*, ie already mostly minimized.  

It is shown that each compact expression is no more than one deiteration or exchange step away from the minimal form.  

Is is shown that *if* you happen to already know the minimal form of an expression then you can always minimize the expression in a linear number of steps.  

That's because you'll be able to figure out the exact right set of exchanges to use.  
Without such apriori knowledge, the secret to creating short proofs is to start at the bottom of an expression (where the terms are guaranteed to exist in the minimal form) 
and use them to discover that exact right set of exchanges as you work your way up to the outermost form.  

Therefore, in order to produce short proofs, MiniC recursively builds extended [e-graphs](https://en.wikipedia.org/wiki/E-graph) for representing expressions, critical terms, and substitutions.  These graphs can be efficiently searched for exchanges.  

MiniC efficiently minimizes an expression by...  
- Reducing the task of minimizing an expression to the task of reducing many simpler, compact expressions.  

- Building the graph for a complex implication by combining the graphs of its antecedent and consequent... 
    - Discover new critical terms in a compact axiom that don't exist in its subterms.  
    - Extend critical terms in antecedent and consequent with new substitutions from each.  

    > From a logic perspective, this combining is a form of *unification*.  
    > From a rewriting perspective, this combining is a form of *completion*.  

- Discover exchanges by searching a complete graph for a substitution that makes the expression a contradiction.

    > From a logic perspective, this search is a form of [*resolution*](https://en.wikipedia.org/wiki/Resolution_(logic)).      
    From a rewriting perspective, this search is a form of e-matching.  

### Complexity
I've convinced myself that MiniC is cubic, O(B * (2 * (D + E)) * P) where...
- B = the # of compact expressions produced by the bottom-up proof procedure
    > Linear complexity, because there must be L or fewer terms in the final conclusion, where L is length of the axiom.

- D = steps required to discover new critical terms in a compact axiom that don't exist in its subterms.
    > Linear complexity, by sweeping the expression in both directions, all terms can be checked to see if they're a critical term in a linear number steps.  

- E = steps required to extend critical terms in antecedent and consequent with substitutions from each other.  
    > Linear complexity, by sweeping the expression in both directions, each critical term can be extended with any missing substitutions.  
    > This could be done at the same time as discovering new critical terms.  

- P = Steps required to propagate a new substitution all critical terms in an expression
    > Linear complexity, because the new substitution is not propagated throughout all expressions in the graph, only throughout the subterms of the axiom, and there are only L of those.


