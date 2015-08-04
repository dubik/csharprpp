using System;
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
            Type barTy = Utils.ParseAndCreateType(code, "Bar$", typeof (Function2<,,>));
        }
    }
}