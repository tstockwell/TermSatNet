# ReRite

ReRite is a SAT solver designed to be that uses proof analysis to reduce nand-based propositional formulas to their canonical form.  
ReRite completely reduces formulas in polynomial time as a function of the length of the starting formula.  
There's more in the wiki, including some proofs. 
The RR solver can optionally be assisted by a database of 'reduction rules' that give the solver a significant performance boost.  
The database is not required for RR to run in polynomial time, the database makes RR infinity fast/smart.  

As RR reduces formulas it may discover new rules.  
RR may optionally save these rules in a *rule database*.  
Also optionally, as RR discovers RR can save new rules in a rule database 
ReRite is also an open source trie-structured database of reduction rules.  
ReRite works by reducing nand-based propositional formulas to their canonical form using the rules in a given database.  



The catch is that a ReRite server cannot **guarantee** that it can completely reduce formulas above its databases' **complexity rating**.  
A databases' complexity rating is the ID of the last formula in the database, which is also the largest ID in the database.  
A databases' **completeness rating** is the maximum number of variables in a formula for which the database is **guaranteed** 
to be able to reduce the formula to its canonical form.
Complexity ratings grow linearly while completeness ratings are logarithmic,   
it's probably easier for folks to understand the concept of completeness rating.  

The open source version of ReRite is limited to a database size that's reasonable to download from GitHub.  
The open-source version of ReRite has a completeness rating of 1000 variables.  
That doesn't mean that it's only capable of solving formulas with 1000 variables.  
It means that the open source version is capable of solving the hardest, thorniest, most complex problems of up to 1000 variables.  
The open source version can reliably solve many large but fundamentally simpler problems with millions of variables.  
Developers are also able to extend the open source rule database on their own,   
the ReRite project provides tools for building your own database.

The commercial version of ReRite has a completeness rating that is magnitudes of order greater than the open source version  
and is capable of solving the most difficult problems on the planet.
It takes computational effort to build a large ReRite database, like mining bitcoins.  
That's why there's a commercial version, as it takes some serious resources to keep growing the rule database.  


Table of Contents
[How ReRite works](how-rulesat-works.md): Overview of the ReRite Formula and Rule Generation System
[Formulas](formulas.md): Details of formula syntax and ordering
[Rules](rules.md): Details of the ReRite database.
[Proof-based Reductions](wildcard-analysis.md): Optionally minimize databases using algorithmic reductions that are slightly slower.  
[ReRite is O(nE2) and P=NP](complexity.md): I'm very open to constructive feedback, I'm serious about this.





TermSAT is a collection of C# applications written for the purpose of discovering and enumerating 
rewrite rules for reducing formulas in propositional logic.  
See http://en.wikipedia.org/wiki/Boolean_satisfiability_problem.  

The original idea was to create a google-sized database of reduction rules, and a complementary SAT engine that 
uses the rule database to reduce large propositional formulas before attempting to solve them with a 'normal' SAT solver.  

TermSat contains a set of MSTest classes that implement scripts that produce such a database (using sqlite) and produce reports.  
TermSat also contains classes for representing formulas and performing all sorts of operations on those formulas, 
such as generating substitution instances, performing unification, or discovering and applying reductions.  

After generating a rule database for the first time, it was noticed that all the reduction rules could be replaced 
by a collection of relatively simple algorithms, that TermSatNet calls 'schemes'.  
A scheme is an algorithm that discovers opportunities for formula reduction.  

For instance, most reduction rules are subsumed by this scheme...
```
	If F, a formula, is an implication and S is a subterm of F's consequent and setting S to TRUE forces F to be true 
	then any occurance of S in F's antecent may be replaced with FALSE.
	Conversely, if S setting to FALSE forces F to be true then any occurance of S in F's antecent may be replaced with TRUE.

	Also, if S is a subterm of F's antecedent then the same rules apply to instance of S in F's consequent.
```


What makes schemes interesting is that represent an infinite set of reduction rules.
And what makes them doubly interesting is that all the rules, for all formulas that are substitution instances of 
all formulas with 3 or fewer variables, can be reduced to their canonical form using TermSatNet's set of scheme algorithms.  

The next question to answer is whether TermSatNet's set of scheme algorithms are 'complete', in the Knuth-Bendix sense.


# SAT engine from reduction rules

A rule database is initialized with all rules necessary 
to reduce instances of formulas with 3 or less variables its canonical form.  
The Scripts.RunRuleGenerator method can be used to initialize a rule database.  
This starting set of rules is enough to guarantee that it's *possible* to reduce 
a given formula to its canonical form, in polynomial time, if rules are applied in the correct order.  

In order to be able to apply rules in any order, the starting set of rules must be made **complete**.  
To this end, the rule database is intended to be infinity expanded with new rules that make the rules 
complete for an ever increasing number of variables.  
The Scripts.ExpandDatabase method can be used to extend a database with rules for N or fewer variables, 
with rules for formulas with N + 1 variables.  

In this way, folks can build their own databases, and their own rule engines, 
that are as powerful as they need them to be.  
They just need to put the computational work into creating the database.  

Fun fact: If Scripts.ExpandDatabase, for some N, should ever fail to generate new rules
then the current set of rules is complete for all N, and P == NP.

I don't expect that to happen, but........

# A **Reduction Scheme** represents an infinite # of reduction rules.

After generating a rule database for the first time, it was noticed that all the rules could be 
replaced by a single 'scheme'.  
A scheme is an algorithm that discovers opportunities for formula reduction and applies the reduction.
The basic scheme (an algorithm that is more than equivalent to all the basic reduction rules of 3 variables or less) is this...
...replace any/all occurrences of T->F with F.
...replace any/all occurrences of F->f with T.
...replace any/all occurrences of f->T with T.
...replace any/all occurrences of T->f with f.
...replace any/all occurrences of f->F with -f.
...replace all instances of Y(f)->X(f) with Y(f)->X(T), where...
	- Y(f) is a formula than contains f as a subformula and reduces to F when f is replaced by F or T, and 
	- X is a formula than contains f as a subformula
...replace all instances of X(f)->Y(f) with X(F)->Y(f), where...
	- Y(f) is a formula than contains f as a subformula and reduces to T when f is replaced by F or T, and 
	- X is a formula than contains f as a subformula

The scheme proceeds in a depth-first fashion, staring at the deepest sub-formulas and proceeding upwards 
to the root formula.

SchemeReducer.Scripts.BasicSchemeEquivalence is a script that proves that the basic scheme is equivalent to the basic rule database.  

## Is the basic scheme complete?
Scripts.IsBasicSchemeComplete is a script that answers this question by looking for a reduction rule 
with N == 4 that is not subsumed by the basic reduction scheme.







