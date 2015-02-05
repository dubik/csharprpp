using System.Linq;
using Antlr.Runtime;
using CSharpRpp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CSharpRppTest
{
    [TestClass]
    public class ParserTest
    {
        private static RppProgram Parse(string code)
        {
            ANTLRStringStream input = new ANTLRStringStream(code);
            RppLexer lexer = new RppLexer(input);
            CommonTokenStream tokenStream = new CommonTokenStream(lexer);
            RppParser parser = new RppParser(tokenStream);
            return parser.CompilationUnit();
        }

        [TestMethod]
        public void EmptyObject()
        {
            const string code = @"package Hello
object Main";
            const string code1 = code + "\n";
            const string code2 = code1 + "\n";

            RppProgram program = Parse(code);
            Assert.IsNotNull(program);
            Assert.AreEqual(1, program.Classes.Count());
        }
    }
}