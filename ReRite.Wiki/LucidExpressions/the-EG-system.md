# Existential Graphs

This document discusses existential graphs.

Among other things, OpenEG, a simple textual notation, suitable for expressing graphs in a Markdown document, is developed for EG.  


## OpenEG Notation

Because this is the 21st century and everything's digital, except for paper and pencil, there's a need for a textual notation for expressing existential graphs.  
Peirce would not be happy.  
But for pragmatic reasons we need a textual syntax for the EG system.

### Empty Space and Empty Cut    

- The symbol T is an expression that represents an empty space, that can possibly be filled in later.
	> Semantically, T means "true", ie T always has the truth value 1.  

- The symbol F is an expression that represents an empty cut.
	> Semantically, F means "false", ie F always has the truth value 0.  

### Symbols    
- Symbols are expressions represented by any combination of lower case letters, numbers, and embedded hyphens.

	> Examples: man, hot-dog, tvc-15

- Expressions may be bounded by parentheses and separated by a space  

    > Examples; (T a), (a b c d), and (wubba lubba (T (dub dub)))

OpenEG uses parentheses to represent a cut in an EG.  
The parentheses visually group elements together in a way that's similar to existential graphs.  

### Sheet Of Assertion
The level-0 structure in EG is usually the *Sheet Of Assertion*, 
which is just a conjunction of graphs.  
In OpenEG, the SOA is written like so...

    ((graph-1, graph-2, graph-3, ...))

which has the boolean semantics...

    graph-1 && graph-2 && graph-3 ...

SOAs are conjunctions.
They are often used to mark the beginning of a proof,  
where the SOA lists the assumptions,  
but they can also be used to express conjunctions in subgraphs.

### Semantics

In terms of boolean logic, the elements of an EG graph are interpreted like so...
- T == TRUE()
- F == FALSE()
- (a b) == NOT(AND(a,b))

Example: (wubba lubba (T (dub dub))) 
==> NOT(AND(wubba, lubba, NOT(AND(TRUE(), NOT(AND(dub,dub))))))
==> NOT(AND(wubba, lubba, dub)))

