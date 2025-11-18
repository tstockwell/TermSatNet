using System;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;

namespace TermSAT.SystemC;


/// <summary>
/// An CGraph is a kind of e-graph for computing the *congruence closures* of expressions, 
/// and tracking the lengths, variable counts, and cofactors of those expressions.  
/// 
/// If you think of the union-find data structure as a method for tracking the 
/// 
/// Cofactors are computed when expressions are added because the cofactors 
/// of a context are easily computed from the cofactors of it's terms.  
/// 
/// This implementation of an e-graph was initially based on this article...  
/// https://cp-algorithms.com/data_structures/disjoint_set_union.html
/// </summary>
public class CGraph
{
    /// <summary>
    /// In an x-graph the id of F is always 0
    /// </summary>
    public static int ID_FALSE = 0;
    /// <summary>
    /// In an x-graph the id of T is always 1
    /// </summary>
    public static int ID_TRUE = 1;

    private int LastId = -1;
    private int GetNextId() => Interlocked.Increment(ref LastId);

    private List<int> Parent= new();
    private Dictionary<int,Expression> Expressions {  get; } = new();
    private Dictionary<Expression, int> Ids { get; } = new();


    /// <summary>
    /// Creates a graph and initializes it with the constants T and F
    /// </summary>
    public CGraph()
    {
        Parent.EnsureCapacity(100);

        AddConstant(true);
        AddConstant(false);
    }

    /// <summary>
    /// Return the Id of the simplest expression that's equivalent to a given expression
    /// </summary>
    /// <param name="expressionId"></param>
    /// <returns></returns>
    public int FindClass(int expressionId)
    {
        int id = expressionId;
        int parent = Parent[id];
        if (id == parent)
            return id;

        List<int> path= new() { id };
        while (true)
        {
            id = parent;
            parent = Parent[id];
            if (id == parent)
                break;
            path.Add(id);
        }

        // optimization: update parent indexes to point to the current minimal
        if (1 < path.Count)
        {
            foreach(var e in path)
            {
                Parent[e] = id;
            }
        }

        return id;
    }

    /// <summary>
    /// Assert that two expressions are equivalent by merging their associated parent trees.
    /// In order to avoid deep trees, the smallest minimal of the two classes is made the parent of the larger minimal.
    /// </summary>
    public void Union(int expressionId, int derivativeId)
    {
        var expressionClass = FindClass(expressionId);
        var derivativeClass = FindClass(derivativeId);
        if (expressionClass != derivativeClass)
        {
            var smallestMinimal = expressionClass;
            var largerMinimal = derivativeClass;
            var expression = Expressions[expressionId];
            var derivative = Expressions[derivativeId];
            if (0 < expression.CompareTo(derivative))
            {
                smallestMinimal = derivativeClass;
                largerMinimal = expressionClass;
            }
            Parent[largerMinimal] = smallestMinimal;
        }
    }

    private Constant AddConstant(bool value)
    {
        var constant = new Constant(value);
        int id = GetNextId();
        Expressions[id] = constant;
        Ids[constant] = id;
        Parent[id] = id;
        return constant;
    }

    public Variable AddVariable(int variable)
    {
        var expression = new Variable(variable);
        if (Ids.TryGetValue(expression, out var id))
        {
            var current = Expressions[id] as Variable;
            Debug.Assert(current != null, $"Variable not found for id: {id}");
            return current;
        }

        var nextId = Interlocked.Increment(ref LastId);
        Parent.EnsureCapacity(nextId + 1);

        Expressions[nextId] = expression;
        Ids[expression] = nextId;
        Parent[nextId] = nextId;

        return expression;
    }

}



