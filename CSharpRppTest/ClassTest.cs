using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
            Assert.IsNotNull(fooTy.GetField("id"));
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
            MethodInfo mainMethod = mainTy.GetMethod("main", BindingFlags.Static | BindingFlags.Public);
            var res = mainMethod.Invoke(null, null);
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
            MethodInfo mainMethod = mainTy.GetMethod("main", BindingFlags.Static | BindingFlags.Public);
            var res = mainMethod.Invoke(null, null);
            Assert.IsNotNull(res);
            var fieldInfo = res.GetType().GetField("k");
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
        public void VarShouldMakeParamAField()
        {
            const string code = @"
class Foo(var k : Int)
{
}
";
            var fooTy = Utils.ParseAndCreateType(code, "Foo");
            Assert.IsNotNull(fooTy.GetField("k"));
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
            Assert.IsNotNull(fooTy.GetField("k"));
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
            Assert.IsNull(fooTy.GetField("k"));
        }

        [TestMethod]
        public void TestParentConstructorCalled()
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
            MethodInfo getMethod = mainTy.GetMethod("get", BindingFlags.Static | BindingFlags.Public);
            var res = getMethod.Invoke(null, null);
            Assert.AreEqual(13, res);
        }

        [TestMethod]
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
            MethodInfo mainMethod = barTy.GetMethod("main", BindingFlags.Static | BindingFlags.Public);
            var res = mainMethod.Invoke(null, null);
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
    def getId : Int = 13
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
            MethodInfo mainMethod = mainTy.GetMethod("main", BindingFlags.Static | BindingFlags.Public);
            var res = mainMethod.Invoke(null, null);
            Assert.AreEqual(13, res);
        }
    }
}