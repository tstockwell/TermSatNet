using System;
using System.Collections.Generic;
using System.Text;

namespace TermSAT.Formulas
{
    public class TermSatException : Exception
    {
        public TermSatException(string message) : base(message) { }
    }
}
