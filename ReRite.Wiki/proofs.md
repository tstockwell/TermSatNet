# Proofs

Need a proof that RR proof trees ,as stored in the RR database, are a polynomial function of the root formula size.  

# Proof Process
RR proofs works by enhancing formulas with a (dynamic) list of the most relevant terms in that formula.
A relevant term in a formula is a term that can force the formula to assume a specific value,  
by replacing all instances of the term with a constant.
A formulas' list of relevant terms is recursive, it includes all relevant terms from known substit

RR uses relevant terms to deduce new reductions, and updates rele
by creating a 'relevance tree' for all sub-formulas in a formula, 
and using that tree to discover reduction opportunities.

The relevance tree is a tree of reductions between formulas where each formula node is enhanced 
with a list of the most relevant terms in that formula. 
RR 'deduces' new reductions 

## RelevantTerm
A relevant term in a formula is a term that can force the formula to assume a specific value,  
by replacing all instances of the term with a constant.

RelevantTerm(long FormulaId, long TermId, bool TermValue, bool FormulaValue)
When all instances of Term in Formula are replaced with the constant TermValue then the formula has the value FormulaValue.  



- 

# Induction

Let's try using induction on formula size...
Let P(N) be the size of the proof tree for formulas of length N.

For length == 1, constants & variables, P(1) == 1.
	> The description on the associated reduction is "canonical formula".

For length 0 < N, where N is odd, P(N) = 2*P((N-1)/2)
 => 1 + (2 * 1) + (2 * 3) + (2 * 6) + (2 * 12)
 => 1 + 3 + 6 + 12 + 24 + 48 + 

