using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CSharpRppTest
{
    [TestClass]
    public class ExpressionTest
    {
        [TestMethod]
        public void TestIf()
        {
            const string code = @"
object Main
{
    def main(k : Int) : Int = if(k > 0) 13 else 23
}
";
            Type mainTy = Utils.ParseAndCreateType(code, "Main$");
            object res = Utils.InvokeStatic(mainTy, "main", new object[] {2});
            Assert.AreEqual(13, res);
            res = Utils.InvokeStatic(mainTy, "main", new object[] { -3 });
            Assert.AreEqual(23, res);
            res = Utils.InvokeStatic(mainTy, "main", new object[] { 0 });
            Assert.AreEqual(23, res);
        }
    }
}
