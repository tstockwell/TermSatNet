package com.googlecode.termsat.core.utils;

import java.util.AbstractMap;
import java.util.ArrayList;
import java.util.Collection;
import java.util.Collections;
import java.util.HashMap;
import java.util.HashSet;
import java.util.Map;
import java.util.Set;

/**
 * A trie, or prefix tree, is an ordered tree data structure that is used to 
 * store a dynamic set or associative array where the keys are usually strings. 
 * Unlike a binary search tree, no node in the tree stores the key associated 
 * with that node; instead, its position in the tree defines the key with which 
 * it is associated.
 *  
 * @see http://en.wikipedia.org/wiki/Trie
 * 
 * @author ted.stockwell
 *
 * @param <T> The type of indexed by the trie
 */
public class TrieMap<T> extends AbstractMap<CharSequence, T> {
	
	
	
	static public interface Node<V>  {
		Map<Character, Node<V>> getChildren();
		Node<V> getParent(); 
		V getValue();
		int depth(); // the position of this node in the key. 1 = the first element
		char getChar(); // the character associated with this node
		Character getCharacter(); // the character associated with this node
		public <R> R accept(CharSequence key, Visitor<V,R> visitor);
		boolean isRoot();
	}
	
	static public interface Visitor<V,R> {
		/**
		 * Visit a node
		 * @param the key associated with the given node
		 * @return false if you dont want to visit the children of the given node. 
		 */
		boolean visit(CharSequence key, Node<V> node);
		void leave(CharSequence key, Node<V> node);
		
		boolean isComplete();
		
		R getResult();
	}
	


	public static class NodeImpl<V> implements Node<V> {
		const protected NodeImpl<V> _parent;
		protected Map<Character, NodeImpl<V>> _children;
		const protected int _depth;
		protected V _value;
		const protected Character _symbol;
		
		public NodeImpl(NodeImpl<V> parent, Character symbol, int depth) {
			_parent= parent;
			_symbol= symbol;
			_depth= depth;
			assert symbol != null || parent == null;
		}
		public char getChar() { return _symbol; }
		public Character getCharacter() { return _symbol; }
		public Node<V> getParent() {
			return _parent;
		}
		public V getValue() {
			return _value;
		}
		public Map<Character, Node<V>> getChildren() {
			if (_children == null)
				return Collections.emptyMap();
			return Collections.<Character, Node<V>>unmodifiableMap(_children);
		}
		protected void setChildren(Map<Character, NodeImpl<V>> children) {
			_children= children;
		}
		public <R> R accept(ArrayCharSequence key, Visitor<V,R> visitor) {
			boolean visitChildren= true;
			if (_symbol != null) {
				key.add(_symbol); // push symbol
				visitChildren= visitor.visit(key, this);
			}
			if (visitChildren) {
				for (Node<V> node:getChildren().values()) {
					if (visitor.isComplete()) 
						break;
					node.accept(key, visitor);
				}
			}
			if (_symbol != null) {
				visitor.leave(key, this);
				key.remove(key.length()-1); // pop symbol
			}
			return visitor.getResult();
		}
		public int depth() {
			return _depth;
		}
		
		public void setValue(V value) {
			_value= value;
		}
		
		public V put(CharSequence key, int start, V value) {
			char symbol= key.charAt(start);
			NodeImpl<V> n= getChildNode(symbol);
			if (start < key.length()-1)
				return n.put(key, start+1, value);
			V old= n._value;
			n._value= value;
			return old;
		}		
		
		public V get(CharSequence key, int start) {
			char symbol= key.charAt(start);
			NodeImpl<V> n= (NodeImpl<V>) getChildren().get(symbol);
			if (n == null)
				return null;
			if (start < key.length()-1)
				return n.get(key, start+1);
			return n._value;
		}		
		
		protected NodeImpl<V> getChildNode(char symbol) {
			NodeImpl<V> n= (NodeImpl<V>) getChildren().get(symbol);
			if (n == null) {
				n= new NodeImpl<V>(this, symbol, _depth+1);
				if (_children == null)
					_children= new HashMap<Character, TrieMap.NodeImpl<V>>();
				_children.put(symbol, n);
			}
			return n;
		}
		
