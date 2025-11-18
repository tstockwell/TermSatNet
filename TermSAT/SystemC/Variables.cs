namespace TermSAT.SystemC;

using System.Collections.Generic;

public class IntegerTrieWithReverse
{
    private class TrieNode
    {
        public Dictionary<int, TrieNode> Children { get; set; } = new Dictionary<int, TrieNode>();
        public int? UniqueId { get; set; } = null;
    }

    private readonly TrieNode _root = new TrieNode();
    private readonly Dictionary<int, int[]> _idToArrayMap = new Dictionary<int, int[]>();
    private int _idCounter = 0;

    /// <summary>
    /// Inserts an integer array into the trie and assigns/retrieves a unique ID, 
    /// storing the reverse mapping.
    /// </summary>
    public int AssignId(int[] array)
    {
        TrieNode current = _root;

        foreach (int number in array)
        {
            if (!current.Children.ContainsKey(number))
            {
                current.Children.Add(number, new TrieNode());
            }
            current = current.Children[number];
        }

        if (current.UniqueId.HasValue)
        {
            return current.UniqueId.Value;
        }
        else
        {
            _idCounter++;
            current.UniqueId = _idCounter;
            // Store the reverse mapping
            _idToArrayMap.Add(_idCounter, array);
            return _idCounter;
        }
    }

    /// <summary>
    /// Looks up the original integer array for a given unique ID.
    /// </summary>
    /// <param name="id">The unique ID to find.</param>
    /// <returns>The original integer array if found, otherwise null.</returns>
    public int[]? LookupArray(int id)
    {
        // Use TryGetValue for safe and efficient lookup
        if (_idToArrayMap.TryGetValue(id, out int[]? array))
        {
            return array;
        }
        return null;
    }
}