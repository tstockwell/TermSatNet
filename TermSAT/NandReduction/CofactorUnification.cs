using Microsoft.Extensions.FileSystemGlobbing;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;
using System.Security.Claims;
using System.Security.Cryptography;
using TermSAT.Formulas;
using System.Linq.Expressions;

namespace TermSAT.NandReduction;



/// <summary>
/// Unifies cofactors, if possible.  
/// This algorithm is used to identify common cofactors of expressions.  
/// 
/// This algorithm....
/// - is based on the algorithm presented in Linear Unification; Patterson, Wegman, 1976
/// - is designed to avoid the exponential complexity of the Robinson unification algorithm.  
/// - is designed to work with LE's expression and cofactor structures.  
/// </summary>
public static class CofactorUnification
{
    public class UnificationState
    {

    }
    //public static void TryUnify(Formula lhs, Formula rhs)
    //{
    //    var state = new UnificationState();

    //    //Create undirected edge(u, o).
    //    state.AddEdge(lhs, rhs);

    //    //While there is a function node Y, Finish(T).
    //    //While there is a variable node r, Finish(r).
    //    // this enumerates the lhs and then the rhs in a top-down, depth-first fashion.
    //    foreach (var term in Nand.NewNand(lhs,rhs).AsFlatTerm())
    //    {
    //        if (!Finish(term))
    //    }
    //    return state.TryUnify(lhs, rhs);
    //}

    public static void Unify(this UnificationState state, Expression lhs, Expression rhs)
    {
        //Begin
        //Create undirected edge(u, o).
        //While there is a function node Y, Finish(T).
        //While there is a variable node r, Finish(r).
        //Print(“UNIFIED”) and halt.
        //Procedure Finish(Y)
        //Begin
        //If pointer(r) defined then print(“FAIL: LOOP”) and halt
        //else pointer(r) := r
        //Create new pushdown stack with operations Push(*) and Pop.
        //Push(r).
        //While stack nonempty do
        //            begin
        //            s := Pop
        //If r, s have different function symbols then print(“FAIL : CLASH”) and halt.
        //While s has some father t do Finish(t).
        //While there is an undirected edge(s, t) do
        //            begin
        //            If pointer(t) undefined then pointer(t) := r
        //If pointer(t) # t then print (“FAIL : LOOP”) and halt.
        //Delete undirected edge(s, t).
        //Push(t).
        //end.
        //Ifs # r then
        //begin
        //If s is variable node, print(s, “+“, r).
        //If s is a function node with outdegree q > 0
        //then create undirected edges { jth son(r), jth son(s)) 1 1 < j < q}
        //        Delete s and directed arcs out of s
        //end.
        //end.
        //Delete node r and all directed arcs out of r.
        //End of Finish.
        //End of Algorithm C.
    }
}
