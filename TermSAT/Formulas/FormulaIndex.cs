using static TermSAT.Formulas.InstanceRecognizer;
using System.Collections.Generic;
using System.Linq;
using TermSAT.RuleDatabase;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace TermSAT.Formulas;


/// <summary>
/// A rewrite of TermSAT.Common.TrieIndex built specifically for TermSAT formulas (that is, nand operators and variables only).  
/// Also, its specifically built for the Scripts.RunNandRuleGenerator method, it stores data in a database instead of memory.  
/// If you want an in-memory version of this class then use an in-memory database.  
/// 
/// This class is not meant to be a prefix tree exactly, its meant to be an index for quickly finding generalizations of a given formula.  
/// Its implemented using a prefix-tree that stores a formula ID, indexed by the formulas 'TermSAT number'.  
/// The advantage of this index is that the tree may be searched for generalizations much more efficiently 
/// than a collection of individual formulas.  
/// The RunNandRuleGenerator method also needs to know the matching formula, so the prefix tree saves the formula ID, 
/// that is, the value in the Id column in the rule database.
/// 
/// TermSAT #...
/// TermSAT formulas can be assigned a number by replacing nand symbols with 0' and removing dots, examples...  
/// ```
///     ||.2|.1|.1.1||.1.2||.1.1|.3|.1.2 => 002010110012001103012
///     .2 => 2
/// ```
/// However, its important to note that this class doesn't treat a TermSAT # as a string, but as an enumeration of ints, 
/// more like this ```{0,0,2,0,1,0,1,1,0,0,1,2,0,0,1,1,0,3,0,1,2}```.
/// where 0 represents a nand operator and every other value represents a variable with that number.
/// 
/// 
/// Notes...
/// This is a rewrite from scratch, because TermSAT.Common.TrieIndex kinda sucks, the visitor API is difficult to follow, I hate it now.  
/// Instead of visitors, I now much prefer to enumerate trees and process the enumeration.  
/// I find that much easier to understand.
/// 
/// </summary>
public static partial class FormulaIndex
{

    public static NodeContext GetDatabaseContext(string datasource)
    {
        var connectionString = "DataSource=" + datasource;
        var options = new DbContextOptionsBuilder()
            .UseSqlite(connectionString)
            .Options;

        return new NodeContext(options);
    }

    public static bool TryGetRoot(this IQueryable<Node> db, out Node root)
    {
        root = db.Where(_ => _.Parent == Node.PARENT_NONE).FirstOrDefault();
        return root != null;
    }
    public static async Task<Node> GetRootAsync(this DbSet<Node> db)
    {
        var root = await db.Where(_ => _.Parent == Node.PARENT_NONE).FirstOrDefaultAsync();
        Debug.Assert(root != null);
        return root;
    }
    public static async Task<Node> AddRootAsync(this DbSet<Node> nodeSet)
    {
        var root = new Node(Node.PARENT_NONE, Node.KEY_ROOT, Node.VALUE_NONE);
        await nodeSet.AddAsync(root);
        return root;
    }
    //public static async Task<Node> GetRootAsync(this NodeContext db)
    //{
    //    if (!db.Nodes.TryGetRoot(out var root))
    //    {
    //        root = await db.AddRootAsync();
    //    }
    //    return root;
    //}

    static public void DeleteAll(this NodeContext ctx) =>
        ctx.Database.ExecuteSqlRaw($"DELETE FROM {nameof(NodeContext.Nodes)}");


    static public async Task AddGeneralizationAsync(this ReRiteDbContext ctx, ReductionRecord formulaRecord)
    {
        var nodeSet = ctx.Lookup;

        // add formula to index by navigating to the node associated with the formula, 
        // adding missing nodes along the way.
        if (!nodeSet.TryGetRoot(out var nodeRecord))
        {
            nodeRecord = await nodeSet.AddRootAsync();
            await ctx.SaveChangesAsync();
        }
        foreach(var term in formulaRecord.Formula)
        {
            if (term is Nand)
            {
                var nextNode = await nodeSet.AsNoTracking()
                    .Where(_ => _.Parent == nodeRecord.Id && _.Key == Node.KEY_NAND)
                    .FirstOrDefaultAsync();

                if (nextNode == null)
                {
                    nextNode = new Node(parent:nodeRecord.Id, key:Node.KEY_NAND, value:Node.VALUE_NONE);
                    await nodeSet.AddAsync(nextNode);
                    await ctx.SaveChangesAsync();
                }
                nodeRecord = nextNode;
            }
            else if (term is Variable varFormula)
            {
                var nextNode = await nodeSet.AsNoTracking()
                    .Where(_ => _.Parent == nodeRecord.Id && _.Key == varFormula.Number)
                    .FirstOrDefaultAsync();

                if (nextNode == null)
                {
                    nextNode = new Node(parent: nodeRecord.Id, key: varFormula.Number, value: Node.VALUE_NONE);
                    await nodeSet.AddAsync(nextNode);
                    await ctx.SaveChangesAsync();
                }
                nodeRecord = nextNode;

            }
            else
            {
                throw new TermSatException("Not a valid TermSAT formula, should be nuttin but nand operators and variables.");
            }
        }

        // when we get to here then nodeRecord is the leaf node, store the formula Id as the value
        nodeRecord.Value = formulaRecord.Id;
    }

