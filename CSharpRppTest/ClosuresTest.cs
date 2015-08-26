using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RppRuntime;

namespace CSharpRppTest
{
    [TestClass]
    public class ClosuresTest
    {
        [TestMethod]
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
            Utils.ParseAndCreateType(code, "Bar$", typeof (Function2<,,>));
        }

        [TestMethod]
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
            var barTy = Utils.ParseAndCreateType(code, "Bar$", typeof(Function2<,,>));
            Assert.IsNotNull(barTy);
            MethodInfo mainMethod = barTy.GetMethod("main", BindingFlags.Public | BindingFlags.Instance);
            Assert.IsNotNull(mainMethod);
        }

        [TestMethod]
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
            var barTy = Utils.ParseAndCreateType(code, "Bar$", typeof (Function2<,,>));
            Assert.IsNotNull(barTy);
            var res = Utils.InvokeStatic(barTy, "main");
            Assert.AreEqual(23, res);
        }

        [TestMethod]
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
            var barTy = Utils.ParseAndCreateType(code, "Bar$", typeof(Function2<,,>));
            var res = Utils.InvokeStatic(barTy, "main");
            Assert.AreEqual(34, res);
        }
    }
}