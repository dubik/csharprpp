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
            res = Utils.InvokeStatic(mainTy, "main", new object[] {-3});
            Assert.AreEqual(23, res);
            res = Utils.InvokeStatic(mainTy, "main", new object[] {0});
            Assert.AreEqual(23, res);
        }

        [TestMethod]
        public void PopSimpleIntIfFuncReturnsUnit()
        {
            const string code = @"
object Main
{
    def main : Unit = 10
}
";
            Type mainTy = Utils.ParseAndCreateType(code, "Main$");
            object res = Utils.InvokeStatic(mainTy, "main");
            Assert.IsNull(res);
        }

        [TestMethod]
        public void PopSimpleIntFromBlockExprIfFuncReturnsUnit()
        {
            const string code = @"
object Main
{
    def main : Unit = {
        10
    }
}
";
            Type mainTy = Utils.ParseAndCreateType(code, "Main$");
            object res = Utils.InvokeStatic(mainTy, "main");
            Assert.IsNull(res);
        }

        [TestMethod]
        public void PopResultOfFuncIfFuncReturnsUnit()
        {
            const string code = @"
object Main
{
    def calc : Int = 13
    def main : Unit = calc()
}
";
            Type mainTy = Utils.ParseAndCreateType(code, "Main$");
            object res = Utils.InvokeStatic(mainTy, "main");
            Assert.IsNull(res);
        }

        [TestMethod]
        public void UnitFuncShouldntPopIfLastExprAlreadyUnit()
        {
            const string code = @"
object Main
{
    def calc : Unit = 13
    def main : Unit = calc()
}";
            Type mainTy = Utils.ParseAndCreateType(code, "Main$");
            object res = Utils.InvokeStatic(mainTy, "main");
            Assert.IsNull(res);
        }

        [TestMethod]
        public void UnitFuncShouldntPopForVarDeclaration()
        {
            const string code = @"
object Main
{
    def main : Unit = {
        val k : Int = 1
    }
}";
            Type mainTy = Utils.ParseAndCreateType(code, "Main$");
            object res = Utils.InvokeStatic(mainTy, "main");
            Assert.IsNull(res);
        }

        [TestMethod]
        public void TestEqualOperator()
        {
            const string code = @"
object Foo {
    def main : Int = {
        val k = 13
        if(k == 10) {
            3
        } else {
            5
        }
    }
}";
            Type mainTy = Utils.ParseAndCreateType(code, "Foo$");
            object res = Utils.InvokeStatic(mainTy, "main");
            Assert.AreEqual(5, res);
        }
    }
}