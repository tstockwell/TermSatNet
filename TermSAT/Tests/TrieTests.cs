using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using TermSAT.Common;

namespace TermSAT.Tests
{
    [TestClass]
    public class TrieTests
    {
        [TestMethod]
        public void TestBasicOperations()
        {
            var tree= new TrieIndex<char, string>();

            // after creating tree, make sure its empty and Count==0
            Assert.IsTrue(tree.IsEmpty);
            Assert.AreEqual(0, tree.Count);

            // add a avalue, make sure we can get it back
            var key1 = "123";
            var value1= "value123";
            tree.Add(key1, value1);
            var result1= tree[key1];
            Assert.AreEqual(value1, result1, "Failed to retrieve the saved value");
            Assert.IsTrue(tree.ContainsKey(key1));
            Assert.AreEqual(1, tree.Count);
            Assert.IsTrue(tree.TryGetValue(key1, out result1));
            Assert.AreEqual(value1, result1, "Failed to retrieve the saved value");

            // make sure count is correct
            Assert.IsFalse(tree.IsEmpty);
            Assert.AreEqual(1, tree.Count);

            // add a 2nd, similar key.
            var key2 = "12x";
            var value2 = "value12x";
            tree.Add(key2, value2);
            var result2= tree[key2];
            Assert.AreEqual(value2, result2, "Failed to retrieve the saved value");
            Assert.IsTrue(tree.ContainsKey(key1));
            Assert.IsTrue(tree.ContainsKey(key2));
            Assert.AreEqual(2, tree.Count);

            Assert.IsTrue(tree.TryGetValue(key2, out result2));
            Assert.AreEqual(value2, result2, "Failed to retrieve the saved value");
            Assert.IsFalse(tree.IsEmpty);

            // and a third similar key
            var key3 = "12x4";
            var value3 = "value12x4";
            tree.Add(key3, value3);
            var result3 = tree[key3];
            Assert.AreEqual(value3, result3, "Failed to retrieve the saved value");
            Assert.IsTrue(tree.ContainsKey(key1));
            Assert.IsTrue(tree.ContainsKey(key2));
            Assert.IsTrue(tree.ContainsKey(key3));
            Assert.AreEqual(3, tree.Count);

            Assert.IsTrue(tree.TryGetValue(key3, out result3));
            Assert.AreEqual(value3, result3, "Failed to retrieve the saved value");
            Assert.IsFalse(tree.IsEmpty);

            // and a forth.  This key has a unique prefix
            var key4 = "--12x4";
            var value4 = "value--12x4";
            tree.Add(key4, value4);
            var result4 = tree[key4];
            Assert.AreEqual(value4, result4, "Failed to retrieve the saved value");
            Assert.IsTrue(tree.ContainsKey(key1));
            Assert.IsTrue(tree.ContainsKey(key2));
            Assert.IsTrue(tree.ContainsKey(key3));
            Assert.IsTrue(tree.ContainsKey(key4));
            Assert.AreEqual(4, tree.Count);

            Assert.IsTrue(tree.TryGetValue(key4, out result4));
            Assert.AreEqual(value4, result4, "Failed to retrieve the saved value");
            Assert.IsFalse(tree.IsEmpty);

            // now, remove a key.
            // make sure the other values are still there
            // make sure Count has changed.
            // make sure the removed value is NOT there
            result2 = null;
            Assert.IsTrue(tree.Remove(key2, out result2));
            Assert.AreEqual(value2, result2, "Failed to retrieve the saved value");
            Assert.IsFalse(tree.ContainsKey(key2));
            Assert.IsTrue(tree.ContainsKey(key1));
            Assert.IsTrue(tree.ContainsKey(key3));
            Assert.IsTrue(tree.ContainsKey(key4));
            Assert.AreEqual(3, tree.Count, "The count is incorrect after removing an item");
            Assert.IsFalse(tree.Remove(key2, out result2));
            Assert.IsNull(result2, "When removing items that are not in container, the out param should be nulled");

            // now clear the tree
            Assert.IsFalse(tree.IsEmpty);
            tree.Clear();
            Assert.IsTrue(tree.IsEmpty);
            Assert.IsFalse(tree.ContainsKey(key1));
            Assert.IsFalse(tree.ContainsKey(key3));
            Assert.IsFalse(tree.ContainsKey(key4));

        }

        [TestMethod]
        public void TestVisitor()
        {
            var dictionary = new Dictionary<string, string>()
            {
                { "12435", "value12345" },
                { "12436", "value12436" },
                { "12735", "value12735" },
                { "x2435", "valuex2435" },
                { "x2436", "valuex2436" },
                { "x2735", "valuex2735" }
            };

            var tree = new TrieIndex<char, string>();
            foreach (var k in dictionary.Keys)
            {
                tree.Add(k, dictionary[k]);
            }

            var visitor = new TestVisitor();
            tree.Accept<string>(visitor);



            Assert.AreEqual(dictionary.Count, visitor.AllValuesFound.Count);
            Assert.AreEqual(18, visitor.AllNodesVisited.Count);
            Assert.AreEqual(18, visitor.AllNodesLeft.Count);

            // must all be unique nodes
            Assert.AreEqual(18, new HashSet<TrieIndex<char, string>.INode>(visitor.AllNodesVisited).Count);
            Assert.AreEqual(18, new HashSet<TrieIndex<char, string>.INode>(visitor.AllNodesLeft).Count);

            // values found are exactly what's in the dictionary
            foreach (var v in visitor.AllValuesFound)
            {
                Assert.IsTrue(dictionary.ContainsValue(v));
            }
            foreach (var v in dictionary.Values)
            {
                Assert.IsTrue(visitor.AllValuesFound.Contains(v));
            }                
        }
    }

    /// <summary>
    /// Visits all the nodes in a trie andcollects a bunch of information
    /// </summary>
    class TestVisitor : TrieIndex<char, string>.IVisitor<string>
    {
        public bool IsComplete => false; // visit all nodes

        public string Result { get; private set; } = "ok";

        public List<TrieIndex<char, string>.INode> AllNodesVisited = new List<TrieIndex<char, string>.INode>();
        public List<TrieIndex<char, string>.INode> AllNodesLeft = new List<TrieIndex<char, string>.INode>();
        public List<string> AllValuesFound = new List<string>();

        public void Leave(TrieIndex<char, string>.INode node)
        {
            AllNodesLeft.Add(node);
            if (node.Value != default(string))
            {
                AllValuesFound.Add(node.Value);
            }
        }

        public bool Visit(TrieIndex<char, string>.INode node)
        {
            AllNodesVisited.Add(node);
            return true; // visit children
        }
    }
}
