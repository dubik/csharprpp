using System;

namespace RppRuntime
{
    public class Option<T>
    {
    }

    public class Some<T> : Option<T>
    {
        public T Item;
    }

    public class Main
    {
        public static int main()
        {
            Some<int> k = new Some<int> {Item = 123};
            return k.Item;
        }
    }

    public class Closures
    {
        public delegate bool MyClosure(int a, int b);

        public Closures()
        {
            bool k = true;
            MyClosure func = (a, b) => a < b == k;

            Console.WriteLine("Hello");
            func(10, 12);
        }
    }
}