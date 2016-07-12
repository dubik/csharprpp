using System.Collections.Generic;
using System.Globalization;
using CSharpRpp;
using NUnit.Framework;

namespace CSharpRppTest
{
    [TestFixture]
    public class ParserExprTest
    {
        [Test]
        public void ParseSimpleIntExpr()
        {
            TestExpr("10", Int(10));
        }

        [Test]
        public void ParseSimplePlusExpr()
        {
            TestExpr("10 + 5", Add(Int(10), Int(5)));
        }

        [Test]
        public void ParseSimpleIdExpr()
        {
            TestExpr("myVar", Id("myVar"));
        }

        [Test]
        public void ParseParenExpr()
        {
            TestExpr("(3 + 2) * 2", Mult(Add(Int(3), Int(2)), Int(2)));
        }

        [Test]
        public void ParseComplexParenExpr()
        {
            TestExpr("(3 + 2) * (10 - 4)", Mult(Add(Int(3), Int(2)), Sub(Int(10), Int(4))));
        }

        [Test]
        public void ParseFuncCallExpr()
        {
            TestExpr("func()", Call("func"));
        }

        [Test]
        public void ParseFuncCallOneArg()
        {
            TestExpr("func(10)", Call("func", new IRppExpr[] {Int(10)}));
        }

        [Test]
        public void ParseFuncCallTwoArgs()
        {
            TestExpr("func(10, x)", Call("func", new IRppExpr[] {Int(10), Id("x")}));
        }

        [Test]
        public void ParseSubWithIntAndId()
        {
            TestExpr("10 - x", Sub(Int(10), Id("x")));
        }

        [Test]
        public void ParseSimplePath()
        {
            var parser = ParserTest.CreateParser("foo.bar");
            IRppExpr actual;
            Assert.IsTrue(parser.ParsePath(out actual));
            Assert.IsNotNull(actual);
            Assert.AreEqual(Selector(Id("foo"), Field("bar")), actual);
        }

        [Test]
        public void ParseMethodCall()
        {
            TestExpr("foo.MyFunc()", Selector(Id("foo"), FollowedCall("MyFunc")));
        }

        [Test]
        public void ParseLongChainOfFieldsAndMethods()
        {
            TestExpr("foo.MyFunc().bar.Length()", Selector(Selector(Selector(Id("foo"), FollowedCall("MyFunc")), Field("bar")), FollowedCall("Length")));
        }

        [Test]
        public void ParseEmptyBlockExpr()
        {
            TestExpr("{}", new RppBlockExpr(Collections.NoNodes));
        }

        [Test]
        public void VarInBlockExpr()
        {
            RppBlockExpr blockExpr = ParserTest.CreateParser(@"{
        val k : String = new String
    }").ParseBlockExpr();

            Assert.IsNotNull(blockExpr);
        }

        [Test]
        public void ParseLogical1()
        {
            TestExpr("x && y", LogicalAnd(Id("x"), Id("y")));
            TestExpr("x || y", LogicalOr(Id("x"), Id("y")));
        }

        [Test]
        public void ParseLogical2()
        {
            TestExpr("x == 3 && y", LogicalAnd(Eq(Id("x"), Int(3)), Id("y")));
            TestExpr("x == 3 || y", LogicalOr(Eq(Id("x"), Int(3)), Id("y")));
        }

        [Test]
        public void ParseLogical3()
        {
            TestExpr("x && y == 3", LogicalAnd(Id("x"), Eq(Id("y"), Int(3))));
            TestExpr("x || y == 3", LogicalOr(Id("x"), Eq(Id("y"), Int(3))));
        }

        [Test]
        public void ParseNot()
        {
            TestExpr("!x", Not(Id("x")));
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

        private static RppUnary Not(IRppExpr expr)
        {
            return new RppUnary("!", expr);
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