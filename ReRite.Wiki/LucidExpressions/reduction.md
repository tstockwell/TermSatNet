# LE Cofactor Reduction


> It is assumed that the reader has already read the [Introduction to Lucid Expressions](lucid-expressions.md).  
> It's especially important to understand the LE concept of *cofactors*.  

LE Cofactor Reduction is a reduction system designed to rewrite expressions in the same way as the RR System, 
but using dynamically computed cofactors instead of pre-generated rewrite rules.  

Like the RR SYstem

In the LE system, reduction is the process of minimizing an expression to its canonical form.  

One can think of reduction as *unwinding* the derivation of an expression to it's axiomatic, canonical form.  
Because LEs' rules of inference are complete, symmetric, and confluent, we can reduce expression by repeatedly 
applying rules to an expression, but only in ways that produce simpler expressions.  
If an expression reduces to T/F then the expression is a tautology/contradiction.  
If an expression reduces to anything but F then the expression is satisfiable.  

The reduction process proceeds from the bottom up.  
This is because the cofactors of subterms must be known in order to compute reductions of higher order expressions.   
The only way to accomplish this is to start at the deepest parts of the starting expression, 
where the terms are atomic and canonical, and build up to more complex canonical expressions.

As reduction proceeds an indexed table of cofactors is built.  
The cofactors of simpler expressions are used to identify opportunities for reduction of the more complex expressions built from them.  

The reduction method performs three fundamental forms of inference...  
- Induction : LE always reduces an expression by first reducing the simplest subterm that's not already known to be canonical.  

	> Thus, LE is always working from simpler expressions to more complex expressions, and 
	> thus LE knows a lot about an expressions' subterms when reducing more complex expressions.  

- Deduction : Grounding Cofactors (a kind of 2-variable Krom clause over expressions) are 
	calculated for every canonical subterm of a mostly-canonical  expression.  

	> When a cofactor or clause is added to the set of all cofactors then resolution is used to deduce any new cofactors.
	> Resolution is polytime since cofactors are also a kind of 2-variable Krom clause.  
	> Clauses are discovered as the reduction process proceeds, which results in more opportunities to reduce more complex expressions.  

- Abduction : Based on cofactors, inference rules are applied to deduce equivalent expressions, and only those that are simpler are valid.  

## Example

Consider the lucid expression...  
```(T (a (a (T b))))```  
which means...  
```A and (if A then B)```.  

(a (a (T b))) is the simplest non-canonical subterm of the starting expression.  
Using deiteration, it can be reduced to...  
```(a (T (T b)))```  
which, using double-cut elimination can can be reduced to...
```(a b)```.  
thus reducing the starting expression to...  
```(T (a b))```  
which is canonical and equivalent to...  
```A and B```  


### Mostly-Canonical Expressions

An expression E = (P Q) is a *mostly-canonical* expression if P and Q are canonical but E is not.  

### Reduction

This section contains a pseudo-code description of the Reduce function.   
The Reduce function accepts a mostly-canonical expression and returns the next reduction.


```
/// A reduced expression and proof (either a subtitution or a cofactor)
record ReduceResult(Expression Reduction, SubstitutionResult? Substitution, Cofactor? Cofactor)  

Let COFACTORS = a global table of tuples that represent all known grounding cofactors and all derivable cofactors of canonical expressions.  
Let SUBSTITUTIONS = a fixed, global table of rewrite rules, includes rewrite rules for cut elimination.  

ReduceResult? Function Reduce (Expression mostlyCanonical)
{
	// if the expression is already known to be canonical then we're done
	if (Contains(CANONICAL, mostlyCanonical))
	{
		return null
	}

	Let substitutionResult = TryFindGeneralization(SUBSTITUTIONS, mostlyCanonical)
	if (substitutionResult && ) 
	{
		// only use the substitution if the conclusion is simpler
		if (Compare(substitutionResult.Conclusion, mostlyCanonical) < 0)
		{
			return (substitutionResult.Conclusion, substitutionResult, null)
		}
	}

	// no substitutions found, that leaves deiteration or paste-and-cut.

	if (mostlyCanonical.RHS == T)
	{ 
		// paste into rhs and cut from lhs
		Let fGroundingfCofactors = Cofactors(mostlyCanonical.LHS).Where(_ => _.R == F && _.C == F)
		foreach (fGroundingfCofactor in fGroundingfCofactors)
		{
			Let reducedE = (mostlyCanonical.LHS[fGroundingfCofactor.S<-T] fGroundingfCofactor.S)
			if (Compare(reducedE, mostlyCanonical) < 0)
			{
				return (reducedE, null, fGroundingfCofactor)
			}
		}
	}
	else if (mostlyCanonical.LHS == T) 
	{ 
		// paste into lhs and cut from rhs
		Let fGroundingfCofactors = Cofactors(mostlyCanonical.RHS).Where(_ => _.R == F && _.C == F)
		foreach (fGroundingfCofactor in fGroundingfCofactors)
		{
			Let reducedE = (fGroundingfCofactor.S  mostlyCanonical.RHS[fGroundingfCofactor.S<-T])
			if (Compare(reducedE, mostlyCanonical) < 0)
			{
				return (reducedE, null, fGroundingfCofactor)
			}
		}
	}
	else 
	{  
		// deiterate, look for f-groundings f-cofactors of either side 
        Let rhsGroundings = Cofactors(mostlyCanonical.RHS).Where(_ => _.R == F && _.C == F)
		foreach (rhsGrounding in rhsGroundings)
		{
			Let reducedE = (rhsGrounding.S  mostlyCanonical.RHS[rhsGrounding.S<-T])
			if (Compare(reducedE, mostlyCanonical) < 0)
			{
				return (reducedE, null, rhsGrounding)
			}
		}

		Let lhsGroundings = Cofactors(mostlyCanonical.LHS).Where(_ => _.R == F && _.C == F)
        Let commonGroundings = Join(lhsGroundings, rhsGroundings, _ => _.S).FirstOrDefault()
        Let (leftCofactor, rightCofactor) = Join(lhsGroundings, rhsGroundings, _ => _.S).FirstOrDefault()
		if (leftCofactor != null)
		{
			Let reducedE = (leftCofactor.S, (leftCofactor.C rightCofactor.C))
		}
		
	}

	// the given expression is canonical
	return null
}
```

