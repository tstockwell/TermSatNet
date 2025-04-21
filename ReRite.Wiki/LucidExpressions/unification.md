# Unification

This document describes the cofactor unification algorithm that LE uses to find common cofactors in the two sides of an expression.  

Given an expression E, with tgf-cofactors of its sides, tgf-L and tgf-R.  
The concept of unification means, using the terms in E...  
	finding substitutions for the empty spaces in tgf-L and...  
	finding substitutions for the empty spaces in tgf-R...  
	such that the substitutions create a common tgf-cofactor, and thus a fgf-cofactor of E.  

Example: The expression ((1 (T 2)) (2 (T 1))) has the tgf-cofactors (T 2) and (T 1), 
which can be unified to a common cofactor (1 2) using the substitutions tgf-L[T<-1] and tgf-R[T<-2].

The unification algorithm deserves special attention because not all unification algorithms are polytime.  
The first unification algorithm by Robinson was exponential, quadradic and linear unification algorithms came later.  
It's important that LE's version of unification is not exponential, and so is given special attention.  

## Overview : Unification of cofactors can identify reduction opportunities.  

This is an example of an expression that can't be reduced by directly computing cofactors of the terms in the expression.  

	|T||1|T.2|2|T.1 => ||1.2||T.1|T.2


Here are some of the cofactors calculated for the above expression, note the last two...

	Cofactors
	(S	==	R) -> (	E ==	C)
	2		F		2		F
	2		T		2		T
	2		F		(T 2)	T
	2		T		(T 2)	F
	(T 1)	F		(2 (T 1))	T	; rhs of (2 (T 1))
	(T 2)	F		(1 (T 2))	T	; rhs of (1 (T 2))


To minimize this expression we need to find an fgf-cofactor of ((1 (T 2)) (2 (T 1))).  
But directly computing the cofactors of al the terms in the expression does not yield the required fgf-cofactor.  
But if we can somehow find a common tgf-cofactor of both sides then that cofactor is also a fgf-cofactor of the parent expression.  
	- tgf-cofactors of left side are 1, and (T 2)  
	- tgf-cofactors of right side are 2, (T 1)

We *could* use iteration to expand both sides, which would yield expressions with a common cofactor.  
However, we'd have to *search* for iterations that work, and there are a potentially exponential # of permutations.  

Instead, we can *unify* the tfg-cofactors, (T 1) and (T 2), to find a common cofactor, in polytime.  
Unification of (L 2) and (R 1) yields [L=1, R=2] and the most-general-unifier is (1 2)
Applying the unifying substitution to ((1 (T 2)) (2 (T 1))) and reordering yields ((1 (1 2)) (2 (1 2))).  
Applying the cofactor substitution (1 2)<-T to ((1 (1 2)) (2 (1 2))) yields ((1 T) (2 T)).  
And therefore this proof using iteration and deiteration...  

	(T ((1 (T 2)) (2 (T 1)))) =>		; axiom  
	(T ((1 (1 2)) (2 (1 2)))) =>		; iteration(s)
	((1 2) ((1 (1 2)) (2 (1 2)))) =>	; iteration, bc (1 2) is common tgf-cofactor
	((1 2) ((1 T) (2 T))) =>			; deiteration  
	((1 2) ((T 1) (T 2)))				; reordering  

...can be constructed directly using unification, without searching, like so...  

	(T ((1 (T 2)) (2 (T 1)))) =>		; axiom  
	((1 2) ((1 T) (2 T))) =>			; unification,  
		where fgf-cofactor of ((1 (T 2)) (2 (T 1))) = 
			S = (1 2), 
			SUBSTITUTIONS...
				(T 2)[T<-1]
				(T 1)[T<-2]
	((1 2) ((T 1) (T 2)))				; reordering  

The 2nd proof is preferable to the first because each line in the 2nd proof is a polytime reduction of the previous line.  
Whereas in the first proof the expression is first expanded and then reduced.  
Always reducing the expression in every step makes it possible, at least for the author, to prove LE's time complexity.  

## Algorithm

### Uniterms (Unified Flatterms)

