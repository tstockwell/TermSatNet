using System;
using System.Collections.Generic;
using System.Text;

namespace TermSAT.Common
{
    /// <summary>
    /// Defines an immutable string of objects of the same type.
    /// I kinda wish that C# defined a marker interface for immutability so that I had a way to define 
    /// sequences and thier items as immutable.
    /// </summary>
    public interface ISequence<TItem> : IComparable<ISequence<TItem>>, IEquatable<ISequence<TItem>>, IEnumerable<TItem>
        where TItem : IComparable<TItem>, IEquatable<TItem> 
    {
        TItem this[int index] { get; }

        int Length { get; }
    }
}
