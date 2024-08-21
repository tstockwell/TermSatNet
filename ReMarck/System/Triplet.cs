using System;

namespace ReMarck.System
{
    public class Triplet
    {
        public object ToFormulaString()
        {
            throw new NotImplementedException();
        }

        public int Id {  get; private set; }
        public Triplet Antecendent {  get; private set; }
        public Triplet Consequent {  get; private set; }
    }
}