using System.Linq;
using CommandLine;
using CSharpRpp.Codegen;
using CSharpRpp.Reporting;

namespace CSharpRpp
{
    public class Program
    {
        public static int Main(string[] args)
        {
            var result = CommandLine.Parser.Default.ParseArguments<RppOptions>(args);
            return result.MapResult(RunAndReturnExitCode, _ => 1);
        }

        private static int RunAndReturnExitCode(RppOptions options)
        {
            Diagnostic diagnostic = new Diagnostic();
            RppCompiler.CompileAndSave(options, diagnostic);

            diagnostic.Report();

            if (diagnostic.Errors.Any())
            {
                return 1;
            }

            return 0;
        }


        public static void CompileBuffer()
        {
            const string code = @"
abstract class List[A] {
    def head : A
    def tail : List[A]
    def isEmpty : Boolean
}

object Nil extends List[Nothing]
{
    override def head: Nothing = throw new Exception(""Empty list"")
    override def tail: List[Nothing] = throw new Exception(""Empty list"")
    override def isEmpty : Boolean = true
}

class Cons[A](val _head: A, val _tail: List[A]) extends List[A]
{
    override def head: A = _head
    override def tail: List[A] = _tail
    override def isEmpty : Boolean = true
}

object Cons
{
    def doSome[A](x: A) : A = x
    def apply[A](head: A, tail: List[A]) : List[A] = new Cons[A](head, tail)
}

object Main
{
    def main() : Int = {
        Cons.doSome[Int](13)
    }
}
";
            const string code1 = @"
object Main
{
    def main() : Unit = {
        val k: Int
    }
}";
            Diagnostic diagnostic = new Diagnostic();
            CodeGenerator codeGen = RppCompiler.Compile(program => RppCompiler.Parse(code1, program), diagnostic, "Sample.dll");
            codeGen.Save("Sample.dll");
        }
    }
}