using System;
using System.Collections.Generic;
using System.Linq;

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

        public static bool IsEmpty<T>(this IEnumerable<T> collection) => !collection.Any();

        public static bool NonEmpty<T>(this IEnumerable<T> collection) => !collection.IsEmpty();

        public static IEnumerable<T> Apply<T>(this IEnumerable<T> collection, Action<T> action)
        {
            return collection.Select(c =>
                {
                    action(c);
                    return c;
                });
        }

        public static IEnumerable<TResult> Apply<T, A1, TResult>(this IEnumerable<T> collection, Func<T, A1, TResult> action, A1 arg)
        {
            return collection.Select(c => action(c, arg));
        }
    }
}