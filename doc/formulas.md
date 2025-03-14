# Formulas

All about formulas in RR.

## Syntax

Formulas are as simple as possible.  
There is one operator, nand, aka the Sheffer stroke.
And numbered variables.
That's it, no negation, no constants.
The normal form uses Polish notation and the symbol '|' for the nand operator, followed by the left and right arguments.  
Variables are represented by the '.' followed by an integer number greater than 0 for representing variables.

Some examples of formulas...
.1
|T.1				;NEGATION
|.1.2				;NAND
|.1|T.2			    ;IMPLICATION 
||T.1|T.2			;DISJUNCTION
|T|.1.2				;CONJUNCTION
|T||T.1|T.2			;NOR


## Ordering

RR formulas are ordered.
In RR, it's very important that there is one, and only one, **canonical** way to represent a given formula.  
Such an ordering is a necessary condition for producing a **globally confluent** set of reduction rules. 

Here are the rules...
1) formulas that are shorter are less complex than longer formulas. 
2) constants are simpler than any other formula, T comes before F 
3) variables are simpler than operators
4) formulas that are the same length are ordered lexicographically.
5) Finally, any formula, A, where N is the highest numbered variable in A, 
	comes before any formula F where the highest numbered variable in F is greater than N.
This rule makes it possible to assign numbers to formulas in finite models (aka real life models).
Without this rule it would be impossible to assign numbers to formulas other than variables, 
because there'd be infinite number of variables at the beginning of the order.

The order of the above rules is important too.


# Example: Knuth-Bendix completion

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

