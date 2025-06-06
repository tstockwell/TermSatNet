﻿using System;
using System.Collections.Generic;

namespace TermSAT.Formulas
{
    /// <summary>
    /// 
    /// This source module defines an API that represents Expressions as an array of all the 
    /// subformulas in the formula, ordered from the leftmost subformula (the formula itself) to the rightmost 
    /// subformula (the last constant or variable that appears in the formula).
    /// The name FlatTerm comes from the chapter on Term Indexing in the book 
    /// 'Handbook of Automated Reasoning',  by R Sekar, V Ramakrishnan, and Andrei Voronkov.
    /// 
    /// The particular ordering implemented by flatterms is known as a 
    /// <a href='https://en.wikipedia.org/wiki/Depth-first_search#DFS_ordering'>DFS ordering</a>, or a 
    /// <a href='https://en.wikipedia.org/wiki/Prefix_order'>prefix ordering</a>.
    /// 
    /// FlatTerms are useful for constructing indexes of sets of formulas.
    /// The indexes built from FlatTerms make it possible to quickly find substitution instances and unifications 
    /// of a given formula in a large set of formulas.
    /// 
    /// Also, FlatTerms can be easier to use that a visitor API when implementing formula reduction algorithms and such.
    /// 
    /// Example:
    /// Given the formula...
    ///     *a*-bc
    /// ...the associated DFSOrdering is a list of a the sub-formulas from right to left...
    ///     { *a*-bc, a, *-bc, -b, b, c }
    ///  The above list of formulas is exactly the same enumeration of formulas that would be obtained 
    ///  by navigating the original formula in a depth first enumeration (enumerating antecedents before 
    ///  consequents).
    ///  
    ///  Note that the original formula from which the sequence is derived can be reconstructed by 
    ///  examining the types of the sub-formulas and the variables in the sequence.
    ///  The types of the sub-formulas looks like this...
    ///     { implication, variable(a), implication, negation, variable(b), variable(c) }.
    ///  Note that by replacing sub-formulas with the symbol that represents their type, we 
    ///  can reconstruct the original formula from which the sequence is derived...
    ///     { *, a, *, -, b, c }
    ///     
    ///  Thus, flatterms are a kind of string that represents a formula.
    ///  
    ///  There are many indexing techniques used in SAT solvers that are based on various string 
    ///  indexing and matching techniques, and flatterms give us a way to represent 
    ///  formulas as strings in a convenient way.
    ///
    /// </summary>
    public interface IFlatTerm<TItem> : IEnumerable<TItem>
        where TItem : IComparable<TItem>, IEquatable<TItem>
    {
        public TItem this[int index] { get; }

        public int Length { get; }
    }




}
