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

        public static void println_args(string format, params object[] args)
        {
            Console.WriteLine(format, args);
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

        public static void printsomething(float p)
        {
            Console.Write("Hello $p");
        }

        public static void Println(string format, params object[] args)
        {
            int len = args.Length;
            Console.Write(format, len);
        }

        public static void PrintlnSomething()
        {
            Println("{0}, {1}", 10, 20);
        }


        public static void Some1(params object[] args)
        {
            
        }

        public static void Some2(object[] args)
        {
            
        }
    }

    // ReSharper restore InconsistentNaming
}