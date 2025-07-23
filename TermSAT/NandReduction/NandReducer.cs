using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using TermSAT.Formulas;
using TermSAT.RuleDatabase;

namespace TermSAT.NandReduction;

/// <summary>
/// 
/// Implements the NandReducer.Reduce method, which, for a given Nand formula, returns an equivalent formula in canonical form.  
/// A 'nand formula' is a formula that only uses nand operators, the constants T an F, and numbered variables.
/// 
/// This class also maintains a global collection of 'proofs' that are used to reduce formulas.  
/// This collection is built out at runtime as formulas are reduced, causing reductions to be discovered, 
/// causing proofs to be created and/or updated.  
/// For every formula a global proof is maintained. 
/// A proof specifies...
///     - Reduction: an atomic Reduction to a simpler form of a formula based on a rule.
///         This nextReduction must be populated and never changes.  
///         If the formula is canonical then NextReduction will have a description like "formula is canonical".  
///     - Result: the simplest known form of the formula. 
///         Basically a slot for memoizing the last value found for ReducedFormula.
/// When Result is also known to be canonical then we say that the proof is complete.
/// Note that proofs of equivalent formulas form a kind of skip list to the simplest, canonical form of the formula.  
/// 
/// // todo: Use proofs as rules when reducing formulas.
/// NandReducer also uses proofs as rules when reducing formulas.
/// For example, suppose the formula |||.1.2|.3|T.2||.2.3|.1|T.2 is reduced to |.1.3.
/// Going forward, NandReducer will attempt to use the rule |||.1.2|.3|T.2||.2.3|.1|T.2 => |.1.3 to reduce 
/// all other formulas before attempting to discover new reductions.
/// That is, any substitution instance of |||.1.2|.3|T.2||.2.3|.1|T.2 can be immediately reduced 
/// using the previously built proof.
/// Basically reusing the work we did to reduce |||.1.2|.3|T.2||.2.3|.1|T.2 to reduce another formula.
/// NandReducer can do this efficiently, see TermSAT.Expressions.InstanceRecognizer.
/// 
/// </summary>
public static class NandReducer
{
   

}