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
    }
}