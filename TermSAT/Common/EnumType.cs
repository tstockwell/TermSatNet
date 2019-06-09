using System;
using System.Collections.Generic;
using System.Text;

namespace TermSAT.Common
{
    public class EnumType<T, V> where T : EnumType<T, V>
    {
        private static Dictionary<V, T> all = new Dictionary<V, T>();

        public static T FindInstance(V value)
        {

            // assume that if the static cache of objects is empty then the 
            // static initializer for <T> has not been called yet.
            // So, call static initializer in order to cause all to be populated
            if (all.Count <= 0)
            {
                System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(T).TypeHandle);
            }

            var i = all[value];
            if (i == null)
            {
                throw new Exception("No such type:" + value);
            }
            return i;
        }

        public V Value { get; }
        protected EnumType(V value)
        {
            this.Value = value;
            all[value] = (T)this;
        }

        public override string ToString()
        {
            return this.Value.ToString();
        }
    }

    public static class EnumTypeExtensions
    {
        /// <summary>
        /// A convenient extension method for converting string to EnumTypes
        /// </summary>
        public static T FindEnumType<T>(this string value) where T : EnumType<T, string>
        {
            return EnumType<T, string>.FindInstance(value);
        }
    }
}
