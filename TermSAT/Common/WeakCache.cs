using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace TermSAT.Common
{
    /// <summary>
    ///    A convenince class for weakly caching item of type T.
    ///    A weak cache automatically removes items from the cache when there are no
    ///    other external object referencing the item in the cache.
    /// </summary>
    /// <typeparam name="K">key Type</typeparam>
    /// <typeparam name="V">value type</typeparam>
    public class WeakCache<K, V> 
        where K : class
        where V : class
    {

        private ConditionalWeakTable<K, V> cache= new ConditionalWeakTable<K, V>();

        public WeakCache() { }

        public V GetOrCreateValue(K key, Func<V> createValue)
        {
            return cache.GetValue(key, _=> createValue());
        }

    }
}
