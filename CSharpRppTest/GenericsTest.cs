using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CSharpRppTest
{
    [TestClass]
    public class GenericsTest
    {
        [TestMethod]
        public void GenericFunction()
        {
            const string code = @"
object Bar
{
    def func[A](x: A) : A = x

    def main(name: String) : String = func[String](name)
}
";
            Type barTy = Utils.ParseAndCreateType(code, "Bar$");
            var res = Utils.InvokeStatic(barTy, "main", new object[] {"hello"});
            Assert.AreEqual("hello", res);
        }

        [TestMethod]
        public void SimpleGenericClass()
        {
            const string code = @"
class Foo[T](val id: T)

object Bar
{
    def main : Foo[Int] = new Foo[Int](10)
}
";
            Type barTy = Utils.ParseAndCreateType(code, "Bar$");
            var res = Utils.InvokeStatic(barTy, "main");
            Assert.IsNotNull(res);
        }

        [TestMethod]
        public void ExtendingSpecializedGeneric()
        {
            const string code = @"
class Foo[T](val id: T)

class SecondFoo(id: Int) extends Foo[Int](id)

object Bar
{
    def main : Foo[Int] = new SecondFoo(10)
}
";
            Type barTy = Utils.ParseAndCreateType(code, "Bar$");
            var res = Utils.InvokeStatic(barTy, "main");
            Assert.IsNotNull(res);
        }

        [TestMethod]
        public void ExtendingSpecializedGenericAndDefineOneMoreGenericParameter()
        {
            const string code = @"
class Foo[T](val id: T)

class SecondFoo[A](id: Int, val name: A) extends Foo[Int](id)

object Bar
{
    def main : Foo[Int] = new SecondFoo[String](10, ""Hello"")
}
";
            Type barTy = Utils.ParseAndCreateType(code, "Bar$");
            var res = Utils.InvokeStatic(barTy, "main");
            Assert.IsNotNull(res);
        }

        [TestMethod]
        public void ClosureLikeClass()
        {
            const string code = @"
abstract class Func[T, R]
{
    def apply(arg : T) : R
}

class MyClosure extends Func[Int, Float]
{
    def apply(f: Float) : Int = {
        10
    }
}

object Bar {
    def main : Int = {
        val f : Func[Int, Float] = new MyClosure()
        f.apply(12.3)
    }
}
";
            Type barTy = Utils.ParseAndCreateType(code, "Bar$");
            var res = Utils.InvokeStatic(barTy, "main");
            Assert.IsNotNull(res);
        }
    }
}