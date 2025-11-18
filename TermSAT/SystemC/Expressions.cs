
using System;
using System.Collections.Generic;
using System.Linq;
using TermSAT.Formulas;

namespace TermSAT.SystemC
{

    abstract public record Expression
    {
        abstract public int Length { get; }

        /// <summary>
        /// In System C variables are numbered, starting at 0
        /// This array is an ordered list of the variable numbers.
        /// </summary>
        abstract public int[] UniqueVariables { get; }

        public int CompareTo(Expression other)
        {
            if (other == this)
                return 0;

            if (this.Equals(other))
                return 0;

            // normalized (variables numbered from 1 to n) expressions with less variables 
            // are simpler than expressions with more variables
            {
                var thisVarCount = this.UniqueVariables.Count;
                var otherVarCount = other.UniqueVariables.Count;
                if (thisVarCount < otherVarCount)
                    return -1;
                if (otherVarCount < thisVarCount)
                    return 1;
            }

            // shorter formulas are simpler than longer formulas
            if (Length < other.Length)
                return -1;
            if (other.Length < Length)
                return 1;

            // constants are simpler than any other formula, T comes before F in this world.
            if (this is Constant constantThis)
            {
                if (other is Constant constantOther)
                {
                    if (constantThis.Value)
                    {
                        if (constantOther.Value)
                        {
                            return 0; // both true
                        }
                        return -1; // this = true, other = false
                    }
                    if (constantThis.Value == constantOther.Value)
                    {
                        return 0; // both false
                    }
                    return 1; // this is false, other = true
                }
                return -1; // this is constant, other is not a constant
            }

            if (other is Constant)
            {
                return 1;
            }

            if (this is Variable)
            {
                if (other is Variable)
                {
                    return (((Variable)this).Number < ((Variable)other).Number) ? -1 : 1;
                }
                return -1;
            }

            if (other is Variable)
            {
                return 1;
            }

            var nandThis = (Context)this;  // always true
            if (other is Context nandOther)
            {
                {
                    var lhsCompare = nandThis.Left.CompareTo(nandOther.Left);
                    if (lhsCompare != 0)
                    {
                        return lhsCompare;
                    }
                }

                {
                    var rhsCompare = nandThis.Right.CompareTo(nandOther.Right);
                    if (rhsCompare != 0)
                    {
                        return rhsCompare;
                    }
                }

                return 0;
#if DEBUG
                throw new TermSatException($"Since these formulas are not equal, we shouldn't get here");
#endif
            }

            return 1; // this is a context and the other is not
        }
    }

    public record Variable : Expression
    {
        override public int Length => 1;
        override public IEnumerable<int> UniqueVariables { get; }
        public int Number { get; }

        public Variable(int value) 
        {
            Number= value;
            UniqueVariables = new List<int>() { value };
        }
    }

    public record Constant : Expression
    {
        override public int Length => 1;
        override public IEnumerable<int> UniqueVariables { get; }
        public bool Value { get; }
        public Constant(bool value)
        { 
            Value = value;
            UniqueVariables = new List<int>();
        }
    }

    public record Context : Expression
    {
        public Expression Left { get; }
        public Expression Right { get; }
        override public int Length { get; }


        private IEnumerable<int>? _uniqueVariables= null;
        override public IEnumerable<int> UniqueVariables
        {
            get
            {
                if (_uniqueVariables is null)
                {
                    _uniqueVariables = Left.UniqueVariables.Concat(Right.UniqueVariables).Order().Distinct();
                    _uniqueVariables = Left.UniqueVariables.Concat(Right.UniqueVariables).Distinct().Order();

                }
                return _uniqueVariables!;
            }
        }
        public Context(Expression Lhs, Expression Rhs) 
        {
            Left = Lhs;
            Right = Rhs;
            Length = Left.Length + Right.Length + 1;
        }
    }
}

