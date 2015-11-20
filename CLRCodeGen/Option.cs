using System;

namespace CLRCodeGen.Monads
{
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
}