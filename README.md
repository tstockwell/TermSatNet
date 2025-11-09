I have convinced myself that it's possible to minimize boolean expressions in polynomial time.  
I would like to have my work reviewed but I have no academic credentials nor acquaintances I can press into service.  
I have to start somewhere, so even though it's embarrassing as hell to show people what I do in my free time :-),  
I invite you to take a look at the documentation and code I wrote for it.  

The documentation presents two systems of logic that work by minimizing expressions, 
and shows that proofs in the latter system have a maximum length that is a polynomial function of the length of the axiom.  

[System C](https://github.com/tstockwell/TermSatNet/wiki/system-c) (for cofactors) is a system of propositional logic inspired by existential graphs.  
Basic expressions are composed of the constant T, variables, and nand operators.  
C uses 4 inference rules; double negation elimination, erasure, deiteration, and iteration.  
The use of the structural rules, iteration and deiteration, is guided by logical constraints called *cofactors* 
that must be present in the expression in order to apply the rule.  
Proofs in C are based on proving equivalence, and work by rewriting/reducing expressions to simpler/reduced expressions.  
Proofs in C are hueristic, requiring inferences to be combined in just the right way to reach a conclusion.  
It is shown that the inference rules are sound.  
It is shown that C is complete, by demonstrating an equivalence to classic propositional calculus.  

System C is then extended to create [System X](https://github.com/tstockwell/TermSatNet/wiki/system-x) (for exchange).  
X defines a set of expression orderings that define what makes one expression simpler than another.  
X includes all the inference rules from System C and adds the rule of associativity and exchange.  
Even though exchange is admissible, 
it's included in X because a single exchange step represents many iteration/deiteration steps, 
thus making proofs shorter.  
X represents expressions as e-graphs that represent the *congruence closure* of an expression.  
The inference rules are restated to operate on e-graphs instead of expressions.  
E-graphs make it possible to efficiently find cofactors in the *congruence closure* of an expression,  
which is required to find exchanges.    
X includes an algorithm that guides the proof process, thus the proof process is automatic.  

Regarding proofs in System X...  
Expressions are reduced from the bottom up,  
and thus only *standard* or *minimal* expressions are ever included in an e-graph.  
A standard expression is a non-minimal expression where every subterm is minimal.  
It is shown that there's never a need to use iteration in a proof 
because there's a shorter proof that uses exchange.  
It is shown that the size of e-graphs grows polynomially in relation to the size of the e-graph's root expression, in other words,  
to find the cofactors required to reduce an expression you only need to look at a polynomial number of other expressions.  
And it is also shown that standard expressions can be reduced in length in no more than three steps.  
Finally, it is shown that any expression can always be fully reduced in a polynomial # of steps.  

In summary...  
Adding a path ordering to a logic system makes it possible to create proofs 
where every step in the proof gets you closer to the goal, the minimal form of the expression.  
The exchange rule acts as a shortcut in a proof that would otherwise would require iteration (a step away from the goal).  
But finding exchanges has a cost, it may require looking through the entire confluence closure of an expression for the right cofactors.  
By using e-graphs, exchanges can be found efficiently.  
Finally, proofs in System X use all forms of reasoning (deductive, inductive, and abductive), and I don't think that's a coincidence.  

If you've read this far, you can find the full version of this stuff in the wiki...   

- [System C: A system of propositional logic](https://github.com/tstockwell/TermSatNet/wiki/system-c) 
- [System X: System C With Exchange](/wiki/system-x.md) 
- [Complexity Of Proofs in System X](wiki/complexity.md) 

You will also find a SAT solver in the code.
I've also added a discussion area for comments.  
