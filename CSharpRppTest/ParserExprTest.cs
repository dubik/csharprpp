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
        public void ParseFuncCallOneArg()
        {
            TestExpr("func(10)", Call("func", new IRppExpr[] {Int(10)}));
        }

        [TestMethod]
        public void ParseFuncCallTwoArgs()
        {
            TestExpr("func(10, x)", Call("func", new IRppExpr[] {Int(10), Id("x")}));
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
            IRppExpr actual;
            Assert.IsTrue(parser.ParsePath(out actual));
            Assert.IsNotNull(actual);
            Assert.AreEqual(Selector(Id("foo"), Field("bar")), actual);
        }

        [TestMethod]
        public void ParseMethodCall()
        {
            TestExpr("foo.MyFunc()", Selector(Id("foo"), FollowedCall("MyFunc")));
        }

        [TestMethod]
        public void ParseLongChainOfFieldsAndMethods()
        {
            TestExpr("foo.MyFunc().bar.Length()", Selector(Selector(Selector(Id("foo"), FollowedCall("MyFunc")), Field("bar")), FollowedCall("Length")));
        }

        [TestMethod]
        public void ParseEmptyBlockExpr()
        {
            TestExpr("{}", new RppBlockExpr(Collections.NoNodes));
        }

        [TestMethod]
        public void VarInBlockExpr()
        {
            RppBlockExpr blockExpr = ParserTest.CreateParser(@"{
        val k : String = new String
    }").ParseBlockExpr();

            Assert.IsNotNull(blockExpr);
        }

        [TestMethod]
        public void ParseLogical1()
        {
            TestExpr("x && y", LogicalAnd(Id("x"), Id("y")));
            TestExpr("x || y", LogicalOr(Id("x"), Id("y")));
        }

        [TestMethod]
        public void ParseLogical2()
        {
            TestExpr("x == 3 && y", LogicalAnd(Eq(Id("x"), Int(3)), Id("y")));
            TestExpr("x == 3 || y", LogicalOr(Eq(Id("x"), Int(3)), Id("y")));
        }

        [TestMethod]
        public void ParseLogical3()
        {
            TestExpr("x && y == 3", LogicalAnd(Id("x"), Eq(Id("y"), Int(3))));
            TestExpr("x || y == 3", LogicalOr(Id("x"), Eq(Id("y"), Int(3))));
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
            return new RppInteger(value);
        }

        private static RppBinOp Add(IRppExpr left, IRppExpr right)
        {
            return RppBinOp.Create("+", left, right);
        }

        private static RppBinOp Sub(IRppExpr left, IRppExpr right)
        {
            return RppBinOp.Create("-", left, right);
        }

        private static RppBinOp Mult(IRppExpr left, IRppExpr right)
        {
            return RppBinOp.Create("*", left, right);
        }

        private static RppBinOp LogicalAnd(IRppExpr left, IRppExpr right)
        {
            return RppBinOp.Create("&&", left, right);
        }

        private static RppBinOp LogicalOr(IRppExpr left, IRppExpr right)
        {
            return RppBinOp.Create("||", left, right);
        }

        private static RppBinOp Eq(IRppExpr left, IRppExpr right)
        {
            return RppBinOp.Create("==", left, right);
        }

        private static RppId Id(string id)
        {
            return new RppId(id);
        }

        private static RppFieldSelector Field(string field)
        {
            return new RppFieldSelector(field);
        }

        private static RppFuncCall Call(string id)
        {
            return new RppFuncCall(id, new List<IRppExpr>());
        }

        private static RppFuncCall Call(string id, IList<IRppExpr> args)
        {
            return new RppFuncCall(id, args);
        }

        private static RppFuncCall FollowedCall(string id)
        {
            return new RppFuncCall(id, new List<IRppExpr>());
        }

        private static RppFuncCall FollowedCall(string id, IList<IRppExpr> args)
        {
            return new RppFuncCall(id, args);
        }

        private static RppSelector Selector(IRppExpr expr, RppMember member)
        {
            return new RppSelector(expr, member);
        }

        #endregion
    }
}