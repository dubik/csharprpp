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
            const string code = @"
class Array(k: Int)
{
   def apply(index: Int, value: Int) : Unit = 10 + 3

   def length: Int = 10
}

class String(len: Int)
{
}

object Main
{
    def main(args: Array[String]) : Int = 10
}
";
            ANTLRStringStream input = new ANTLRStringStream(code);
            JRppLexer lexer = new JRppLexer(input);
            CommonTokenStream tokens = new CommonTokenStream(lexer);
            JRppParser parser = new JRppParser(tokens);
            var result = parser.compilationUnit();
            var s = ((CommonTree) result.Tree).ToStringTree();
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

            /*
             * Array[String]
             * Array[Array[String]]
             * Pair[String, Int]
             * Pair[String, Array[String]]
            RppProgram  p = new RppProgram();
            RppClass c = new RppClass("Array");
            RppFunc f = new RppFunc();
            c.AddFunc();
            p.Add();
             */
        }
    }
}