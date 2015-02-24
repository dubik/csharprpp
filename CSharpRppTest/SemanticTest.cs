using CSharpRpp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CSharpRppTest
{
    [TestClass]
    public class SemanticTest
    {
        [TestMethod]
        public void IdShouldGetClassType()
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
    }
}