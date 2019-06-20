using System;
using System.Collections;
using System.Collections.Generic;

namespace TermSAT.Common
{

    /**
     * todo: replace all references to null with default(type)
     */

    /**
     * A trie, or prefix tree, is an ordered tree data structure that is used to 
     * store a dynamic set or associative array where the keys are usually strings. 
     * Unlike a binary search tree, no node in the tree stores the key associated 
     * with that node; instead, its position in the tree defines the key with which 
     * it is associated.
     * 
     * TermSAT uses a trie as an index that's used to quickly find substitution instances
	 * and unifications of a given formula in a large set of formulas.
     * TermSAT requires a trie that can work directly with formulas, thus an off the shelf trie, 
     * that uses strings as keys, was not suitable for TermSAT's purposes.
     * This trie is designed to work with any generic structure that is a list of items.
     * 
     * This trie implementation includes a vistor API that allows clients to traverse 
     * the trie structure.  Actually, this aspect of the trie is more important to TermSAT 
     * than the ability to use it as a Dictionary.
     *  
     * @see http://en.wikipedia.org/wiki/Trie
     * 
     * @author ted.stockwell
     *
     */
    public class TrieMap<TKey, TItem, TValue> : IDictionary<TKey, TValue>
        where TKey : ISequence<TItem> 
        where TItem : IComparable<TItem>, IEquatable<TItem> 
    {

        private static readonly Dictionary<object, object> EMPTY_DICTIONARY = new Dictionary<object, object>();
        private static readonly object NO_VALUE = null;


        /// <summary>
        /// The root node has Depth == -1, Key == null, and Parent == null
        /// Non-root nodes have Depth== position within original key
        /// </summary>
        public interface INode
        {
            IDictionary<TItem, INode> Children { get; }
            INode Parent { get; }
            TValue Value { get; } 
            int Depth { get; } 
            TItem Key { get; } 

            bool IsRoot();
            TValue FindValue(TKey key, int index);
            TResult Accept<TResult>(IVisitor<TResult> visitor);
            TValue Add(TKey key, TValue value);
            TValue Remove(TKey key);
        }

        public interface IVisitor<TResult>
        {
            /**
             * Visit a node
             * @param the key associated with the given node
             * @return false if you dont want to visit the children of the given node. 
             */
            bool Visit(INode node);
            void Leave(INode node);
            bool IsComplete { get; }
            TResult Result { get; }
        }


        public class NodeImpl : INode
        {
            public INode Parent { get; protected set; }
            private IDictionary<TItem, INode> _children;
            public IDictionary<TItem, INode> Children
            {
                get
                {
                    if (_children == null)
                        return EMPTY_DICTIONARY as IDictionary<TItem, INode>;
                    return _children;
                }
                protected set { _children = value; }
            }
            public int Depth { get; protected set; }
            public TValue Value { get; protected set; }
            public TItem Key { get; protected set; }
            public bool IsRoot()  { return Parent == null; }

            public NodeImpl(NodeImpl parent, TItem key, int depth)
            {
                Parent = parent;
                Key = key;
                Depth = depth;
                System.Diagnostics.Trace.Assert(key != null || parent == null);
            }
            public TResult Accept<TResult>(IVisitor<TResult> visitor)
            {
                bool visitChildren = !IsRoot() ? visitor.Visit(this) : true;
                if (visitChildren)
                {
                    foreach (var node in Children.Values)
                    {
                        if (visitor.IsComplete)
                            break;
                        node.Accept(visitor);
                    }
                }
                if (!IsRoot())
                {
                    visitor.Leave(this);
                }
                return visitor.Result;
            }

            public TValue Add(TKey key, TValue value)
            {
                // if at end of key then return the value of this node
                if (key.Length - 1 <= Depth)
                {
                    var oldValue = Value;
                    Value = value;
                    return oldValue;
                }

                var symbol = key[Depth+1];

                if (_children == null)
                    _children = new Dictionary<TItem, INode>();

                if (!_children.TryGetValue(symbol, out INode n))
                    n = new NodeImpl(this, symbol, Depth + 1);

                _children[symbol] = n;

                return n.Add(key, value);
            }

            public TValue FindValue(TKey key, int index)
            {
                if (key.Length <= index)
                    throw new IndexOutOfRangeException();
                if (index < Depth)
                    throw new IndexOutOfRangeException();
                if (index == Depth)
                    return Value;
                var item = key[index];
                var n = Children[item];
                if (n == null)
                    return default(TValue);
                return n.FindValue(key, index + 1);
            }

            public TValue Remove(TKey key)
            {
                var symbol = key[Depth];
                var oldValue = Value;

                if (key.Length - 1 <= Depth)
                {
                    Value = default(TValue);

                    if (Children.Count <= 0 && Parent != null)
                        Parent.Children.Remove(symbol);

                    return oldValue;
                }

                var childNode = Children[symbol];
                if (childNode == null)
                    return default(TValue);
                childNode.Remove(key);

                if (Value.Equals(default(TValue)) && Children.Count <= 0 && Parent != null)
                    Parent.Children.Remove(symbol);

                return oldValue;
            }

            void Clear()
            {
                _children = null;
                Value = default(TValue);
            }
        }



        private readonly NodeImpl _root= new NodeImpl(null, default(TItem), -1);
        public int Count { get; protected set; } = 0;

        public TrieMap()
        {
        }

        public bool IsEmpty { get { return Count <= 0; } }

        public ICollection<TKey> Keys { 
            /*
             * The TrieMap implementation will have to be refactored to support the retrieval of keys.
             * Currently TrieMap does not retain references to the original keys and cannot create new keys.
             * So the only way to return keys is to refactor this implementation so that it retains references to 
             * the orignal keys.
             * But TermSAT doesn't currently need the keys and I don't wanna waste memory on them.
             */
            get 
            { 
                throw new NotSupportedException(); 
            } 
        }

        /**
         * Visits all the nodes in the trie and gathers a list of all the values stored in the trie.
         */
        class ValueCollectorVisitor : IVisitor<HashSet<TValue>>
        {
            HashSet<TValue> keys = new HashSet<TValue>();
            public bool IsComplete { get { return false; } }
            public HashSet<TValue> Result { get { return keys; } }

            public void Leave(INode node) { /* do nothing */  }

            public bool Visit(INode node)
            {
                var value = node.Value;
                if (value != null)
                    keys.Add(value);
                return true;
            }
        };
        public ICollection<TValue> Values { get { return Accept(new ValueCollectorVisitor()); } }

        public bool IsReadOnly { get; set; }

        public TValue this[TKey key] {
            get { return _root.FindValue(key, -1); }
            set
            {
                if (!IsReadOnly)
                {
                    Add(key, value);
                }
            }
        }

        public bool Remove(TKey key)
        {
            if (IsReadOnly)
                return false;
            return !_root.Remove(key).Equals(default(TValue));
        }

        public void Add(TKey key, TValue value)
        {
            if (!IsReadOnly)
            {
                if (value == null)
                    throw new NotSupportedException("This implementation does not support null values");

                var previousValue = _root.Add(key, value);
                if (previousValue == null || previousValue.Equals(default(TValue)))
                    Count++;
            }
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }


        public bool TryGetValue(TKey key, out TValue value)
        {
            throw new System.NotImplementedException();
        }

        public void Clear()
        {
            _root.Children.Clear();
        }

        public TResult Accept<TResult>(IVisitor<TResult> visitor)
        {
            _root.Accept(visitor);
            return visitor.Result;
        }

        public bool ContainsKey(TKey key)
        {
            return this[key] != null;
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
        {
            throw new NotSupportedException();
        }

        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            throw new System.NotImplementedException();
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        {
            throw new NotSupportedException();
        }

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        {
            throw new NotSupportedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotSupportedException();
        }
    }
}

