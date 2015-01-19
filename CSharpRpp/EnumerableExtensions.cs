using System;
using System.Collections.Generic;

namespace CSharpRpp
{
    public static class EnumerableExtensions
    {
        public static void ForEach<T>(this IEnumerable<T> collection, Action<T> action)
        {
            collection.ForEachWithIndex((index, el) => action(el));
        }

        public static void ForEachWithIndex<T>(this IEnumerable<T> collection, Action<int, T> action)
        {
            int index = 0;
            var iter = collection.GetEnumerator();
            while (iter.MoveNext())
            {
                action(index, iter.Current);
                index++;
            }
        }
    }
}