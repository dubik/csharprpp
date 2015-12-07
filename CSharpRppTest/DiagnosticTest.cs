using System.Linq;
using CSharpRpp.Exceptions;
using CSharpRpp.Reporting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CSharpRppTest
{
    [TestClass]
    public class DiagnosticTest
    {
        [TestMethod]
        public void NonInitializedLocalVar()
        {
            const string code = @"
object Main
{
    def main: Unit = {
        val k: Int
    }
}
";
            Diagnostic diagnostic = new Diagnostic();
            Utils.ParseAndAnalyze(code, diagnostic);
            Assert.AreEqual(1, diagnostic.Errors.Count());
            Assert.AreEqual(102, diagnostic.Errors.First().Code);
        }

        [TestMethod]
        [ExpectedException(typeof (SemanticException))]
        public void TypeNotFound()
        {
            const string code = @"
object Main
{
    def main: Foo = {
    }
}
";
            Utils.ParseAndAnalyze(code, new Diagnostic());
        }

        [TestMethod]
        [ExpectedException(typeof(SemanticException))]
        public void ValueIsNotAMember()
        {
            const string code = @"
class Item {
}

object Main {
    def main(item: Item): Unit = {
        item.calculate()
    }
}
";
            Utils.ParseAndAnalyze(code, new Diagnostic());
        }
    }
}