## Complexity

LEs can be reduced to their canonical form in polynomial time.  

The steps that the LE reduction method performs can be categorized into two types...  
- The number of times the starting expression is reduced.
	> In other words, the number of steps in the equivalence proof from the starting expression to its canonical form.   
- The number of steps in each reduction.
	> The number of steps in each reduction is proportional to the number of cofactor records created during each reduction, or 1 if substitution occurred.   

It will be shown that the maximum size of any equivalence/reduction proof is at most Pow(N,2), where N is the length of the expression to reduce.  

And it will be shown that the maximum number of cofactor records computed during a proof is limited to Pow(2N,2)

That makes LE's time complexity on the order of O(Pow(N,2) * Pow(2N,2)) = O(4Pow(N,4)).  



-----------------------------




# Insight : Unification of cofactors can identify a reduction in polytime.

Given an expression, the # of cofactors that can be directly computed from the expression is polynomial.  

We could use deiteration and iteration to expand the set of cofactors until we found a reduction (if one exists).  
However, there are a potentially exponential # of such cofactors.  
Instead, we can use unification to quickly deduce common tgf-cofactors that we can use to reduce the expression.  

Which seems doable, since....
- to minimize we need to find an fgf-cofactor of (T ((1 (T 2)) (2 (T 1)))) 
- computing deductive closure of cofactors does not yield a reduction
- Must find common tgf-cofactor of both sides.  
	- tgf-cofactors of left side are 1, and (T 2)  
	- tgf-cofactors of right side are 2, (T 1)
- unifying (T 2) and (T 1) is easy, but must do unification correctly  
	> ie dont use the standard Robinson unification algorithm which is exponential, see Handbook.  
- we need to retain the unifying substitution so that we can actually perform the rewrite.  
	That is, given that (1 2) is a fgf-cofactor of ((1 (T 2)) (2 (T 1))), how do we rewrite it?  
	By applying the unifying substitution and reordering to get ((1 (1 2)) (2 (1 2))).  
	Then we apply the substitution (1 2)<-T to get ((1 T) (2 T)), and therefore  
	(T ((1 (T 2)) (2 (T 1)))) => ((1 2) ((1 T) (2 T))) => ((1 2) ((T 1) (T 2))) 

Here are some of the cofactors calculated for the above expression, note the last two...

	Cofactors
	(S	==	R) -> (	E ==	C)
	2		F		2		F
	2		T		2		T
	2		F		(T 2)	T
	2		T		(T 2)	F
	(T 1)	F		(2 (T 1))	T	; rhs of (2 (T 1))
	(T 2)	F		(1 (T 2))	T	; rhs of (1 (T 2))

if we can unify (T 1) and (T 2) then we can create a common fgt-cofactor of both sides 
and thus a fgf-cofactor that we can use to reduce the expression.  

Still one issue... how to actually make the rewrite that reduces the expression?
That is, we know that (1 2)



	(1 (T 2)) == (1 (1 2))			; iteration
	(2 (T 1)) == (2 (1 2))			; iteration
	(1 2)	F		(2 (1 2))	T	; rhs of (2 (1 2))
	(1 2)	F		(2 (T 1))	T	; from previous two lines

	(1 2)	F		(2 (T 1))	T
	(1 2)	F		((1 (T 2)) (2 (T 1))))  F
	(1 2)	F		(1 (T 2))	T	; 

	|T||1|T.2|2|T.1 => ||1.2||T.1|T.2 ; eq to (1->2 && 2->1) => (2 == 1)
	|T||1|T.2|2|T.1 => ||1.2||T.1|T.2 ; eq to (1->2 && 2->1) => (2 == 1)

This is an example of an expression that can't be reduced by directly computing cofactors of the terms in the expression.  

	|T||1|T.2|2|T.1 => ||1.2||T.1|T.2


# Insight : Add an ordering rule that expressions with fewer unique terms are less than expressions with more terms.  
This new rule is applied before the length ordering rule, giving it a higher priority than the length rule.  
 
The effect would be this...  
Instead of this rule (which is quite difficult to implement)...  

	|T||1|T.2|2|T.1 => ||1.2||T.1|T.2 ; eq to (1->2 && 2->1) => (2 == 1)

the system would generate these rules, which are easier to implement...  

	|T||1|T.2|2|T.1 => 
	|T||1|1.2|2|1.2 => ; simple unification to find form that's reducible, using cofactor calculations?
	||1.2||T.1|T.2 ; paste-and-cut (iteration followed by deiteration), 

	|T||1|1.2|2|T.2 => 
	|T||1|1.2|2|1.2 => ; simple unification that finds common terms

PS:  Minimizing the unique terms makes more sense as a measure of 'simple' than the length of the expression.  
That's because when you think about the 'size' of the expression, the LE implementation can usually store 
expressions with fewer terms more efficiently than expressions that are 'shorter'.  
This is because, in a computer's memory, it's possible to reuse an expression as a pointer, 
which doesn't require the expression to be duplicated.  



