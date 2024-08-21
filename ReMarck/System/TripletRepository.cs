using System;
using System.Collections.Generic;
using System.Linq;
using ReMarck.System;

namespace ReMarck.System
{
    public class TripletRepository
    {
        public static TripletRepository FromFormula(string v)
        {
            throw new NotImplementedException();
        }

        //private Dictionary<Triplet> by

        private TripletRepository()
        {
        }

        void AddTriplet(Triplet triplet)
        {

        }

        /// <summary>
        /// Returns the set of Triplets in this repository that have no parents.
        /// In other words, a repository may contain several distinct formulas and 
        /// this method returns the root triplets of those formulas.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Triplet> Roots { get; private set;}
    }
}
