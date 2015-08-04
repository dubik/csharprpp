using System;
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
        var k: (Int, Int) => Boolean = (x: Int, y: Int) => x < y
        10
    }
}
";
            var barTy = Utils.ParseAndCreateType(code, "Bar$", typeof(Function2<,,>));
            Assert.IsNotNull(barTy);
            MethodInfo mainMethod = barTy.GetMethod("main", BindingFlags.Static | BindingFlags.Public);
            Assert.IsNotNull(mainMethod);
        }
    }
}