using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

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
    public class TrieMap<TItem, TValue> : IDictionary<IEnumerator<TItem>, TValue>
        where TItem : IComparable<TItem>, IEquatable<TItem> 
    {
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
            TValue FindValue(IEnumerator<TItem> key);
            TResult Accept<TResult>(IVisitor<TResult> visitor);
            TValue Add(IEnumerator<TItem> key, TValue value);
            TValue Remove(IEnumerator<TItem> key);
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
                        return ImmutableDictionary<TItem, INode>.Empty;
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
                if (visitChildren && _children != null)
                {
                    foreach (var node in _children.Values)
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

            public TValue Add(IEnumerator<TItem> key, TValue value)
            {
                // if at end of key then return the value of this node
                if (!key.MoveNext())
                {
                    var oldValue = Value;
                    Value = value;
                    return oldValue;
                }

                var symbol = key.Current;

                if (_children == null)
                    _children = new Dictionary<TItem, INode>();

                if (!_children.TryGetValue(symbol, out INode n))
                    n = new NodeImpl(this, symbol, Depth + 1);

                _children[symbol] = n;

                return n.Add(key, value);
            }

            public TValue FindValue(IEnumerator<TItem> key)
            {
                if (!key.MoveNext())
                    return Value;
                var item = key.Current;
                if (!Children.TryGetValue(item, out INode n))
                    return default(TValue);
                return n.FindValue(key);
            }

            public TValue Remove(IEnumerator<TItem> key)
            {
                if (!key.MoveNext())
                {
                    var oldValue = Value;
                    Value = default(TValue);

                    if (_children.Count <= 0)
                        Parent.Children.Remove(Key);

                    return oldValue;
                }

                var symbol = key.Current;
                var childNode = Children[symbol];
                if (childNode == null)
                    return default(TValue);
                return childNode.Remove(key);
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

        public ICollection<IEnumerator<TItem>> Keys { 
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

        /// <summary>
        /// Unlike the standard .NET IDictionary interface, this method doesn't throw an exception 
        /// if the ley is not found, because I think that's stupid.
        /// It's normal to look for items that are not necessarily in the container, it's is NOT an error.
        /// </summary>
        public TValue this[IEnumerator<TItem> key] {
            get { return _root.FindValue(key); }
            set
            {
                if (!IsReadOnly)
                {
                    Add(key, value);
                }
            }
        }

        public bool Remove(IEnumerator<TItem> key)
        {
            if (IsReadOnly)
                return false;
            bool valueWasRemoved = !_root.Remove(key).Equals(default(TValue));
            if (valueWasRemoved)
                Count--;
            return valueWasRemoved;
        }

        public void Add(IEnumerator<TItem> key, TValue value)
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

        public void Add(KeyValuePair<IEnumerator<TItem>, TValue> item)
        {
            Add(item.Key, item.Value);
        }


        public bool TryGetValue(IEnumerator<TItem> key, out TValue value)
        {
            value = _root.FindValue(key);
            return value != null;
        }

        public void Clear()
        {
            _root.Children.Clear();
            Count = 0;
        }

        public TResult Accept<TResult>(IVisitor<TResult> visitor)
        {
            _root.Accept(visitor);
            return visitor.Result;
        }

        public bool ContainsKey(IEnumerator<TItem> key)
        {
            return this[key] != null;
        }

        bool ICollection<KeyValuePair<IEnumerator<TItem>, TValue>>.Contains(KeyValuePair<IEnumerator<TItem>, TValue> item)
        {
            throw new NotSupportedException();
        }

        void ICollection<KeyValuePair<IEnumerator<TItem>, TValue>>.CopyTo(KeyValuePair<IEnumerator<TItem>, TValue>[] array, int arrayIndex)
        {
            throw new System.NotImplementedException();
        }

        bool ICollection<KeyValuePair<IEnumerator<TItem>, TValue>>.Remove(KeyValuePair<IEnumerator<TItem>, TValue> item)
        {
            throw new NotSupportedException();
        }

        IEnumerator<KeyValuePair<IEnumerator<TItem>, TValue>> IEnumerable<KeyValuePair<IEnumerator<TItem>, TValue>>.GetEnumerator()
        {
            throw new NotSupportedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotSupportedException();
        }
    }
}

