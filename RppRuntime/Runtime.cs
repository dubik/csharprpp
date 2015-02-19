using System;

namespace RppRuntime
{
    // ReSharper disable InconsistentNaming
    public class Runtime
    {
        public static void println(string line)
        {
            Console.WriteLine(line);
        }
    }

    public class Foor
    {
        public int _field;

        public Foor(int field)
        {
            _field = field;
        }

        public int calculate(int k)
        {
            return _field + k;
        }
    }


    public class Foo
    {
        public int calculate(int x, int y)
        {
            return x + y;
        }
    }

    public class Boo
    {
        public static int calculate(int x, int y)
        {
            return x + y;
        }
    }

    // ReSharper restore InconsistentNaming
}