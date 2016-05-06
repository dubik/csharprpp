using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CSharpRpp.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CSharpRppTest
{
    [TestClass]
    public class ClassTest
    {
        [TestMethod]
        public void TestClassWithoutBody()
        {
            const string code = @"
class Bar

class Foo(val id: Int)

class Oor extends Bar
";

            var types = Utils.ParseAndCreateTypes(code, new List<string> {"Bar", "Foo", "Oor"}).ToArray();
            var barTy = types[0];
            var fooTy = types[1];
            var oorTy = types[2];
            Assert.IsNotNull(barTy);
            Assert.IsTrue(oorTy.IsSubclassOf(barTy));
            Assert.IsNotNull(fooTy.GetProperty("id"));
        }


        [TestMethod]
        public void TestEmptyClassExtend()
        {
            const string code = @"
class Foo
{
}

class Bar extends Foo
{
}
";
            var types = Utils.ParseAndCreateTypes(code, new List<string> {"Foo", "Bar"}).ToArray();
            var fooTy = types[0];
            var barTy = types[1];
            Assert.IsTrue(barTy.IsSubclassOf(fooTy));
        }

        [TestMethod]
        public void ResolveFuncFromBaseClass()
        {
            const string code = @"
class Foo
{
    def calculate(k : Int) : Int = k + 10
}

class Bar extends Foo
{
    def main() : Int = calculate(13)
}
";
            var types = Utils.ParseAndCreateTypes(code, new List<string> {"Foo", "Bar"}).ToArray();
            var fooTy = types[0];
            var barTy = types[1];
            Assert.IsTrue(barTy.IsSubclassOf(fooTy));
        }

        [TestMethod]
        public void InstantiateClassWhichInheritsAnotherClass()
        {
            const string code = @"
class Foo
{
}

class Bar extends Foo
{
}

object Main
{
    def main() : Foo = {
        val k : Foo = new Bar
        k
    }
}
";
            var mainTy = Utils.ParseAndCreateType(code, "Main$");
            var res = Utils.InvokeStatic(mainTy, "main");
            Assert.IsNotNull(res);
            Assert.AreEqual("Bar", res.GetType().Name);
        }

        [TestMethod]
        public void NonTrivialConstructor()
        {
            const string code = @"
class Foo(var k : Int)
{
    k = 13
}

object Main
{
    def main() : Foo = new Foo(1)
}
";
            var mainTy = Utils.ParseAndCreateType(code, "Main$");
            var res = Utils.InvokeStatic(mainTy, "main");
            Assert.IsNotNull(res);
            var fieldInfo = res.GetType().GetProperty("k");
            var k = fieldInfo.GetValue(res);
            Assert.AreEqual(13, k);
        }


        [TestMethod]
        public void InheritingClassWithClassParams()
        {
            const string code = @"
class Foo(var k : Int)
{
}

class Bar(var k: Int) extends Foo(k)
{
}

";
            var barTy = Utils.ParseAndCreateType(code, "Bar");
            Assert.IsNotNull(barTy);
        }

        [TestMethod]
        public void ValShouldMakeParamAField()
        {
            const string code = @"
class Foo(val k : Int)
{
}
";
            var fooTy = Utils.ParseAndCreateType(code, "Foo");
            Assert.IsNotNull(fooTy.GetProperty("k"));
        }

        [TestMethod]
        public void NoModificatorShouldntPromotClassParamToField()
        {
            const string code = @"
class Foo(k : Int)
{
}
";
            var fooTy = Utils.ParseAndCreateType(code, "Foo");
            Assert.IsNull(fooTy.GetProperty("k"));
        }

        [TestMethod]
        public void TestParentPrimaryConstructorCalled()
        {
            const string code = @"
class Foo(var k : Int)
{
}

class Bar(k: Int) extends Foo(k)
{
}

object Main
{
    def get() : Int = {
        val inst : Foo = new Bar(13)
        inst.k
    }
}
";
            var mainTy = Utils.ParseAndCreateType(code, "Main$");
            var res = Utils.InvokeStatic(mainTy, "get");
            Assert.AreEqual(13, res);
        }

        [TestMethod]
        public void TestParentSecondayConstructorCalled()
        {
            const string code = @"
class Foo(var k: Int)
{
    def this() = this(27)
}

class Bar extends Foo

object Main
{
    def get() : Int = {
        val inst : Foo = new Bar
        inst.k
    }
}
";
            var mainTy = Utils.ParseAndCreateType(code, "Main$");
            var res = Utils.InvokeStatic(mainTy, "get");
            Assert.AreEqual(27, res);
        }

        [TestMethod]
        [TestCategory("Generics")]
        public void ParseClassGenericArg()
        {
            const string code = @"
class Foo[T]
";
            var fooTy = Utils.ParseAndCreateType(code, "Foo");
            Assert.IsNotNull(fooTy);
            Assert.IsTrue(fooTy.IsGenericType);
            Assert.AreEqual(1, fooTy.GetGenericArguments().Length);
        }

        [TestMethod]
        [TestCategory("Generics")]
        public void DeclareGenericField()
        {
            const string code = @"
class Foo[T](val k : T)
";
            var fooTy = Utils.ParseAndCreateType(code, "Foo");
            Assert.IsNotNull(fooTy);
            Assert.IsTrue(fooTy.IsGenericType);
        }

        [TestMethod]
        [TestCategory("Generics")]
        public void InstantiateGenericClass()
        {
            const string code = @"
class Foo[T]

object Bar
{
    def main() : Foo[Int] = {
        val k : Foo[Int] = new Foo[Int]()
        k
    }
}
";
            var barTy = Utils.ParseAndCreateType(code, "Bar$");
            var res = Utils.InvokeStatic(barTy, "main");
            Assert.IsNotNull(res);
        }

        [TestMethod]
        public void InvokeOverridenMethod()
        {
            const string code = @"
class Human
{
    def getId : Int = 10
}

class Person extends Human
{
    override def getId : Int = 13
}

object Main
{
    def main : Int = {
        val k : Human = new Person
        k.getId()
    }
}
";
            var mainTy = Utils.ParseAndCreateType(code, "Main$");
            var res = Utils.InvokeStatic(mainTy, "main");
            Assert.AreEqual(13, res);
        }

        [TestMethod]
        public void EmptyAbstractClass()
        {
            const string code = @"
abstract class Foo
";
            Type fooTy = Utils.ParseAndCreateType(code, "Foo");
            Assert.IsNotNull(fooTy);
            Assert.IsTrue(fooTy.IsAbstract);
        }

        [TestMethod]
        public void HaveAbstractMethod()
        {
            const string code = @"
abstract class Foo
{
    def length: Int
}
";
            Type fooTy = Utils.ParseAndCreateType(code, "Foo");
            Assert.IsNotNull(fooTy);
            MethodInfo lengthMethod = fooTy.GetMethod("length", BindingFlags.Public | BindingFlags.Instance);
            Assert.IsNotNull(lengthMethod);
            Assert.IsTrue(lengthMethod.IsAbstract);
            Assert.IsTrue(lengthMethod.IsVirtual);
        }

        [TestMethod]
        [ExpectedException(typeof (SemanticException))]
        public void MissingAbstractMethodShouldThrowAnException()
        {
            const string code = @"
abstract class Foo
{
    def length: Int
    def length(k : Int) : Int
}

class Bar extends Foo
{
}
";
            Utils.ParseAndCreateType(code, "Bar");
        }

        // TODO enable this
        /*
        [TestMethod]
        [Ignore]
        [ExpectedException(typeof (SemanticException))]
        public void MissingAbstractModifierForEmptyMethods()
        {
            const string code = @"
class Foo
{
    def length : Int
}
";
            Utils.ParseAndCreateType(code, "Foo");
        }
        */

        [TestMethod]
        public void SecondaryConstructor()
        {
            const string code = @"
class Foo(length: Int)
{
    def this() = this(10)
}
";
            var fooTy = Utils.ParseAndCreateType(code, "Foo");
            Assert.IsNotNull(fooTy);
            ConstructorInfo[] constructors = fooTy.GetConstructors();
            Assert.AreEqual(2, constructors.Length);
        }

        [TestMethod]
        public void CreateObjectWithSecondaryConstructor()
        {
            const string code = @"
class Foo(val length: Int)
{
    def this() = this(13)
}

object Main
{
    def main : Foo = new Foo
}
";
            var mainTy = Utils.ParseAndCreateType(code, "Main$");
            Assert.IsNotNull(mainTy);
            var fooInst = Utils.InvokeStatic(mainTy, "main");
            Assert.AreEqual(13, fooInst.GetPropertyValue("length"));
        }

        [TestMethod]
        [TestCategory("Generics")]
        public void GenericBaseConstructor()
        {
            const string code = @"
class MyOption[A]
class MySome[A](val a: A) extends MyOption[A]

object Main
{
    def main : Int = {
        val k : MySome[Int] = new MySome[Int](123)
        k.a
    }
}
";
            var mainTy = Utils.ParseAndCreateType(code, "Main$");
            var res = Utils.InvokeStatic(mainTy, "main");
            Assert.AreEqual(123, res);
        }

        [TestMethod]
        [TestCategory("Generics")]
        public void AssignGenericFieldToAVar()
        {
            const string code = @"
class TSome[A](val a: A)

object Main
{
    def main : Int = {
        val k : TSome[Int] = new TSome[Int](123)
        val p = k.a
        p
    }
}
";
            var mainTy = Utils.ParseAndCreateType(code, "Main$");
            var res = Utils.InvokeStatic(mainTy, "main");
            Assert.AreEqual(123, res);
        }

        [TestMethod]
        [TestCategory("Generics")]
        public void SpecifyTypesForBaseClass()
        {
            const string code = @"
class TOption[A](val x : A)

class SomeInt(x : Int) extends TOption[Int](x)

object Main
{
    def main : Int = {
        val k : SomeInt = new SomeInt(123)
        k.x
    }
}
";
            var mainTy = Utils.ParseAndCreateType(code, "Main$");
            var res = Utils.InvokeStatic(mainTy, "main");
            Assert.AreEqual(123, res);
        }

        [TestMethod]
        [TestCategory("Generics")]
        public void OptionMonad()
        {
            const string code = @"
abstract class TOption[+A]
{
    def isEmpty : Boolean
    def get: A
    def map[B](f: (A) => B): TOption[B] = if(isEmpty()) TNone else new TSome[B](f(get()))
    def flatMap[B](f: (A) => TOption[B]): TOption[B] = if(isEmpty()) TNone else f(get())
}

class TSome[A](val x: A) extends TOption[A]
{
    override def isEmpty : Boolean = false
    override def get : A = x
}

object TNone extends TOption[Nothing]
{
    override def isEmpty : Boolean = true
    override def get : Nothing = throw new Exception(""Nothing to get"")
}

object Main
{
    def main : Int = {
        val k : TSome[Int] = new TSome[Int](123)
        val p = k.x
        p
    }
}
";
            var mainTy = Utils.ParseAndCreateType(code, "Main$");
            var res = Utils.InvokeStatic(mainTy, "main");
            Assert.AreEqual(123, res);
        }

        [TestMethod]
        public void AccessFieldFromBaseClass()
        {
            const string code = @"
class Foo(var k: Int)
{
    def this() = this(27)
}

class Bar extends Foo

object Main
{
    def get() : Int = {
        val inst : Bar = new Bar
        inst.k
    }
}
";
            var mainTy = Utils.ParseAndCreateType(code, "Main$");
            var res = Utils.InvokeStatic(mainTy, "get");
            Assert.AreEqual(27, res);
        }

        [TestMethod]
        public void CreateSealedClass()
        {
            const string code = @"sealed class Foo";
            var fooTy = Utils.ParseAndCreateType(code, "Foo");
            Assert.IsTrue(fooTy.IsSealed);
        }

        [TestMethod]
        public void TestClassAndObjectWithTheSameName()
        {
            const string code = @"
class Foo

object Foo
{
    def calc: Int = 13
}

object Main
{
    def callObject: Int = Foo.calc()
    def callClass : Foo = new Foo
}

";
            var mainTy = Utils.ParseAndCreateType(code, "Main$");
            var res = Utils.InvokeStatic(mainTy, "callObject");
            Assert.AreEqual(13, res);
            var fooInst = Utils.InvokeStatic(mainTy, "callClass");
            Assert.IsNotNull(fooInst);
            Assert.AreEqual("Foo", fooInst.GetType().Name);
        }

        [TestMethod]
        public void ChainCalls()
        {
            const string code = @"
class Foo {
    def calculate() : Int = 13
    def myself() : Foo = new Foo
    def subCalc() : Int = myself().calculate()
}
";
            var fooTy = Utils.ParseAndCreateType(code, "Foo");
            Assert.IsNotNull(fooTy);
            object foo = Activator.CreateInstance(fooTy);
            object res = fooTy.GetMethod("subCalc").Invoke(foo, null);
            Assert.AreEqual(13, res);
        }

        [TestMethod]
        public void ArgumentOfChainedCallIsFunction()
        {
            const string code = @"
class Foo {
    def getInt(i: Int) : Int = i
}

object Main {
    def getMainInt: Int = 27
    def mainn: Int = {
        val foo = new Foo
        foo.getInt(getMainInt())
    }
}
";
            Type mainTy = Utils.ParseAndCreateType(code, "Main$");
            object res = Utils.InvokeStatic(mainTy, "mainn");
            Assert.AreEqual(27, res);
        }

        [TestMethod]
        public void WriteToProperty()
        {
            const string code = @"
class Properties(var length: Int) {

}

object Main {
  def main: Properties = {
    val p = new Properties(13)
    p.length = 27
    p
  }
}
";
            Type mainTy = Utils.ParseAndCreateType(code, "Main$");
            Assert.IsNotNull(mainTy);
            object propertiesInst = Utils.InvokeStatic(mainTy, "main");
            Assert.AreEqual(27, propertiesInst.GetPropertyValue("length"));
        }

        [TestMethod]
        public void WriteToGenericProperty()
        {
            const string code = @"
class Properties[A](var item: A) {

}

object Main {
  def main: Properties[Int] = {
    val p = new Properties(13)
    p.item = 27
    p
  }
}
";
            Type mainTy = Utils.ParseAndCreateType(code, "Main$");
            Assert.IsNotNull(mainTy);
            object propertiesInst = Utils.InvokeStatic(mainTy, "main");
            Assert.AreEqual(27, propertiesInst.GetPropertyValue("item"));
        }

        [TestMethod]
        public void CallingFunctionsWithoutParens()
        {
            const string code = @"
class Foo {
    def func: Int = 13
}

object Main {
    def main : Int = {
        val f = new Foo
        f.func
    }
}
";
            Type mainTy = Utils.ParseAndCreateType(code, "Main$");
            object res = Utils.InvokeStatic(mainTy, "main");
            Assert.AreEqual(13, res);
        }
    }
}