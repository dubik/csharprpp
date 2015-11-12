namespace CLRCodeGen.Generics
{
    internal class First<A, B>
    {
        private class Second<C, D>
        {
            public void Func<E, G>()
            {
                A a = default(A);
                D d = default(D);
            }
        }
    }
}