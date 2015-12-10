using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
            Utils.ParseAndCreateType(code, "Bar$");
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
            var barTy = Utils.ParseAndCreateType(code, "Bar$");
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
            var barTy = Utils.ParseAndCreateType(code, "Bar$");
            Assert.IsNotNull(barTy);
            var res = Utils.InvokeStatic(barTy, "main");
            Assert.AreEqual(23, res);
        }

        [TestMethod]
        [TestCategory("Closures"), TestCategory("Generics")]
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
            var barTy = Utils.ParseAndCreateType(code, "Bar$");
            var res = Utils.InvokeStatic(barTy, "main");
            Assert.AreEqual(34, res);
        }

        [TestMethod]
        [TestCategory("Closures"), TestCategory("Generics")]
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
            var barTy = Utils.ParseAndCreateType(code, "Bar$");
        }

        [TestMethod]
        [TestCategory("Closures"), TestCategory("Generics")]
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
            var mainTy = Utils.ParseAndCreateType(code, "Main$");
            var res = Utils.InvokeStatic(mainTy, "main");
            Assert.AreEqual(14, res);
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
    }
}