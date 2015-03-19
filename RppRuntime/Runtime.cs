using System;
using System.Security.Policy;

namespace RppRuntime
{
    // ReSharper disable InconsistentNaming
    public class Runtime
    {
        public static void println(string line)
        {
            Console.WriteLine(line);
        }

        public static void println_args(string format, params object[] args)
        {
            Console.WriteLine(format, args);
        }
    }

    public class Boo
    {
        public bool LogEq(int p)
        {
            int k = 10;
            return k == p;
        }

        public bool LogEq(bool first, bool second, bool third)
        {
            if (first && second && third)
            {
                return true;
            }

            return false;
        }

        public bool LogOr2(bool first, bool second)
        {
            return first || second;
        }

        public bool LogOr(bool first, bool second, bool third)
        {
            return first || second || third;
        }

        public static int DoWhile()
        {
            int k = 10;
            int res = 0;
            while (k > 0)
            {
                k = k - 1;
                res = res + 1;
            }
            return res;
        }

        public static bool less(int x)
        {
            return x < 10;
        }

        public static bool more(int x)
        {
            return x > 10;
        }

        public static bool lessEq(int x)
        {
            return x <= 10;
        }

        public static bool moreEq(int x)
        {
            return x >= 10;
        }

        public static bool eq(int x)
        {
            return x == 10;
        }

        public static bool notEq(int x)
        {
            return x != 10;
        }
    }

    // ReSharper restore InconsistentNaming
}