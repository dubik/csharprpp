﻿using System;

namespace CLRCodeGen.Monads
{
    public class Item
    {
        public string Calculate() => "Hello";
    }

    public class Bag<TA> where TA : Item, new()
    {
        public string Create()
        {
            TA item = new TA();
            return item.Calculate();
        }
    }

    internal abstract class Option<TItem>
    {
        public abstract bool IsEmpty();
        public abstract TItem Get();

        public Option<TRes> Map<TRes>(Func<TItem, TRes> f)
        {
            if (IsEmpty())
            {
                return new None<TRes>();
            }
            else
            {
                return new Some<TRes>(f(Get()));
            }
        }
    }

    internal class None<TItem> : Option<TItem>
    {
        public override TItem Get()
        {
            throw new NotImplementedException();
        }

        public override bool IsEmpty()
        {
            return false;
        }
    }

    internal class Some<TItem> : Option<TItem>
    {
        private TItem _a;

        public Some(TItem a)
        {
            _a = a;
        }

        public override TItem Get()
        {
            return _a;
        }

        public override bool IsEmpty()
        {
            return false;
        }
    }

    class Main1
    {
        /*
        public A Calculate<A>(A x)
        {
            return x;
        }
        */

        public A Calculate<A>(Func<A, A> x, A y)
        {
            return x(y);
        }
    }

    class Main2
    {
        public static void Main3()
        {
            Main1 f = new Main1();
            f.Calculate(x => x, 13);
        }
    }
}