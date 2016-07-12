using System.Linq;
using CSharpRpp.Exceptions;
using CSharpRpp.Reporting;
using NUnit.Framework;

namespace CSharpRppTest
{
    [TestFixture]
    public class DiagnosticTest
    {
        [Test]
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

        [Test]
        public void TypeNotFound()
        {
            const string code = @"
object Main
{
    def main: Foo = {
    }
}
";
            Assert.Throws<SemanticException>(() => Utils.ParseAndAnalyze(code, new Diagnostic()));
        }

        [Test]
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
            Assert.Throws<SemanticException>(() => Utils.ParseAndAnalyze(code, new Diagnostic()));
        }
    }
}