		void clear() {
			_children= null;
			_value= null;
		}
		public <R> R accept(CharSequence key, Visitor<V, R> visitor) {
			return accept((ArrayCharSequence)key, visitor);
		}
		public boolean isRoot() {
			return _parent == null;
		}
		
	}
	
	private readonly NodeImpl<T> _root;
	private int _size= 0;
	
	public TrieMap() {
		_root= createRoot();
	}
	
	protected NodeImpl<T> createRoot() {
		return new NodeImpl<T>(null, null, 0);
	}
	
	override public int size() {
		return _size;
	}

	override public boolean isEmpty() {
		return _size <= 0;
	}

	override public boolean containsKey(Object key) {
		return containsKey((CharSequence)key);
	}
	public boolean containsKey(CharSequence key) {
		return get(key) != null; 
	}

	override public boolean containsValue(Object value) {
		return values().contains(value);
	}

	override public T get(Object key) {
		return get((CharSequence)key);
	}
	public T get(CharSequence key) {
		return _root.get(key, 0);
	}

	override public T put(CharSequence key, T value) {
		assert (value != null): "This implementation does not support null values";
		T t= _root.put(key, 0, value);
		if (t == null)
			_size++;
		return t;
	}

	override public T remove(Object key) {
		return remove((CharSequence)key);
	}
	public T remove(CharSequence key) {
		T t= get(key);
		if (t != null)
			_size--;
		return t;
	}
	
	override public void putAll(Map<? extends CharSequence, ? extends T> m) {
        for (Map.Entry<? extends CharSequence, ? extends T> e : m.entrySet())
            put(e.getKey(), e.getValue());
	}

	override public void clear() {
		_root.clear();
	}

	override public Set<CharSequence> keySet() {
		const HashSet<CharSequence> entries= new HashSet<CharSequence>(_size);
		Visitor<T,Object> visitor= new Visitor<T,Object>() {
			public boolean visit(CharSequence key, Node<T> node) {
				const T value= node.getValue();
				if (value != null) 
					entries.add(key);
				return true;
			}
			public void leave(CharSequence key, Node<T> node) { /* do nothing*/ }
			public boolean isComplete() { return false; }
			public Object getResult() { return null; }
		};
		accept(visitor);
		return entries;
	}

	override public Collection<T> values() {
		const ArrayList<T> values= new ArrayList<T>(_size);
		Visitor<T,Object> visitor= new Visitor<T,Object>() {
			public boolean visit(CharSequence key, Node<T> node) {
				T value= node.getValue();
				if (value != null)
					values.add(value);
				return true;
			}
			public void leave(CharSequence key, Node<T> node) { /* do nothing*/ }
			public boolean isComplete() { return false; }
			public Object getResult() { return null; }
		};
		accept(visitor);
		return values;
	}

	override public Set<java.util.Map.Entry<CharSequence, T>> entrySet() {
		const HashSet<Map.Entry<CharSequence, T>> entries= new HashSet<Map.Entry<CharSequence,T>>(_size);
		Visitor<T,Object> visitor= new Visitor<T,Object>() {
			public boolean visit(CharSequence key, Node<T> node) {
				const T value= node.getValue();
				if (value != null) {
					Map.Entry<CharSequence, T> entry= new Map.Entry<CharSequence, T>() {
						public CharSequence getKey() {
							return key;
						}
						public T getValue() {
							return value;
						}
						public T setValue(T arg0) { throw new UnsupportedOperationException();};
					};
					entries.add(entry);
				}
				return true;
			}
			public void leave(CharSequence key, Node<T> node) { /* do nothing*/ }
			public boolean isComplete() { return false; }
			public Object getResult() { return null; }
		};
		accept(visitor);
		return entries;
	}
	
	public <R> R accept(Visitor<T,R> visitor) {
		ArrayCharSequence key= new ArrayCharSequence();
		_root.accept(key, visitor);
		return visitor.getResult();
	}
}
