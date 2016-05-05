using System.Collections.Generic;

namespace CSharpRpp.TypeSystem
{
    internal class ArrayOfTypesComparator : IEqualityComparer<RType[]>
    {
        public static readonly ArrayOfTypesComparator Instance = new ArrayOfTypesComparator();

        public bool Equals(RType[] x, RType[] y)
        {
            if (x == y)
                return true;

            if (x.Length != y.Length)
                return false;

            for (int i = 0; i < x.Length; i++)
            {
                // TODO perhaps replace with != because then it will be possible to override or may be not...
                if (!ReferenceEquals(x[i], y[i]))
                    return false;
            }

            return true;
        }

        public int GetHashCode(RType[] types)
        {
            int result = 1;
            foreach (var type in types)
            {
                result += (result * 397) ^ type.GetHashCode();
            }
            return result;
        }
    }
}