A [flatterm](lucid-expressions.md#flatterm) is an array of an expressions' subterms, enumerated in a depth-first fashion, starting with the expression itself.  

A unified flatterm, or uniterm, is similar to a flatterm 
but where each element in the flaterm is an array of terms instead of a single term, 
and where each array of terms represents a set of valid substitutions for that term.

A uniterm is a way of recording (or representing) the iterations and deiterations applied to an expression.  

LE is only concerned with the uniterms of mostly-canonical expressions (and therefore canonical expressions too).  

A uniterm is called *mostly complete* when all possible iterations/deiterations to/from both sides of U are represented.  
That is, a mostly-complete uniterm represents all iterations/deiterations that are possible using just the 
terms in a mostly-canonical expression.

A uniterm is *complete* when it also includes all possible iterations/deiterations computed from cofactors .  

A more rigorous definition of mostly-complete uniterms...

	for any term I in the flatterm of a mostly-canonical expression E...
		for every cut C that contains I, let O be the side of C opposite the side that contains I...
			for every fgf-cofactor of O
				If I == the cofactor term then T is a valid substitution for I.
				If T is a valid substitution for I then the cofactor term is a valid substitution for I.

#### Examples
(T a)

Flatterm of (T a) = {(T )}
subterm 		index	level
------------	-----	-------
(T a)			0		0
T			 	1		1
a				2       1

Uniterm of (T a)...  
subterm 		index	level
------------	-----	-------
[(T a)]			0		0
[T,a]		 	1		1
[a]				2       1

Uniterm of (1 (T 2))...  
subterm 		index	level
------------	-----	-------
[(1 (T 2))]		0		0
[1]				1		1
[(T 2)]			2		1
[T,1,2]		 	3		2
[2]				4       2

#### Cofactor Unification

*cofactor unification* is LE's procedure for finding common cofactors of two given expressions.  
LE discovers common tgf-cofactors in expressions and uses them to apply iteration/deiteration 
rules that reduce expressions.  

Since rules are ordered, expressions are mostly reduced in the following pattern...
- applying ordering, double-cut elimination, and iteration/deiteration rules until the expression is mostly-canonical.  
- Using cofactor unification, ordering, and double-cut elimination to reduce mostly-canonical expressions to canonical expressions.  

LE implements these steps by...
- First, building a mostly-canonical uniterm that represents a mostly-canonical expression.
- Using cofactor unification to extend a uniterm with new cofactors 

Cofactor unification takes at most M*N steps to find any unifying substitutions, 
where M and N are the # of substitutions 

Let E be an expression E of the form (L R) where E has no fgf-cofactors but L and R both have a set of tgf-cofactors, 
denoted by TGroundingFCofactors(L) (abbrv as TGFC(L)) and TGFC(R).  
The cofactor unification problem is to unify one of the terms in TGFC(L) with one of the terms in TGFC(R) 
to produce a common tfg-cofactor of E, and this an fgf-cofactor of E.

To unify any two given tgf-cofactor terms of a mostly-canonical expression E given a uniterm U of E...  
choose terms in U that unify the two given terms according to the following algorithm...


	Let E be a mostly-canonical expression of the form (L R)
	Let lTGFC and rTGFC be tfg-cofactors of LHS(E) and RHS(E) respectively.
	Let lSubstitutes and rSubstitutes be lists of valid substitutes for lTGFC and rTGFC.  
	foreach term I in Flatterm(LHS(E))
		If LHS(I) contains lTGFC then add lTGFC[T<-RHS(I)] to lSubstitutes. 
		If RHS(I) contains lTGFC then add lTGFC[T<-LHS(I)] to lSubstitutes. 
	Conversly...
	foreach term I in Flatterm(RHS(I)) 
		If LHS(I) contains rTGFC then add rTGFC[T<-RHS(I)] to rSubstitutes. 
		If RHS(I) contains rTGFC then add rTGFC[T<-LHS(I)] to rSubstitutes. 
	Finally...
	If lSubstitutes and rSubstitutes contain a common term then that term is a common tgf-cofactor 
	of both LHS(E) and RHS(E).

This algorithm basically builds a partial uniterm, just only all instances of a given term.  


Take 2...

Assume that 

	Let E be a mostly-canonical expression with tfg-cofactors lTGFC and rTGFC of LHS(E) and RHS(E) respectively.  
	Let lSubstitutes and rSubstitutes be lists of valid substitutes for lTGFC and rTGFC.  
Build complete uniterm....

	Let Substitutes list of tuples (int i, Expression substitute)
	Let head = 0
	Foreach i in Range(0, Length(E) - 1)
		Let I = Flatterm(E)[i]
		If I == T Then
			Foreach j in Range(head, i)
				
			EndFor
		Else
		EndIf 
		If LHS(I) contains lTGFC then add lTGFC[T<-RHS(I)] to lSubstitutes. 
		If RHS(I) contains lTGFC then add lTGFC[T<-LHS(I)] to lSubstitutes. 
	Conversly...
	foreach term I in Flatterm(E) 
		If LHS(I) contains rTGFC then add rTGFC[T<-RHS(I)] to rSubstitutes. 
		If RHS(I) contains rTGFC then add rTGFC[T<-LHS(I)] to rSubstitutes. 
	Finally...
	If lSubstitutes and rSubstitutes contain a common term then that term is a common tgf-cofactor 
	of both LHS(E) and RHS(E).


Let Synonyms(E) return a set of terms that may validly replaced the empty spaces (T's) in E.  
Examples... Slots((T 2)) = [2], Symbols((1 (T 2))) = [2,1]
Let S be a unifying set of substitutions, SUBS(term) into E (E[x<-y]) 
such E become E' where tgf-L' and tgf-R' are equal, or unified.  



The LE unification problem reduces to a 'standard' unification problem of just two variables.


Proof...  
Given an expression E, with tgf-cofactors of its sides, tgf-L and tgf-R.  
The concept of unification means, using the terms in E...  
	finding substitutions for the empty spaces (T'S) in tgf-L and...  
	finding substitutions for the empty spaces in tgf-R...  
	such that the substitutions create a common tgf-cofactor, and thus a fgf-cofactor of E.  
Given an expression E to reduce  
