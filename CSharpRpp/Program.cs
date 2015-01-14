using System;
using Antlr.Runtime;
using Antlr.Runtime.Tree;

[assembly: CLSCompliant(true)]
namespace CSharpRpp
{
    public class Program
    {
        private static void Main(string[] args)
        {
            string code = @"
class Array(k: Int)
{
   def length = 10
}";
            ANTLRStringStream input = new ANTLRStringStream(code);
            JRppLexer lexer = new JRppLexer(input);
            CommonTokenStream tokens = new CommonTokenStream(lexer);
            JRppParser parser = new JRppParser(tokens);
            var result = parser.compilationUnit();
            CommonTreeNodeStream treeNodeStream = new CommonTreeNodeStream(result.Tree);
            JRppTreeGrammar walker = new JRppTreeGrammar(treeNodeStream);
            RppProgram program = walker.walk();
        }
    }
}