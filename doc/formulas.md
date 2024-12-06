# Formulas

All about formulas in RuleSAT.
The most important part of this document is the section on 

## Syntax

Formulas are as simple as possible.  
There is one operator, nand, aka the Sheffer stroke.
And numbered variables.
That's it, no negation, no constants.
The normal form uses Polish notation and the symbol '*' for the implication operator, followed by the antecedent and then consequent. 
Variables are represented by the '.' followed by an integer number greater than 0 for representing variables.

Some examples of formulas...
.1
|.1|.1.1			;TRUE
||.1|.1.1|.1|.1.1	;FALSE
|.1.1				;NEGATION
|.1|.1.2			;IMPLICATION 
||.1.1|.2.2			;DISJUNCTION
||.1.2|.1.2			;CONJUNCTION

## Alternative Syntax

Instead of ||.1.2|.1.2, write...
1= |.1.2
2= |~1~1
where ~N is a formula number

## Ordering

RuleSAT formulas are ordered.
In RuleSAT, it's very important that there is one, and only one, **canonical** way to represent a given formula.  
Such an ordering is a necessary condition for producing a **globally confluent** set of reduction rules. 

This document describes a method of ordering of formulas.
The purpose of this ordering is to provide a method of determining the 'complexity' of a formula.  
The ultimate purpose of such an ordering is to provide the basis for 
creating a globally confluent set of reduction rules for reducing proposition formulas.

## Ordering


Here is the ordering...
1) formulas that are shorter are less complex than longer formulas. 
2) formulas that are the same length are ordered by the complexity of thier antecedent.
	That is, formulas with less complex antecendents come before those with more complex antecedents
3) formulas that are the same length and have the same antecedent are ordered by the complexity of thier subsequent.
4) otherwise, formulas are ordered lexically using the following symbol order:
	- F
	- T
	- variables
	- negation
	- nand
	- implication
5) Finally, any formula, A, where N is the highest numbered variable in A, 
	comes before any formula F where the highest numbered variable in F is greater than N.
This rule makes it possible to assign numbers to formulas in finite models (aka real life models).
Without this rule it would be impossible to assign numbers to formulas other than variables, 
because there'd be infinite number of variables.

The order of the above rules must be respected.

Examples..
Rule 1: .2 comes before *.2T 
Rule 2: *T.1 comes before *.1.1 
Rule 3: |F.1 comes before |F.2
Rule 4: *.2|1.3 comes before *.2*1.3 
Rule 5: |.1.2 comes before .3

	
# Enumerating Formulas
F
T
|FF   rule: |FF->T
|FT	  rule: |FT->T
|TF   rule: |TF->T
|TT   rule: |TT->F 
.1
|F.1	rule: |F.1->T
|T.1
|.1|T.1 rule: |.1|T.1 -> T
.2
|T.2
|.1.2

