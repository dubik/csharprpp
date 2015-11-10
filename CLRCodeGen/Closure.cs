namespace CLRCodeGen
{
    class Closure
    {
        class MySum<TRes, T1, T2> 
        {
            public TRes Apply(T1 arg1, T2 arg2)
            {
                return default(TRes);
            }
        }

        class My<TA> : MySum<TA, TA, TA>
        {
            
        }

        public void DoSomething<TArg>()
        {
            MySum<int, float, float> s;
            MySum<int, float, TArg> f;
        }

        private delegate void Foo<TK>(TK k);
        public void DoAno<T>()
        {
            Foo<T> f = null;
            f(default(T));
        }
    }
}
