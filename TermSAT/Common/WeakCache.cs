using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace TermSAT.Common
{
    /// <summary>
    ///    A convenince class for weakly caching items.
    ///    
    ///    IMPORTANT...
    ///    The WeakReference implementation in .NET is pretty weak (get it ;-)).
    ///    Unlike in Java, there is no way in .NET, AFAIK, to clean up WeakReferences whose targets 
    ///    have been garbage collected.
    ///    That means that even though the values saved in a cache may be garbage collected the key will not.
    ///    
    ///    It's recommended that, if possible, the values stored in a cache have finalizers 
    ///    that will remove themselves from any cache during finalization 
    /// 
    /// </summary>
    public class WeakCache<TKey, TValue> where TValue : class
    {

        private Dictionary<TKey, WeakReference<TValue>> cache= new Dictionary<TKey, WeakReference<TValue>>();

        public WeakCache() { }

        public TValue GetOrCreateValue(TKey key, Func<TValue> createValue)
        {
            TValue value;

            // first try to get the value without locking anything
            if (cache.TryGetValue(key, out WeakReference<TValue> reference))
                if (reference.TryGetTarget(out value))
                    return value;


            // try again, with locking
            lock (cache)
            {
                if (!cache.TryGetValue(key, out reference))
                {
                    value = createValue();
                    reference = new WeakReference<TValue>(value);
                    cache.Add(key, reference);
                }
                else
                {
                    if (!reference.TryGetTarget(out value))
                    {
                        value = createValue();
                        reference.SetTarget(value);
                    }
                }
            }

            return value;
        }
        public void Remove(TKey key)
        {
            lock (cache)
            {
                cache.Remove(key);
            }
        }
        
        public bool TryGetValue(TKey key, out TValue value)
        {
            if (cache.TryGetValue(key, out WeakReference<TValue> reference))
                if (reference.TryGetTarget(out value))
                    return true;
            value = default(TValue);
            return false;
        }

        public int Count => cache.Count;
    }
}
