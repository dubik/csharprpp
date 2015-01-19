using System;
using Antlr.Runtime;
using Antlr.Runtime.Tree;

[assembly: CLSCompliant(true)]
namespace CSharpRpp
{
    public class Program
    {
        private static int Main(string[] args)
        {
            const string code = @"
class Array(k: Int)
{
   def apply(index: Int, value: Int) : Unit = 10
}";
            ANTLRStringStream input = new ANTLRStringStream(code);
            JRppLexer lexer = new JRppLexer(input);
            CommonTokenStream tokens = new CommonTokenStream(lexer);
            JRppParser parser = new JRppParser(tokens);
            var result = parser.compilationUnit();
            CommonTreeNodeStream treeNodeStream = new CommonTreeNodeStream(result.Tree);
            JRppTreeGrammar walker = new JRppTreeGrammar(treeNodeStream);
            RppProgram program = walker.walk();
            program.Name = "Sample";
            RppScope scope = new RppScope(null);
            CodegenContext codegenContext = new CodegenContext();
            program.PreAnalyze(scope);
            program.CodegenType(scope);
            program.CodegenMethodStubs(scope, codegenContext);
            program.Analyze(scope);
            program.Codegen(codegenContext);
            program.Save();
            return 0;
        }
    }
}