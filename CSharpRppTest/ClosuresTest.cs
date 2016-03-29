using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static CSharpRppTest.Utils;

namespace CSharpRppTest
{
    [TestClass]
    public class ClosuresTest
    {
        [TestMethod]
        [TestCategory("Closures"), TestCategory("Generics")]
        public void ParseAndResolveClosureType()
        {
            const string code = @"
object Bar
{
    def main : Unit = {
        var k : (Int, Int) => Int = null
    }
}
";
            ParseAndCreateType(code, "Bar$");
        }

        [TestMethod]
        [TestCategory("Closures"), TestCategory("Generics")]
        public void ParseSimpleClosure()
        {
            const string code = @"
object Bar
{
    def main() : Int = {
        var func: (Int, Int) => Boolean = (x: Int, y: Int) => x < y
        10
    }
}
";
            var barTy = ParseAndCreateType(code, "Bar$");
            Assert.IsNotNull(barTy);
            MethodInfo mainMethod = barTy.GetMethod("main", BindingFlags.Public | BindingFlags.Instance);
            Assert.IsNotNull(mainMethod);
        }

        [TestMethod]
        [TestCategory("Closures")]
        public void CallClosure()
        {
            const string code = @"
object Bar
{
    def main() : Int = {
        var func: (Int, Int) => Int = (x: Int, y: Int) => x + y
        func(10, 13)
    }
}
";
            var barTy = ParseAndCreateType(code, "Bar$");
            Assert.IsNotNull(barTy);
            var res = InvokeStatic(barTy, "main");
            Assert.AreEqual(23, res);
        }

        [TestMethod, TestCategory("Closures"), TestCategory("Generics")]
        public void PassClosureAsAParam()
        {
            const string code = @"
object Bar
{
    def invoker(func: (Int, Int) => Int) : Int = {
        func(10, 24)
    }

    def main() : Int = {
        var func: (Int, Int) => Int = (x: Int, y: Int) => x + y
        invoker(func)
    }
}
";
            var barTy = ParseAndCreateType(code, "Bar$");
            var res = InvokeStatic(barTy, "main");
            Assert.AreEqual(34, res);
        }

        [TestMethod, TestCategory("Closures"), TestCategory("Generics")]
        public void GenericMethod()
        {
            const string code = @"
object Bar
{
    def func[A](k : A) : A =  {
         val f: (A) => A = (x : A) => x
         f[A](k)
    }
}
";
            var barTy = ParseAndCreateType(code, "Bar$");
        }

        [TestMethod, TestCategory("Closures"), TestCategory("Generics")]
        public void ParseOneParamClosureWithoutParents()
        {
            const string code = @"
object Main
{
    def calc(func: Int => Int, v : Int) : Int = func(v)
    def main : Int = {
        calc(x => x + 1, 13)
    }
}
";
            var mainTy = ParseAndCreateType(code, "Main$");
            var res = InvokeStatic(mainTy, "main");
            Assert.AreEqual(14, res);
        }

        [TestMethod, TestCategory("Closures")]
        public void FiguringOutTypesOfSimpleClosureTypeInAGenericClass()
        {
            const string code = @"
class Foo[A](val k: A) {
  def map(f: A => A): Foo[A] = new Foo(f(k))
}

object Main {
  def main: Foo[Int] = {
    val foo = new Foo(12)
    foo.map(x => x * 2)
  }
}
";
            var mainTy = ParseAndCreateType(code, "Main$");
            var res = InvokeStatic(mainTy, "main");
            Assert.IsNotNull(res);
            Assert.AreEqual(24, res.GetPropertyValue("k"));
        }

        [TestMethod]
        public void ReturnTypeForClosure()
        {
            /*
            const string code = @"
class Bar[A, B]
{
    def func(i: A, k: B) : B = {
        val f: (A, B) => B = (x: A, y: B) => x
        f[A, B](i, k)
    }
}
";
*/
            //var barTy = Utils.ParseAndCreateType(code, "Bar$");
        }

        [TestMethod, TestCategory("Closures")]
        public void UseClosureInClassParam()
        {
            const string code = @"
class Foo(val f: Int => Boolean) {
    def isItTwo(v: Int) : Boolean = f(v)
}

object Main {
    def main(v: Int) : Boolean = {
        val foo = new Foo((k) => k == 13)
        foo.isItTwo(v)
    }
}
";
            var mainTy = ParseAndCreateType(code, "Main$");
            var res = InvokeStatic(mainTy, "main", new object[] {13});
            Assert.IsTrue((bool) res);
        }

        [TestMethod, TestCategory("Closures")]
        public void CaptureLocalVariable()
        {
            const string code = @"
object Main {
    def returnTheSame(v: Int) : Int = v
    def main : Int = {
        val foo = 13
        val func = () => returnTheSame(foo)
        func()
    }
}
";
            var mainTy = ParseAndCreateType(code, "Main$");
            var res = InvokeStatic(mainTy, "main");
            Assert.AreEqual(13, res);
        }

        [TestMethod, TestCategory("Closures")]
        public void CapturedVariableUsedInExpressions()
        {
            const string code = @"
object Main {
    def returnTheSame(v: Int) : Int = v
    def main : Int = {
        val foo = 13
        val func = () => foo = 27
        func()
        foo + 13
    }
}
";
            var mainTy = ParseAndCreateType(code, "Main$");
            var res = InvokeStatic(mainTy, "main");
            Assert.AreEqual(40, res);
        }

        [TestMethod, TestCategory("Closures")]
        public void StoringToCapturedVariable()
        {
            const string code = @"
object Main {
    def main : Int = {
        val foo = 13
        val func = () => foo = 27
        func()
        foo = 1
        foo
    }
}
";
            var mainTy = ParseAndCreateType(code, "Main$");
            var res = InvokeStatic(mainTy, "main");
            Assert.AreEqual(1, res);
        }

        [TestMethod, TestCategory("Closures")]
        public void CaptureSeveralVariables()
        {
            const string code = @"
object Main {
    def main : Int = {
        val foo = 13
        val bar = 27
        val func = () => foo + bar
        val z = func()
        foo + bar + z
    }
}
";
            var mainTy = ParseAndCreateType(code, "Main$");
            var res = InvokeStatic(mainTy, "main");
            Assert.AreEqual(80, res);
        }


        [TestMethod, TestCategory("Closures")]
        public void ClosureWhichDoesntReturnValue()
        {
            const string code = @"
object Main {
    protected def twice(f: () => Unit): Unit = {
        f()
        f()
    }

    def count: Int = {
        var c = 0
        twice(() => c = c + 1)
        c
    }

    def main : Int = count()
}
";
            var mainTy = ParseAndCreateType(code, "Main$");
            var res = InvokeStatic(mainTy, "main");
            Assert.AreEqual(2, res);
        }
    }
}