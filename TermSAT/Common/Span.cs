using System;
using System.Collections.Generic;
using System.Text;

namespace TermSAT.Common
{
    /**
     * An immutable object the denotes a span of items with an array or list
     */
    public class Span
    {
        public Span(int index, int count)
        {
            Index= index;
            Count= count;
        }

        public int Index { get; private set; }
        public int Count { get; private set; }
    }
}
