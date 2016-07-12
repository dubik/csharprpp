using System.Collections.Generic;
using CSharpRpp;
using CSharpRpp.Exceptions;
using CSharpRpp.Parser;
using CSharpRpp.TypeSystem;
using NUnit.Framework;
using static CSharpRppTest.Utils;
using Assert = NUnit.Framework.Assert;

namespace CSharpRppTest
{
    [TestFixture]
    public class SemanticTest
    {
        [Test]
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

        [Test]
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

        [Test]
        public void TestDifferentReturnTypesForSameFunctionName()
        {
            IList<RppFunc> functions = new List<RppFunc> {_intCreateFunc, _unitCreateFunc};
            Assert.Throws<System.Exception>(() => FuncValidator.Validate(functions));
        }

        [Test]
        public void MethodWithSameNameDefinedTwice()
        {
            IList<RppFunc> functions = new List<RppFunc> {_intCreateFunc, _intCreateFunc};
            Assert.Throws<System.Exception>(() => FuncValidator.Validate(functions));
        }

        [Test]
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
            Assert.Throws<SemanticException>(() => ParseAndAnalyze(code));
        }

        [Test]
        public void MethodReturnTypeShouldMatchLastExpressionType()
        {
            const string code = @"
object Main
{
    def main: Int = ""Hello""
}
";
            Assert.Throws<SemanticException>(() => ParseAndAnalyze(code));
        }

        [Test]
        public void ShouldReportErrorWhenSymbolIsNotFound()
        {
            const string code = @"
object Main {
    def main : Int = SomeClass
}
";
            Assert.Throws<SemanticException>(() => ParseAndCreateType(code, "Main$"));
        }

        [Test]
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
            Assert.Throws<SemanticException>(() => ParseAndCreateType(code, "Main$"));
        }

        [Test]
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
            Assert.Throws<SemanticException>(() => ParseAndCreateType(code, "Main$"), "not enough arguments");
        }

        [Test]
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
            Assert.Throws<SemanticException>(() => ParseAndCreateType(code, "Main$"), "not enough arguments");
        }
    }
}