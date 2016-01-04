using System.Collections.Generic;
using CSharpRpp;
using CSharpRpp.Exceptions;
using CSharpRpp.Parser;
using CSharpRpp.TypeSystem;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static CSharpRppTest.Utils;

namespace CSharpRppTest
{
    [TestClass]
    public class SemanticTest
    {
        [TestMethod]
        public void IdShouldResolveToClassType()
        {
            const string code = @"
class Foo
{
}

object Bar
{
    def calculate : Foo = {
        val k : Foo = new Foo
        k
    }
}
";
            RppProgram program = ParseAndAnalyze(code);
            RppId id = program.First<RppId>("k");
            RType objectType = id.Type.Value;
            Assert.IsNotNull(objectType, "Identifier should have been resolved to RType");
        }

        [TestMethod]
        public void ObjectAndClassWithTheSameNameShouldBeAllowed()
        {
            const string code = @"
class Bar
{
}

object Bar
{
}
";

            RppProgram program = ParseAndAnalyze(code);
            Assert.IsNotNull(program);
        }

        private readonly RppFunc _intCreateFunc = new RppFunc("create", ResolvableType.IntTy);
        private readonly RppFunc _unitCreateFunc = new RppFunc("create", ResolvableType.UnitTy);

        [TestMethod]
        [ExpectedException(typeof (System.Exception))]
        public void TestDifferentReturnTypesForSameFunctionName()
        {
            IList<RppFunc> functions = new List<RppFunc> {_intCreateFunc, _unitCreateFunc};
            FuncValidator.Validate(functions);
        }

        [TestMethod]
        [ExpectedException(typeof (System.Exception))]
        public void MethodWithSameNameDefinedTwice()
        {
            IList<RppFunc> functions = new List<RppFunc> {_intCreateFunc, _intCreateFunc};
            FuncValidator.Validate(functions);
        }

        [TestMethod]
        [ExpectedException(typeof (SemanticException))]
        public void TypeDonotMatch()
        {
            const string code = @"
class Bar
class Foo

object Main
{
    def main() : Unit = {
        var foo : Foo = new Foo()
        var bar: Bar = new Bar()
        foo = bar
    }
}
";
            ParseAndAnalyze(code);
        }

        [TestMethod]
        [ExpectedException(typeof (SemanticException))]
        public void MethodReturnTypeShouldMatchLastExpressionType()
        {
            const string code = @"
object Main
{
    def main: Int = ""Hello""
}
";
            ParseAndAnalyze(code);
        }

        [TestMethod]
        [ExpectedException(typeof (SemanticException))]
        public void ShouldReportErrorWhenSymbolIsNotFound()
        {
            const string code = @"
object Main {
    def main : Int = SomeClass
}
";
            ParseAndCreateType(code, "Main$");
        }

        [TestMethod]
        [ExpectedException(typeof (SemanticException))]
        public void ShouldReportErrorWhenClassIsUsed()
        {
            const string code = @"
class QNil

object Main{
    def main: Unit = {
        var l = QNil
    }
}
";
            ParseAndCreateType(code, "Main$");
        }

        [TestMethod]
        public void NotEnoughArgumentsForNew()
        {
            const string code = @"
class Node[A](val item: A)

object Main{
    def main: Unit = {
        var n: Node[Int] = new Node
    }
}
";
            AssertRaisesException<SemanticException>(() => ParseAndCreateType(code, "Main$"), "not enough arguments");
        }

        [TestMethod]
        public void NotEnoughArgumentsForFunction()
        {
            const string code = @"
object Main{
    def func(k: Int) : Int = k

    def main: Unit = {
        func()
    }
}
";
            AssertRaisesException<SemanticException>(() => ParseAndCreateType(code, "Main$"), "not enough arguments");
        }

    }
}