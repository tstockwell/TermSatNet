using System;
using System.Collections.Generic;

public class TwoSatSolver
{
    private readonly int _numVariables;
    private readonly List<int>[] _adj;
    private readonly List<int>[] _revAdj;
    private readonly bool[] _visited;
    private readonly Stack<int> _order;
    private readonly int[] _component;
    private readonly bool?[] _assignment;

    public TwoSatSolver(int numVariables)
    {
        _numVariables = numVariables;
        _adj = new List<int>[2 * numVariables];
        _revAdj = new List<int>[2 * numVariables];
        _visited = new bool[2 * numVariables];
        _order = new Stack<int>();
        _component = new int[2 * numVariables];
        _assignment = new bool?[numVariables];

        for (int i = 0; i < 2 * numVariables; i++)
        {
            _adj[i] = new List<int>();
            _revAdj[i] = new List<int>();
        }
    }

    /// <summary>
    /// Adds a clause (a OR b) to the problem.
    /// A clause (a OR b) is equivalent to the implications (~a => b) and (~b => a).
    /// </summary>
    /// <param name="a">The first literal. Index starts from 0. Use negative for negation (e.g., ~x1 becomes -1).</param>
    /// <param name="b">The second literal. Index starts from 0. Use negative for negation.</param>
    public void AddClause(int a, int b)
    {
        // Convert problem literals to graph vertices.
        // A variable x (positive) is represented by vertex 2*x.
        // Its negation ~x (negative) is represented by vertex 2*x + 1.
        int u = GetVertex(a);
        int v = GetVertex(b);
        int notU = GetNegation(u);
        int notV = GetNegation(v);

        // Add implications (~a => b) and (~b => a).
        AddEdge(notU, v);
        AddEdge(notV, u);
    }

    /// <summary>
    /// Adds a directed edge from u to v.
    /// </summary>
    private void AddEdge(int u, int v)
    {
        _adj[u].Add(v);
        _revAdj[v].Add(u);
    }

    /// <summary>
    /// Converts a literal (variable index) to its corresponding vertex in the graph.
    /// For variable x (0-based): 
    /// - `x` is `2*x`
    /// - `~x` is `2*x + 1`
    /// </summary>
    private int GetVertex(int literal)
    {
        int varIndex = Math.Abs(literal) - 1;
        if (literal > 0)
        {
            // Positive literal (x)
            return 2 * varIndex;
        }
        else
        {
            // Negative literal (~x)
            return 2 * varIndex + 1;
        }
    }

    /// <summary>
    /// Returns the vertex representing the negation of a given vertex.
    /// </summary>
    private int GetNegation(int vertex)
    {
        return vertex % 2 == 0 ? vertex + 1 : vertex - 1;
    }

    /// <summary>
    /// The DFS traversal used by Kosaraju's algorithm.
    /// First DFS builds the finish time stack.
    /// </summary>
    private void Dfs1(int u)
    {
        _visited[u] = true;
        foreach (var v in _adj[u])
        {
            if (!_visited[v])
            {
                Dfs1(v);
            }
        }
        _order.Push(u);
    }

    /// <summary>
    /// The second DFS traversal used by Kosaraju's algorithm.
    /// It traverses the reversed graph to find SCCs.
    /// </summary>
    private void Dfs2(int u, int c)
    {
        _component[u] = c;
        foreach (var v in _revAdj[u])
        {
            if (_component[v] == -1)
            {
                Dfs2(v, c);
            }
        }
    }

    /// <summary>
    /// Solves the 2-SAT problem and returns a satisfying assignment if one exists.
    /// </summary>
    /// <returns>An array of booleans representing the assignment, or null if unsatisfiable.</returns>
    public bool[] Solve()
    {
        // Step 1: Kosaraju's Algorithm to find SCCs
        for (int i = 0; i < 2 * _numVariables; i++)
        {
            if (!_visited[i])
            {
                Dfs1(i);
            }
        }

        Array.Fill(_component, -1);
        int c = 0;
        while (_order.Count > 0)
        {
            int u = _order.Pop();
            if (_component[u] == -1)
            {
                Dfs2(u, c++);
            }
        }

        // Step 2: Check for unsatisfiability
        for (int i = 0; i < _numVariables; i++)
        {
            int u = 2 * i;
            int notU = u + 1;
            if (_component[u] == _component[notU])
            {
                return null; // Unsatisfiable
            }
        }

        // Step 3: Construct satisfying assignment
        // Iterate through SCCs in reverse topological order (which is how we found them).
        for (int i = 0; i < 2 * _numVariables; i++)
        {
            int u = _order.Pop(); // Pop from the finish time stack
            int varIndex = u / 2;
            if (_assignment[varIndex] == null)
            {
                _assignment[varIndex] = (u % 2 == 0); // Assign current literal to true
                _assignment[GetNegation(u) / 2] = (u % 2 != 0); // Assign its negation to false
            }
        }

        var result = new bool[_numVariables];
        for (int i = 0; i < _numVariables; i++)
        {
            result[i] = _assignment[i].Value;
        }

        return result;
    }
}