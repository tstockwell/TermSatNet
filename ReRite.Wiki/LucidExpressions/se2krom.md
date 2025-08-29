# Determining the satisfiability of a nand formula using Krom logic.

## Abstract

Structural logic can be understood as the application of logical principles or reasoning 
to analyze or understand the structure of a system or object.  
Structural logic involves using logical thinking to examine the organization, arrangement, and interrelationships 
of all the components within a given structure.  

Structural logic can be used to analyze boolean formulas 
by modeling the relationships between all the parts of the expression.  

In this document it is shown that the structural logic embedded in nand formulas can be 
modeled using Krom formulas (2-CNF) and .  
Doing so makes it possible to determine the satisfiability of the original formula in a 
polynomially bounded number of steps.

## 

Let E be a boolean formula that uses only nand operators, variables, and the constants T and F.
We will call such a formula a *structured formula*.  

Theorem: Structured formulas can be converted to an equivalent set of Krom formulas in a polynomial # or steps.  

Proof by induction on the # of operators N in the structured formula.

For N == 0,1...
When there are no operators, or a single operator in E then E is already a Krom formula.

For 1 < N...
Let E be an expression of the form |AC
