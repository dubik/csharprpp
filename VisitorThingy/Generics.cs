using System.Reflection;
using Type = System.Type;

namespace VisitorThingy
{
    class Foo<T>
    {
        public Foo<int> Field;
        public Foo<T> Field1;
        public T Field2;
    }

    class Generics
    {
        public static void MainA()
        {
            Type typeOfFoo = typeof (Foo<>);
            FieldInfo[] info = typeOfFoo.GetFields();
        }
    }
}