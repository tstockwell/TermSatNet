## UNDER CONSTRUCTION

I use this repository for doing experiments in rewriting and logic.  
I'm mostly interested in teaching myself how to build rewrite-based systems.  
Here, I have built a logic system from rewrite rules.  

I've recently made this repository public because I would like to get feedback  
on this system, and because it provides a public record of my work.  

This stuff is not ready for primetime, I'm working on docs and code.  

This repository contains...
- [**SystemC**](https://github.com/tstockwell/TermSatNet/wiki/SystemC)  
    A reductive rewrite system that's also a system of [implicational propositional 
logic](https://en.wikipedia.org/wiki/Implicational_propositional_calculus).  
    SystemC is both a formal logic system and a formal rewrite system.  
    
    SystemC is based on the concept of a *cofactor* (aka an entailment, or logical consequence).  
    Cofactors are entailments between nested terms in an expression.  

    SystemC also has a path ordering (ie a concept of entropy).  
    SystemC works by rewriting expressions into simpler expressions, thus proving equivalence.  

    > [Here](), it is shown that SystemC is *sound*.  
    > [Here](), it is shown that SystemC is equivalent to classic propositional logic and is therefore *complete*..

    > [Here](), it is shown that if you know all the cofactors of an expression  
    then you can minimize expressions in a quadradically-limited # of steps.  
    
    > [Here](), it is shown that if you know all the cofactors of an expression,  
    and you construct your proof in the right way,  
    then you can fully minimize expressions in a linear # of steps.

- [**MiniC**](https://github.com/tstockwell/TermSatNet/wiki/MiniC)  
    An automated, bottom up, proof procedure for SystemC.  
    MiniC uses extended [e-graphs](https://en.wikipedia.org/wiki/E-graph) to efficiently represent expressions and thier cofactors.  

    MiniC uses an extended version of equality saturation to compute all the cofactors  in an expression and thier possible substitutions.

    > [Here](), it is shown that MiniC can compute all the cofactors in an expression in a polynomially-limited # of steps.

- **C SAT**  
    A SAT solver built with MiniC.  



