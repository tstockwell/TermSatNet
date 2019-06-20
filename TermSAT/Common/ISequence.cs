using System;
using System.Collections;
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

    /// <summary>
    /// ISequence adapter interface for strings.
    /// </summary>
    public class StringSequence : ISequence<char>
    {
        public readonly string Value;

        public StringSequence(string value) { Value= value; }

        public char this[int index] => Value[index];
        public override string ToString() => Value;
        public int Length => Value.Length;
        public int CompareTo(ISequence<char> other) => Value.CompareTo(other.ToString());
        public bool Equals(ISequence<char> other) => Value.Equals(other.ToString());
        public IEnumerator<char> GetEnumerator() => Value.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => Value.GetEnumerator();
    }



}
