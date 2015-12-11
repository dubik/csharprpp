using System;
using System.Collections.Generic;

namespace CSharpRpp
{
    public static class ListExtensions
    {
        public static List<T> List<T>(params T[] args)
        {
            return new List<T>(args);
        }
    }

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

        public static void EachPair<TFirst, TSecond>(this IEnumerable<TFirst> first, IEnumerable<TSecond> second, Action<TFirst, TSecond> resultSelector)
        {
            first.EachPairWithIndex(second, (index, firstItem, secondItem) => resultSelector(firstItem, secondItem));
        }

        public static void EachPairWithIndex<TFirst, TSecond>(this IEnumerable<TFirst> first, IEnumerable<TSecond> second,
            Action<int, TFirst, TSecond> resultSelector)
        {
            int index = 0;
            var firstIter = first.GetEnumerator();
            var secondIter = second.GetEnumerator();
            while (firstIter.MoveNext() && secondIter.MoveNext())
            {
                resultSelector(index, firstIter.Current, secondIter.Current);
                index++;
            }
        }
    }
}