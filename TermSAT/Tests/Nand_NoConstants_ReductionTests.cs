﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Immutable;
using System.Linq;
using TermSAT.Formulas;
using TermSAT.NandReduction;
using TermSAT.RuleDatabase;

namespace TermSAT.Tests;

/// <summary>
/// Test for the logic system NON (Nothing But Nand) where formulas consist of just variables and nand operators.  
/// No negation, no constants.  
/// The reason for adopting this system is that wildcard analysis subsumes *way* more of the rules 
/// generated for this system by the rule generator algorithm (knuth-bendix algorithm) than 
/// other logic systems.  
/// 
/// </summary>
[TestClass]
public class Nand_NoConstants_ReductionTests
{
    /// <summary>
    /// ||.1.1|.1.1	=> .1
    /// The 1st rule generated by [the rule generator without constants](TermSAT.NandReduction.Scripts.RunNandRuleGenerator).
    /// This rule **should** be subsumed by wildcard analysis but it turns out that 
    /// wildcard analysis *without constants* doesn't do it.
    /// WTF!?
    /// 
    /// Turns out that constants are required in order to complete proofs.  
    /// Here's the proof that reduces ||.1.1|.1.1 to .1.
    /// ||.1.1|.1.1
    /// => ||T.1|T.1    ; reduce children, |.1.1 => |T.1
    /// => |T|T.1       ; remove double negation, |T|T.1 => .1
    /// => .1
    /// I have not been able to find a proof of ||.1.1|.1.1 => .1 without constants.
    /// It seems that constants are required for wildcard analysis to work.
    /// In any case, it turns out that wildcard analysis is easy to patch 
    /// to make it compatible with a system without constants.  
    /// And I'd rather patch wildcard analysis than attempt to replace it, 
    /// since I like how easy it is to understand wildcard analysis.  
    /// 
    /// Constants were removed from formulas for a reason, by removing constants we avoid the 
    /// dead-ends produces by critical terms formed when using constants.
    ///     > Search the project for '|T||.1|T.2|.2|T.1 => ||.1.2||T.1|T.2'
    /// The Knuth-Bendix way of dealing with formulas like the previous one is to generate more rules.  
    /// Instead, I opted to reduce the system by removing constants, thereby removing critical terms 
    /// like this and their associated problems. 
    /// Put another way, I can show that the production system without constants is locally confluent 
    /// while the production system with constants is not.
    /// 
    /// So, it turns out that constants are required in order to complete proofs, 
    /// BUT, we don't want to keep them around as a proof proceeds, 
    /// because doing so will lead to the dead-ends associated with the critical terms that constants create.  
    /// 
    /// No worries though, we can patch wildcard analysis like so...
    /// All test reduction sequences are terminated with a proof step that replaces all T with .1 and all 
    /// F with |.1.1 where .1 is the test variable.  
    /// For example, wildcard analysis will reduce |.1|.1|.1.1 => |T.1.  
    /// Which, after replacing T with the test variable of .1 becomes |.1.1.
    /// So, the no-constant version of the reduction becomes |.1|.1|.1.1 => |.1.1.  
    /// 
    /// </summary>
    [TestMethod]
    public void ReduceFormula_TT_5()
    {
        var nonCanonicalformula = (Nand)Formula.GetOrParse("||.1.1|.1.1");
        var canonicalFormula = Formula.GetOrParse(".1");
        Assert.AreEqual(TruthTable.GetTruthTable(nonCanonicalformula).ToString(), TruthTable.GetTruthTable(canonicalFormula).ToString());
        var reducedFormula = NandReducer.Reduce(nonCanonicalformula);
        Proof proof = Proof.GetReductionProof(nonCanonicalformula);
        Assert.AreEqual(canonicalFormula, reducedFormula);
    }

    [TestMethod]
    public void ReduceFormula_TT_A()
    {
        var nonCanonicalformula = (Nand)Formula.GetOrParse("|.1|.1|.1.1");
        var canonicalFormula = Formula.GetOrParse("|.1.1");
        Assert.AreEqual(TruthTable.GetTruthTable(nonCanonicalformula).ToString(), TruthTable.GetTruthTable(canonicalFormula).ToString());
        var reducedFormula = NandReducer.Reduce(nonCanonicalformula);
        Proof proof = Proof.GetReductionProof(nonCanonicalformula);
        Assert.AreEqual(canonicalFormula, reducedFormula);
    }



    /// <summary>
    /// |.1|.1|.1.1	=> |.1.1
    /// The 1st rule generated by the rule generator that should be subsumed by wildcard analysis.
    /// 
    /// |.1|.1|.1.1	
    /// => |.1|.1|T.1
    /// => |.1|F|T.1
    /// => |.1T
    /// => |T.1
    /// 
    /// vs...
    /// 
    /// |.1|.1|.1.1	
    /// => |.1|.1|.1T
    /// => |.1|.1|.TT
    /// => |.1|.1F
    /// => |.1T
    /// </summary>
    [TestMethod]
    public void ReduceDoubleNegationTest()
    {
        var nonCanonicalformula = (Nand)Formula.GetOrParse("|.1|.1|.1.1");
        var canonicalFormula = Formula.GetOrParse("|.1.1");
        Assert.AreEqual(TruthTable.GetTruthTable(nonCanonicalformula).ToString(), TruthTable.GetTruthTable(canonicalFormula).ToString());
        var reducedFormula = NandReducer.Reduce(nonCanonicalformula);
        Proof proof = Proof.GetReductionProof(nonCanonicalformula);
        Assert.AreEqual(canonicalFormula, reducedFormula);
    }
}
