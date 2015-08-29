using System;

namespace RppRuntime
{
    public abstract class Option<T>
    {
        public abstract bool IsEmpty { get; }
        public abstract T Get { get; }

        public Option<B> Map<B>(Func<T, B> f)
        {
            if (IsEmpty)
            {
                return new None<B>();
            }

            return new Some<B>(f(Get));
        }
    }

    public class Some<T> : Option<T>
    {
        public Some(T val)
        {
            Get = val;
        }

        public override bool IsEmpty => false;
        public override T Get { get; }
    }

    public class None<T>: Option<T>
    {
        public override bool IsEmpty => true;

        public override T Get
        {
            get { throw new Exception("Is empty"); }
        }
    }

    public class Main
    {
        public static int main()
        {
            var k = new Some<int>(123);
            k.Map(x => x * 2);
            return k.Get;
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