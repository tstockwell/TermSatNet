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
            var tree= new TrieMap<StringSequence, char, string>();
            var key= new StringSequence("123");
            var value= "value123";
            tree.Add(key, value);
            var result= tree[key];
            Assert.AreEqual(value, result, "Failed to retrieve the saved value");
        }
    }
}
