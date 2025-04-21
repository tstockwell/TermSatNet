# LE System Pseudo Code

The document contains all the pseudo code that describes the LE system.  

The system is a collection of static function.

The syntax used here is a kind of pseudo C# and LINQ.  

Many of the functions describe what would be LINQ operators in C#.  

## RHS, LHS

	Expression RHS(Expression E)
	Expression LHS(Expression E)

Returns the left-hand or right-hand sides of an expression

## HighVar

	Variable HighVar(Expression E)

HighVar returns the highest variable in a formula.  

## Vars

	Variable[] Vars(Expression E)

Vars returns an ordered list of all the unique variables in a given formula.  
In a normalized LE system this would be an enumeration of all variables from 0 to HighVar(E).  


## Flatterm

	Expression[] Flatterm(Expression E)

A flatterm is an array of an expressions' subterms, enumerated in a depth-first fashion, starting with the expression itself.  
This structure is an extremely useful way to represent expressions.  

### Example
(a (b (b c)))

Flatterm of (a (b (b c)))...  

subterm 		index	level
------------	-----	-------
(a (b (a c))) 	0		0
a (leftmost)	1       1
(b (a c))		2		1
b (leftmost)	3		2
(b c)			4		2
b				5		3
c				6		3

Length = 7 = Sizeof(Flatterm((a (b (b c)))))

Length	= (literal count * 2) - 1  
		= (operator count * 2) + 1  
		= flatterm.Count()

## Length
Let Length(E) => Flatterm(E).Count()

The *length* of an LE expression is defined as the length of the expressions' flatterm.  
Note: Expressions always have an odd length.  

## Level 
Level is a property of an expressions' subgraphs.  
Simply, a subgraphs' level is the number of parens that you need to cross to get to that subgraph.  


## Compare Function

Expressions are ordered.  
Compare(A, B) is a function that compares A and B with respect to their LE ordering and returns...  
	-1  : if A < B  
	0	: if A == B  
	1	: if A < B  

	bool Compare(Expression A, Expression B)
	{  
		// expressions that use lower variables is less complex
		if HighVar(A) < HighVar(B) then return -1  
		if HighVar(A) > HighVar(B) then returns 1  

		// Expressions with fewer terms are simpler than formulas with more terms.  
		if Length(A) < Length(B) then return -1  
		if Length(B) < Length(A) then return 1.  

		// expression with simpler left-hand sides are simpler
		if (LHS(A) < LHS(B)) return -1
		if (LHS(A) > LHS(B)) return 1

		// expression with simpler right-hand sides are simpler
		if (RHS(A) < RHS(B)) return -1
		if (RHS(A) > RHS(B)) return 1

		return 0; // A and B should be the exact same expression
	}

Examples..
Rule 1: T and F come before any other expressions
Rule 1: 1 comes before 2, comes before (1 2), comes before 3, comes before a
Rule 2: (a a) comes before (a (a a))
Rule 3: F before T, (1 (T 1)) comes before (1 (1 1)), comes before ((T 1) 1)
Rule 4: F comes before T

## Decrement
	
	Applies all the inference rules, in order, and returns the first reduction found.
	If no reduction found then returns null

## Increment  
	
	Applies all the inference rules, in order, and returns the first augmentation found.  
	If no reduction found then returns null

## ToNormalForm

Returns the canonical form of a given expression.

	Expression ToNormalForm(Expression E)
	{
		Let N = Decrement(E)
		if (N == null) return null;
		while(0 < Compare(N,E)) 
		{
			Let D = Decrement(E)
			if (D == null) return N
			N = D
		}
	}

## ToHigherForms

This function is the complement of ToNormalForm and  
returns an ordered enumeration of all expressions in the *iterative closure* of the given expression.  
The returned expressions are in increasing order.
The returned enumeration is finite and polynomial in size.

	
## Cofactoring Algorithm

	[Assert, E => IsCanonical(E), E must be canonical]
	Queryable<(E,S,R,C)> Cofactors(Expression E) 
	{
        // A variable can be forced to T or F by setting it to T or F.
        // So variables start with two groundings, one that compels the variable to T, 
        // and one that compels the formula to F.
        // Same for constants.

		// E[T<-T] == T and E[T<-F] == T
		if (E == T) return [(T,T,T,T),(T,T,F,F)]; 

		// E[F<-T] == F and E[F<-F] == F
		if (E == F) return [(F,F,T,T),(F,F,F,F)]; 

		// E[S<-T] == T and E[S<-F] == F, when S is a variable
		if (Length(E) == 1) return [(E,S,T,T), (E,S,F,F)]; 

		//-------------------------------

		// for f-groundings of either side, E has a t-grounding
		Let tFactors= 
			Cofactors(E.LHS)
				.Where(_ => _.C == F)
				.Select(_ => (E, _.S, _.R, T))
			.Concat(
				Cofactors(E.RHS)
				.Where(_ => _.C == F)
				.Select(_ => (E, _.S, _.R, T)))

		//-------------------------------

		// for common t-cofactors of both sides, E has a f-cofactor
		Let fFactors = 
			Join(
				Cofactors(E.RHS).Where(_ => _.R == T), 
				Cofactors(E.LHS), 
				(l,r) => l.S == r.S && l.C == T)
			.Select((l,r) => (E, _.S, _.R, F))

		//-------------------------------

		// For f-groundings of either side,  
		// the iterated version of the other side is also an f-grounding,  
		// and thus E has a t-grounding based on that derived f-grounding.  
		// Example: Given (1 (T 2)), since 1 is an f-grounding of the lhs,  
		//		and since thus rhs[T<-1] == (1 2) may be substituted for the rhs,  
		//		then (1 2) is also a t-grounding of (1 (T 2)) with a conclusion of (1 (1 2))
		Let iFactors= 
			Cofactors(E.LHS)
				.Where(_ => _.C == F)
				.Select(_ => (E.RHS, E.RHS[T<-_.S], T, (E.LHS E.RHS[T<-_.S]))

		// return the union of all the cofactor types
		return tFactors.Concat(fFactors).Concat(iFactors).Distinct();
	}

Example usage...

	Let normalE= ToNormalForm(E) // cofactors are only available for canonical expression
	Let fGroundings = Cofactors(normalE).Where(_ => _.C == F && _.R == F);
