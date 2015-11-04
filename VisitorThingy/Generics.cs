using Type = System.Type;

namespace VisitorThingy
{
    class Foo<T>
    {
    }

    class Generics
    {

        public static void MainA()
        {
            Type typeOfFoo = typeof (Foo<>);

        }
    }
}
