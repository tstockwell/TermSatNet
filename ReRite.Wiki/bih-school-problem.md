## Abstract

In this document I discover that this rule is a valid rule (for 3 <= Length(a))...
    [[ab][ac]] => [T[a[[Tb][Tc]]]]
...but that RR doesn't generate this rule, because Length([[ab][ac]]) < Length([T[a[[Tb][Tc]]]]).  
That's why RR should just add *all* rules to the LOOKUP table 
and *always* check every generalization to see if it constructs a valid reduction.  

Now that I think about... aren't rules confluent iff they are reversible?
The answer, is no, some systems use reversible rules because that can make it easy to prove confluence...  
A system with only reversible rules can still be non-confluent if there are "critical pairs" 
where different rewrite sequences lead to different terms.


The RR method minimizes expressions by repeatedly applying three kinds of operations to expressions; erasures, insertions, and rewrites.  
These operations are equivalent to Peirce's Rules plus rewrites, and are therefore sound and complete.  
RR reduces an expression by first removing all redundancies, by repeatedly applying available rewrites.
At this point the resulting expression will be 'mostly canonical', an expression of the form F = [A B] where both a and b are canonical.  
RR then reduces the expression further with uno application of uno of the following inference operations...
## Erasure/
    Let T be a term in F.B (or, if not F.B then F.A) that, when assigned a truth value, reduces the expression to F.  
    RR calls these terms *critical cofactors*, or often as "wildcards".  
    I haven't yet discovered a name for these in the existing literature.  
    A cofactor is a 
    Given such a term, a formula may be reduced by 
## Insertion
2. those that reduce a 'mostly canonical' expression (an expression of the form [A B] where both a and b are canonical).  
    by discovering a term in B (or, if not B then A) that, when assigned a truth value, reduces the expression to F.  
    Given such a term, a formula may be reduced by 


## Overview

