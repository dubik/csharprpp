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

        public static void printFormat(string format, params object[] args)
        {
            Console.WriteLine(format, args);
        }
    }

    public class Human
    {
        public virtual int getId()
        {
            return 10;
        }
    }

    public class Person : Human
    {
        public override int getId()
        {
            return 13;
        }

        public static int getGlobalId()
        {
            Human person = new Person();
            return person.getId();
        }
    }

    public class OneFieldClass<T>
    {
        public T MyField;
    }


    public class Pair<T, K>
    {
        public T Name;
        public K Value;
    }

    public class StringKey<K> : Pair<string, K>
    {
        public K SecondValue;
    }

    public class OneFieldFactory
    {
        public static void Create()
        {
            var k = new OneFieldClass<int>();
            var l = new OneFieldClass<string>();
            var p = new OneFieldClass<Element>();
        }
    }

    public class Element
    {
        public string Name;
    }

    public class Stack<T>
    {
        public T element;
    }

    public class Factory<T>
    {
        public static Stack<T> Create()
        {
            return new Stack<T>();
        }

        public static Stack<int> CreateInt()
        {
            return new Stack<int>();
        }

        public static Stack<Element> CreateElement()
        {
            return new Stack<Element>();
        }
    }

    class StackRoot<T>
    {
        public virtual StackRoot<T> Push(T element)
        {
            return new StackNext<T>(element, this);
        }

        public virtual T Top()
        {
            throw new Exception("No element");
        }

        public virtual StackRoot<T> Pop()
        {
            throw new Exception("No element");
        }
    }

    class StackNext<T> : StackRoot<T>
    {
        private readonly T _element;
        private readonly StackRoot<T> _previous;

        public StackNext(T element, StackRoot<T> prev)
        {
            _element = element;
            _previous = prev;
        }

        public override T Top()
        {
            return _element;
        }

        public override StackRoot<T> Pop()
        {
            return _previous;
        }
    }

    public class Boo
    {
        public void Write()
        {
            Runtime.printFormat("Hello {0}", 10);
        }

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

        public static int varArgs(string k, int p, params bool[] args)
        {
            return args.Length;
        }

        public static bool[] CreateArray()
        {
            bool[] a = {true, false};
            varArgs("", 10, false, true);
            return a;
        }

        public int IntegerProperty { get; set; }
    }

    // ReSharper restore InconsistentNaming
}