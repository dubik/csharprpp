using System;
using System.Collections.Generic;

namespace CSharpRpp
{
    public static class EnumerableExtensions
    {
        public static void ForEach<T>(this IEnumerable<T> collection, Action<T> action)
        {
            var iter = collection.GetEnumerator();
            while (iter.MoveNext())
            {
                action(iter.Current);
            }
        }
    }
}
