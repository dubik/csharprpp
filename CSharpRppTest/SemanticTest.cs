using System;
using System.Collections.Generic;
using CSharpRpp;
using CSharpRpp.Exceptions;
using CSharpRpp.Parser;
using CSharpRpp.TypeSystem;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
            RppProgram program = Utils.ParseAndAnalyze(code);
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

            RppProgram program = Utils.ParseAndAnalyze(code);
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
            Utils.ParseAndAnalyze(code);
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
            Utils.ParseAndAnalyze(code);
        }

        [TestMethod]
        [ExpectedException(typeof(SemanticException))]
        public void ShouldReportErrorWhenSymbolIsNotFound()
        {
            const string code = @"
object Main {
    def main : Int = SomeClass
}
";
            Utils.ParseAndCreateType(code, "Main$");
        }

        [TestMethod]
        [ExpectedException(typeof(SemanticException))]
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
            Utils.ParseAndCreateType(code, "Main$");
        }

    }
}