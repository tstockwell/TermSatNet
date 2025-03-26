# Collatz Conjecture

The idea here is to test what I think I know about solving logic problems 
by solving a well-known open problem that is considered to be exceptionally difficult, 
that can also be expressed in existential first-order logic.  

So, a perfect opportunity to try out the UNO algorithm.

In this document I attempt to answer the Collatz Conjecture by expressing the 
problem as a an extended OLE expression and then minimizing it using the UNO algorithm.  

I figure that I could just prove the conjecture true or false, 
but if false then it would be more awesome if I could find a satisfying solution.  




## POLE formulation of Collatz 
Here's a formula in first-order logic from [Three Variations on a Theme by Collatz;Levesque](https://www.cs.toronto.edu/~hector/Papers/Collatz/variations.pdf) 
written in extended OLE

A(x)A(y)A(z)
[	
	Q(s(0), 0, 0) 
	&&	Q(xoo) -> Q(ox z) 
	&&	Q(s(s(x)) oo) -> Q(s(o) y x) 
	&&	Q(x s(y) s(s(s(z)))) -> Q(s(s(x)) y z)
]

Replace s function with propositions...

A(x)A(y)A(z)
[	
	E(s0)
	[
		Q(s0, 0, 0) 
		&&	Q(x, 0, 0) -> Q(0, x, z) 
		&&	Q(s(s(x)), 0, 0) -> Q(s0, y, x) 
		&&	Q(x, s(y), s(s(s(z)))) -> Q(s(s(x)), y, z)
	]
]
where s0 == s(0), sx == s(x)




UNO does not work with universal formulas, we need to convert to an existential expression...

[T 
	E(x)E(y)E(z) 
	[T 
		[	
			Q(s(o) oo) 
			||	Q(xoo) -> Q(ox z) 
			||	Q(s(s(x)) oo) -> Q(s(o) y x) 
			||	Q(x s(y) s(s(s(z)))) -> Q(s(s(x)) y z)
		]
	]
]

Remove existential quantifiers and the outer double cut to get an existential first order expression...
[	
	Q(s(0), 0, 0) 
	||	Q(x, 0, 0) -> Q(0, x, z) 
	||	Q(s(s(x)), 0, 0) -> Q(s(0), y, x) 
	||	Q(x, s(y), s(s(s(z)))) -> Q(s(s(x)), y, z)
]

remove function s by replacing 




## References

### Variations
[Three Variations on a Theme by Collatz;Levesque](https://www.cs.toronto.edu/~hector/Papers/Collatz/variations.pdf)

Provides a simpler formulation of the Collatz conjecture in first-order logic.  
I converted this to existential first order logic, then to OLE.  
