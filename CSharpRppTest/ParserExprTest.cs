using System.Collections.Generic;
using System.Globalization;
using CSharpRpp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CSharpRppTest
{
    [TestClass]
    public class ParserExprTest
    {
        [TestMethod]
        public void ParseSimpleIntExpr()
        {
            TestExpr("10", Int(10));
        }

        [TestMethod]
        public void ParseSimplePlusExpr()
        {
            TestExpr("10 + 5", Add(Int(10), Int(5)));
        }

        [TestMethod]
        public void ParseSimpleIdExpr()
        {
            TestExpr("myVar", Id("myVar"));
        }

        [TestMethod]
        public void ParseParenExpr()
        {
            TestExpr("(3 + 2) * 2", Mult(Add(Int(3), Int(2)), Int(2)));
        }

        [TestMethod]
        public void ParseComplexParenExpr()
        {
            TestExpr("(3 + 2) * (10 - 4)", Mult(Add(Int(3), Int(2)), Sub(Int(10), Int(4))));
        }

        [TestMethod]
        public void ParseFuncCallExpr()
        {
            TestExpr("func()", Call("func"));
        }

        [TestMethod]
        public void ParseSubWithIntAndId()
        {
            TestExpr("10 - x", Sub(Int(10), Id("x")));
        }

        [TestMethod]
        public void ParseSimplePath()
        {
            var parser = ParserTest.CreateParser("foo.bar");
            IRppExpr selector;
            Assert.IsTrue(parser.ParsePath(out selector));
            Assert.IsNotNull(selector);
        }

        private static void TestExpr(string code, IRppExpr expected)
        {
            var parser = ParserTest.CreateParser(code);
            IRppExpr expr = parser.ParseExpr();
            Assert.AreEqual(expected, expr);
        }

        #region Helpers

        private static RppInteger Int(int value)
        {
            return new RppInteger(value.ToString(CultureInfo.InvariantCulture));
        }

        private static BinOp Add(IRppExpr left, IRppExpr right)
        {
            return new BinOp("+", left, right);
        }

        private static BinOp Sub(IRppExpr left, IRppExpr right)
        {
            return new BinOp("-", left, right);
        }

        private static BinOp Mult(IRppExpr left, IRppExpr right)
        {
            return new BinOp("*", left, right);
        }

        private IRppExpr Id(string id)
        {
            return new RppId(id);
        }

        private IRppExpr Call(string id)
        {
            return new RppFuncCall(id, new List<IRppExpr>());
        }

        #endregion
    }
}