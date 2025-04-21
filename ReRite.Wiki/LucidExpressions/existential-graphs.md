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


### Syntax    

#### Base32-Hex-Lower
OpenEG uses an encoding that can be characterized as [Base32 encoding with an Extended Hex, Lower Case, Alphabet](https://www.rfc-editor.org/rfc/rfc4648#page-10). 
This encoding is used because it has the property that sort order is preserved when encoding/decoding.  
This property isn't relevant to existential graphs but it is to the LE system.  

Symbols use the Base32, lower-case alphabet with embedded formatting.  
	> By convention; always start with lower case, other characters translated to slashes. 
	> Examples: one, 2, home/lucid, tvc15, one+two, cat-dog
	> No parentheses are allowed in symbols

Expressions are constructed by wrapping symbols and other expressions in a *cut*.  

    > Examples; (T a), (a b c d), and (wubba lubba (T (dub dub)))

OpenEG uses parentheses to represent a cut in an EG.  
The parentheses visually group elements together in a way that's similar to existential graphs.  

### Sheet Of Assertion
The top-level structure in EG is the *Sheet Of Assertion*, 
which is just a conjunction of graphs, 
that represent the axioms of a system.  

In OpenEG, the SOA is written like so...

    ((graph-1, graph-2, graph-3, ...))

which has the boolean semantics...

    graph-1 && graph-2 && graph-3 ...

SOAs are conjunctions.
They are often used to mark the beginning of a proof,  
where the SOA lists the assumptions,  
but they can also be used to express conjunctions in subgraphs.


### Conversions From Propositional Calculus to OpenEG...
Conversions From boolean expressions to Lucid expressions...

    - NAND: a ~& b   => (A b)
    - NOT:  ~a       => (T a)
    - AND:  a && b   => (T (a b))
    - IMPL: a -> b   => (a (T b))
    - OR:   a || b   => ((T a) (T b))
