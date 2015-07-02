using System.Collections;
using System.Collections.Generic;
using CSharpRpp;
using CSharpRpp.Parser;
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
            RppObjectType objectType = id.Type as RppObjectType;
            Assert.IsNotNull(objectType, "Identifier should have been resolved to RppObjectType");
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

        private RppFunc intCreateFunc = new RppFunc("create", RppPrimitiveType.IntTy);
        private RppFunc unitCreateFunc = new RppFunc("create", RppPrimitiveType.UnitTy);

        [TestMethod]
        [ExpectedException(typeof(System.Exception))]
        public void TestDifferentReturnTypesForSameFunctionName()
        {
            IList<RppFunc> functions = new List<RppFunc>() {intCreateFunc, unitCreateFunc};
            FuncValidator.Validate(functions);
        }

        [TestMethod]
        [ExpectedException(typeof(System.Exception))]
        public void MethodWithSameNameDefinedTwice()
        {
            IList<RppFunc> functions = new List<RppFunc>() { intCreateFunc, intCreateFunc };
            FuncValidator.Validate(functions);
        }
    }
}