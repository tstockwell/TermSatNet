using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace TermSAT.Common
{
    /// <summary>
    /// Defines an immutable string of objects of the same type.
    /// I kinda wish that C# defined a marker interface for immutability so that I had a way to define 
    /// __sequences and their items as immutable.
    /// </summary>
    public interface ISequence<TItem> : IEnumerable<TItem>
        where TItem : IComparable<TItem>, IEquatable<TItem> 
    {
        public TItem this[int index] { get; }

        public int Length { get; }
    }




}
