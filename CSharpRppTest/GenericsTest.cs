using System;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CSharpRppTest
{
    [TestClass]
    public class GenericsTest
    {
        [TestMethod]
        [TestCategory("Generics")]
        public void SimplestPossibleGeneric()
        {
            const string code = @"
class Foo[T]
";
            Type fooTy = Utils.ParseAndCreateType(code, "Foo");
            Assert.IsNotNull(fooTy);
            Assert.IsTrue(fooTy.IsGenericType);
        }

        [TestMethod]
        [TestCategory("Generics")]
        public void SimplestPossibleGenericWithGenericField()
        {
            const string code = @"
class Foo[T](val k: T)
";
            Type fooTy = Utils.ParseAndCreateType(code, "Foo");
            Assert.IsNotNull(fooTy);
            Assert.IsTrue(fooTy.IsGenericType);
        }

        [TestMethod]
        [TestCategory("Generics")]
        public void ExtendingSimpleGenericClass()
        {
            const string code = @"
class Bar[T]
class Foo[T] extends Bar[T]
";
            Type fooTy = Utils.ParseAndCreateType(code, "Foo");
            Assert.IsNotNull(fooTy);
            Assert.IsTrue(fooTy.IsGenericType);
        }

        [TestMethod]
        [TestCategory("Generics")]
        public void ExtendingSimpleGenericClassWithPredefinedGenericArgument()
        {
            const string code = @"
class Bar[T]
class Foo[T] extends Bar[Int]
";
            Type fooTy = Utils.ParseAndCreateType(code, "Foo");
            Assert.IsNotNull(fooTy);
            Assert.IsTrue(fooTy.IsGenericType);
            Assert.AreEqual(typeof (int), fooTy.BaseType?.GenericTypeArguments[0]);
        }

        [TestMethod]
        [TestCategory("Generics")]
        public void ExtendingClassWithPredefinedGenericArgument()
        {
            const string code = @"
class Bar[T]
class Foo extends Bar[Int]
";
            Type fooTy = Utils.ParseAndCreateType(code, "Foo");
            Assert.IsNotNull(fooTy);
            Assert.IsFalse(fooTy.IsGenericType);
            Assert.AreEqual(typeof (int), fooTy.BaseType?.GenericTypeArguments[0]);
        }


        [TestMethod]
        [TestCategory("Generics")]
        public void GenericStaticFunction()
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
        [TestCategory("Generics")]
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
        [TestCategory("Generics")]
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
        [TestCategory("Generics")]
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
        [TestCategory("Generics")]
        public void ClosureLikeClass()
        {
            const string code = @"
abstract class Func[R, T]
{
    def apply(arg : T) : R
}

class MyClosure extends Func[Int, Float]
{
    override def apply(f: Float) : Int = {
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

        [TestMethod]
        public void GenericInNestedClasses()
        {
            const string code = @"
class First[A, B] {
  class Second[C, D] {
  }
}
";
            Type firstTy = Utils.ParseAndCreateType(code, "First");
            Assert.IsNotNull(firstTy);
            Assert.AreEqual(2, firstTy.GetGenericArguments().Length);
            CollectionAssert.AreEqual(new[] {"A", "B"}, firstTy.GetGenericArguments().Select(t => t.Name).ToList());
            Type secondTy = firstTy.GetNestedType("Second");
            CollectionAssert.AreEqual(new[] {"A", "B", "C", "D"}, secondTy.GetGenericArguments().Select(t => t.Name).ToList());
        }
    }
}