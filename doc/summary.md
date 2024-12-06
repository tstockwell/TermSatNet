TermSAT is a server-based SAT solver that reduces nand-based propositional formulas to their canonical form, 
TermSAT reduces formulas using rewrite rules that have been previously discovered and saved in a trie-structured rule database.  
TermSAT reduces formulas in polynomial time as a function of the length of the formula (you read that correctly).  

The catch is that a TermSAT server cannot **guarantee** that it can completely reduce formulas above its **complexity rating**.  
A databases' complexity rating is the ID of the last formula in the database, which is also the largest ID in the database.  
A databases' **completeness rating** is the maximum number of variables in a formula for which the database is **guaranteed** 
to be able to reduce the formula to its canonical form.
Complexity ratings grow linearly while completeness ratings are logarithmic.

The open source version of TermSAT is limited to a database size that's reasonable to download from GitHub.
The open-source version of TermSAT has a completeness rating of 1000 variables.  
That doesn't mean that it's only capable of solving formulas with 1000 variables, 
it means that the open source version is capable of solving the hardest, thorniest, most complex problems of up to 1000 variables.  
The open source version can reliably solve many difficult, large problems with millions of variables.
Developers are also able to extend the open source rule database on their own, 
the RuleSAT project provides tools for building your own database.

The commercial version of TermSAT has a completeness rating that is magnitudes of order greater than the open source version and 
is capable of solving the most difficult problems on the planet.
Similarly to how it takes computational effort (and therefore money) to build a BitCoin, 
it takes computational effort to build a TermSAT database.  
That's why there is a commercial version, as it takes some serious resources to keep expanding the database.  

Table of Contents
[Introduction](how-rulesat-works.md): How RuleSAT works.
[Formulas](formulas.md)
[Proofs](proofs.md)
[RuleSAT is O(nE3) and P=NP](complexity.md)

