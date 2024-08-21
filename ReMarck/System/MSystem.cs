using ReMarck.System;
using System;
using System.Collections.Generic;

namespace ReMarck.System
{
    /// <summary>
    ///     MSystem implements the various rules that make up the StalMarck method.
    ///     An MSystem will reduce a set of Triplets until the associated formula is proved to 
    ///     be satisfiable or a contradiction.
    /// </summary>
    /// 
    /// <ImplementationNotes>
    /// 
    ///     MSystem starts multiple threads that make changes to the internal set of Triplets.
    ///     These threads will eventually 'commit' changes.
    ///     Commits are optimistic, that is, when a thread commits changes the commit can be rejected and the 
    ///     thread must either abandon it's work or repeat it.
    ///     MSYstem implements a kind of transactional memory that enforthat makes it possible to identify conflicts and 
    ///     enforce sequentiallity.
    ///     Transactions are atomic, consistent and isolated.
    ///     
    /// </ImplementationNotes>
    public class MSystem
    {
        public TripletRepository Triplets {  get; private set; }

        public MSystem(TripletRepository triplets)
        {
            Triplets= triplets;
        }

        public void ApplyReductionRules()
        {
            throw new NotImplementedException();
        }

        public void ApplySimpleRules()
        {
            throw new NotImplementedException();
        }
    }
}