The expression shown below is used in [Binary Implication Hypergraphs;Francès de Mas](#BIHs) as an example 
of a simple expression that cant be simplified by any currently known automated simplification method 
(other than hers, that uses BIHs.  Its needs a name, I propose 'No Mas Simplification', cause it can simplify until it cant no more be simplified).  

```
             ( 
                     ((A∨B) → (C ↔A))  
                 ∧  ((A ↔B) → ((¬A∨B) ∧ C))   
                 ∧  (  ((A∧B)∨(¬C))  ∧ (B →C) )) 
             ) 
             → (C → B)        
```

The thing is, the RR concept of 'wildcard analysis' can also simplify this expression.  

That doesn't surprise me.  
The RR system is basically equivalent to existential graphs and 
wildcard-analysis is basically equivalent to Peirce's simplification rules 
(which are known to be complete, see [Mathematical Logic with Diagrams;Dau]()).

Another thing, this isn't even a difficult problem, it can be reduced to T using just the RR concept of 'groundings', 
a simpler concept than a BIG much less the concept of a BIH.  

> A grounding is a subset of all the instances of some term in some formula such that 
> when all the terms in the grounding are replaced by a constant,  
> the formula reduces to a constant.  

Using the RR method it can be solved just by working from the bottom up and testing only common variables.   
The RR test suite contains more difficult tests that require 'unification' (derived terms other than variables) to complete.  

## Cofactors

Linear cofactor relationships represent sufficient conditions for the minimization of all of the above mentiunod decision diagrams (DD).
In this paper, we study linear (EXOR-based) relationships among any non-empty subset of the four two-variable cofactors of a Boolean function. 

## Conversions from propositional calculus to existential/RR notation...

    - NAND  :(A ~& B)   => [A ~& B]                 => [AB]
    - NOT   :(~A)       => [T~A]                    => [TA]
    - AND   :(A && B)   => [T[ A && B ]]            => [T[AB]]
    - IMPL  :(A -> B)   => [[T A ->] B ]            => [[TA]B]     
    - OR    :(A || B)   => [[T A ||][T B ]]         => [[TA][TB]]
    - EQ    :(A == B)   => [[ A == B ][[TA][TB]]]   => [[AB][[TA][TB]]]

Note that the original operators can be left embedded in the RR notation when converting, 
this makes the result just about as easy to read as the original 
while having the benefit of being much easier for humans to minimize smallish expressions.  

Brackets are visually attached to the operator to which they belong, this makes it easier to visually validate bracket nesting.

## Original Problem in RR Notation
School Problem in propositional calculus...
```
             ( 
                     ((A || B) -> (C == A))  
                 &&  ((A == B) -> (((~A) || B) && C))   
                 &&  (((A && B) || (~C)) && (B -> C))) 
             ) 
             -> (C -> B)        
```

School problem in RR Notation...
```
             [T[ 
                     [[T [[T A ||][T B ]] ]-> [[ C == A ][[TC][TA]]] ]  
                 &&  [[T [[ A == B ][[TA][TB]]] ]-> [T[ [[T [T~A] ]||[T B ]] && C ]] ]   
                 &&  [T[
                        [[T [T[ A && B ]] ]||[T [T~C] ]]
                        && 
                        [[T B ]-> C] 
                     ]] 
                ]
                -> [[TC] -> B]        
             ]
```
Believe it or not, once the conversion rules are burned into your brain the above expression is much easier to visually validate.  

In the following the school problem is reduced to T using plain RR 'wildcard analysis'.
In what follows, when it is stated to 'apply the rule such-and-such' it means that 
the reduction such-and-such can be shown to be valid using wildcard analysis.  
Put another way, rules are reductions that have been previously proved.  
The LHS of a rule has already been shown to reduce to the RHS of the rule using wildcard analysis.  
Rules describe equivalence relationships between expressions.  
That's the nice thing about the RR system, once you reduce an expression to another expression 
you can reuse the two formulas as a rule to reduce even more expressions.  
First, apply the rules [T[TA]] => A in the third line 
and [T[TC]] => C and [T[T[AB]]] => [AB] in the fifth line to get...
```
             [T[ 
                     [[T [[T A ||][T B ]] ]-> [[ C == A ][[TC][TA]]] ]  
                 &&  [[T [[ A == B ][[TA][TB]]] ]-> [T[ [A[TB]] && C ]] ]   
                 &&  [T[
                        [[AB] -> C]
                        && 
                        [[TB] -> C] 
                     ]] 
                ]
                -> [[TC] -> B]        
             ]
```
Now, reduce lines 4-8...
[T[ [[ab]c][[Tb]c] ]]
=> [c[ [[ab]T] [[Tb]T] ]]
=> [c[ [[ab]T] b ]]
=> [c[ [[aT]T] b ]]
=> [c[ a b ]]
=> [c[ab]]
```
             [T[ 
                     [[T [[Ta][Tb]] ]-> [[ac][[Ta][Tc]]] ]  
                 &&  [[T [[ A == B ][[TA][TB]]] ]-> [T[ [A[TB]] && C ]] ]   
                 &&  [c[ab]] 
                ]
                -> [b[Tc]]        
             ]
```
Reduce line 2...
[T [[Ta][Tb]] ...] 
=> [ab]  
[ [ab] [[ac][[Ta][Tc]]] ]  

             ---------------
             Reduce....
                         [T[TC]] => C
                     [[[T[AB]] C] [[TB]C]] => [C [[T[AB]][TB]]] ; reduce to 'shorter' formula that comes first in expression order
                     [[T[AB]][TB]] => T
                     [T[TC]] => C
                     [T[TA]] => A
                 remove C in...        
                 [T[                         
                     [T[                     
                         [[T [[TA]||[TB]] ] [[CA][[TC][TA]]] ] 
                         [[T [[AB][[TA][TB]]] ] [T[ [A[TB]] C ]] ] 
                     ]]                      
                     C                      
                 ]]                          
                 to get...
                 [T[                         
                     [[T [[AB][[TA][TB]]] ] [T[ [A[TB]] T ]] ] 
                     C                      
                 ]]                          
                 [ [T[[AB][[TA][TB]]]] [A[TB]] ] => [[A [TB]] [B [TA]]]
             ====>
             [
                 [                         
                     [[A [TB]] [B [TA]]]
                     C                      
                 ]                               
                 [[TC]B]                     
             ]                               
             ====>
             [
                 [                         
                     [[A [CB]] [B [CA]]]
                     C                      
                 ]                               
                 [[TC]B]                     
             ]                               
             ====>
             [
                 [                         
                     [T[A[TB]]]
                     C                      
                 ]                               
                 [[TC]B]                     
             ]                               
             
             ---------------
             To solve, lets assume that the two top-most sub-expressions are canonical, as would be the case in RR.  
             RR would go about performing wildcard analysis for the common variables B and C, and stopping when a reduction opportunity is discovered.  
             Note that RR is using *abductive reasoning*, basically making an educated guess.  
             Using BIHs is not abductive reasoning, it's deductive reasoning, and it's much more efficient.  
             Using BIHs 
             
         </summary>

## References

### BIHs
[Binary Implication Hypergraphs for the Representation and Simplification of Propositional Formulae; Francès de Mas](#pay-per-view)

### Dau
[Mathematical Logic with Diagrams, Based on the Existential Graphs of Peirce;Dau](http://www.dr-dau.net/Papers/habil.pdf).

### Cofactors
[Linear Cofactor Relationships in Boolean Functions; Zhang, Chrzanowska-Jeske, Mishchenko, Burch](https://people.eecs.berkeley.edu/~alanmi/publications/2005/tcad05_lcr.pdf)