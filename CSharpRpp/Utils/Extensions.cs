using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace CSharpRpp.Utils
{
    internal static class Extensions
    {
        public static int GetDeepHashCode<T>([CanBeNull] this IList<T> list)
        {
            if (list != null)
            {
                unchecked
                {
                    return list.Aggregate(0, (result, element) => (result * 397) ^ element.GetHashCode());
                }
            }

            return 0;
        }
    }
}