    public static async Task<IEnumerable<SearchResult>> FindGeneralizationsAsync(this IQueryable<Node> ctx, Formula formulaToMatch, int maxMatchCount)
    {
        List<SearchResult> results = null;

        //  this method does a depth-first search of the node tree
        var todo = new Stack<(int position, Dictionary<int, Formula> substitutions, Node node)>();
        {
            // add formula to index by navigating to the node associated with the formula, 
            // adding missing nodes along the way.
            if (!ctx.TryGetRoot(out var root))
            {
                throw new Exception($"A root must be present before calling {nameof(FindGeneralizationsAsync)}");
            }
            var branches = await ctx.AsNoTracking().Where(_ => _.Parent == root.Id).ToListAsync();
            foreach (var branch in branches.AsEnumerable().Reverse())
            {
                todo.Push(new(0, new Dictionary<int, Formula>(), branch));
            }
        }

        // inside the loop we'll be looking up formulas by position.
        // The time complexity of the GetFormulaAtPosition method grows linearly as a function of the length of the formula,
        // I think its much more efficient to put formulaToMatch in an array right now.
        var flatTerm = formulaToMatch.AsFlatTerm().ToArray();

        int matchCount = 0;
        while (todo.Any() && matchCount < maxMatchCount)
        {
            var state = todo.Pop();
            int currentPosition = state.position; // current position within the formula to match
            var currentSubstitutions = state.substitutions;
            var currentSymbol = state.node.Key;

            var instanceSubformula = flatTerm[currentPosition]; //formulaToMatch.GetFormulaAtPosition(currentPosition);

            // if this node is a variable then get the substitution associated 
            // with the variable.
            // If there is no substitution then create one 
            if (0 < currentSymbol)
            {
                if (currentSubstitutions.TryGetValue(currentSymbol, out Formula subtitute))
                {
                    // A substitution already exists, if current subformula does not match previous 
                    // substitution then not a match 
                    if (!subtitute.Equals(instanceSubformula))
                    {
                        continue;
                    }
                }
                else
                {
                    currentSubstitutions = new Dictionary<int, Formula>(currentSubstitutions)
                    {
                        { currentSymbol, instanceSubformula }
                    };
                }
                currentPosition += instanceSubformula.Length;
            }
            else if (0 == currentSymbol)
            {
                // if the formula doesn't start with a nand then formula is not a match
                if (instanceSubformula is Nand)
                {
                    currentPosition++;
                }
                else
                {
                    continue;
                }
            }

            var children = await ctx.AsNoTracking().Where(_ => _.Parent == state.node.Id).ToListAsync();

            if (children.Count <= 0)
            {
                // this node is a leaf but there is still formula left, so not a match
                if (currentPosition < formulaToMatch.Length)
                {
                    continue;
                }

                // this node is a leaf and we have matched the entire 
                // formula so we have found a match
                if (formulaToMatch.Length <= currentPosition)
                {
                    matchCount++;
                    var result = new SearchResult(state.node, currentSubstitutions);
                    if (maxMatchCount <= 1)
                    {
                        return [result];
                    }
                    if (results == null)
                    {
                        results = new();
                    }
                    results.Add(result);
                }
                continue;
            }

            // this node is not a leaf but we're out of formula, so not a match
            if (formulaToMatch.Length <= currentPosition)
            { 
                continue;
            }

            // keep searching, put branches on the queue
            foreach (var child in children.AsEnumerable().Reverse())
            {
                todo.Push(new(currentPosition, currentSubstitutions, child));
            }

        }
        if (results == null)
        {
            return Enumerable.Empty<SearchResult>();
        }
        return results;
    }
}
