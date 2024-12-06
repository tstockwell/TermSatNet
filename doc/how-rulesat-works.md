# How RuleSAT works

Overview of the RuleSAT Formula and Rule Generation System.

## Introduction 
I'm a run-of-the-mill software developer, recently retired, better than most other run-of-the-mill developers at writing code.
As an engineer, I'm super into production systems, I find them really useful and super interesting.  
Add, because of my raging ADD, I think about production systems a lot.  
And looking back, I think I have something to say about how to build software, and it has to do with production systems.  
RuleSAT is *not* what I set out to say, more about that some other time.  
RuleSAT popped into existence while I was trying to write down what I wanted to say about production systems.  

Specifically, I posed this question to myself, it's a question about generating the rules for a production system...
	> Can I somehow automatically discover, or somehow generate, a set of rules for a production system 
	> that will reduce bool formulas to their canonical form in provably polynomial time?

And it turns out that, yeah, I can construct such a set of rules.  
And it's not even complicated to do so, the procedure for generating all the rules is described in another section in this document.  
The procedure *is* a lot of work, but not at all hard to understand.  
I call this procedure the **rule sieve**.  

BUT, the set of rules produced by the rule sieve is **infinite**.  
	> That's disappointing but not surprising.  

BUT, the rule sieve emits all the rules for formulas with N variables 
	before the rules for formulas with N+1 variables.  
	> So, the set of all rules for formulas with N variables is finite.  
	> That's good news, this could have been a show-stopper.  

BUT, even for small N the # of rules is really really large.  Like f'ing large.  
	> That sucks.  This really complicates things.
	> Definitely will need a distributed system to solve real-life problems.

BUT, the # of rules can be greatly reduced by simplifying the structure of formulas
	> Boiling formulas down to nothing more than variables and a single operator dramatically reduces the # of rules.  
	> This is good news, we can solve real-life problems on real-life computers.  

BUT, none of this simplifies the rule sieve procedure.  
	> It takes a lot of computation to produce rules.  
	> And the amount of work it takes to produce the next rule increases with each rule generated.  

RuleSAT started with the realization that I could fairly easily create a production system 
that could solve propositional formulas that were substitution instances of formulas with no more than 3 variables.  
And I wondered what it would take to expand that to 4 variables, and so on.

I ended up modeling RuleSAT as a [production system](https://en.wikipedia.org/wiki/Production_system_(computer_science)).  
A RuleSAT database is a set of rules that can be used by the production system to reduce formulas.

> The RuleSAT production engine is a tiny rule-based engine 
> that uses a pluggable set of rules 
> to reduce propositional formulas to their canonical form.

My 'bootstrap' program generates a database of rules, ie 'another program'.  
The 'program' is executed by the RuleSAT reduction engine when it is given a formula to reduce.  
We can change the program by changing the rules.  
And we can generate a 'program' by generating rules.

The trick then is to generate a set of rules that do what you want.  
RuleSAT does this by, first, defining a formula reduction ordering.  
Then RuleSAT essentially uses the [Knuth-Bendix completion method](https://en.wikipedia.org/wiki/Knuth%E2%80%93Bendix_completion_algorithm) 
to generate rules to complete the system.

# The RuleSAT System

RuleSAT works by reducing propositional formulas to their canonical form.  

A RuleSAT system is composed of the following components...
- The RuleSAT Rule Generator : Creates or extends a RuleSAT rule database with new rules
- A RuleSAT Rule database : A previously built database of rules
- The RuleSAT production engine : Uses the rules in a rule database to reduce formulas
- A formula to reduce.

The RuleSAT production engine repeatedly rewrites the formula, 
as specified by rules in the database, 
until no more rules apply, 
at which time the formula will be in canonical form.  

The Rule Generator can bootstrap RuleSAT databases and extend existing databases.
The Rule Generator is essentially an implementation of the [Knuth-Bendix completion method](https://en.wikipedia.org/wiki/Knuth%E2%80%93Bendix_completion_algorithm).  

## RuleSAT formula reduction ordering and rule generation

First, RuleSAT defines an enumeration of all possible formulas.  
Do so provides an ordering for formulas.  
This ordering is used to define a canonical form for all formulas.
For any non-canonical formula, the canonical form is the first formula in RuleSATs' formula ordering 
with the same truth table.

As formulas are enumerated they are marked as canonical or non-canonical.  
If canonical then its added to the rule database.
If non-canonical and cant be reduced by the existing rules then it is also added to the database.
Non-canonical formulas in the database are the left sides of rules and the associated canonical rules are the right sides.  

Formulas are enumerated by...
Adding the next variable to the list.  
Variables are numbered starting from 1.
Then 




