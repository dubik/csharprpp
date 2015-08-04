using System;

namespace RppRuntime
{
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
