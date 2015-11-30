using CommandLine;

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
            RppCompiler.CompileAndSave(options.InputFiles, GetOutputFileName(options));
            return 0;
        }

        private static string GetOutputFileName(RppOptions options)
        {
            return !string.IsNullOrEmpty(options.Out) ? options.Out : "out.exe";
        }

        /*
        private static void Main(string[] args)
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
            const string code1 = @"class Foo

object Foo
{
    def calc: Int = 13
}

object Main
{
    def callObject: Int = Foo.calc()
    def callClass : Foo = new Foo
}";

            RppCompiler.CompileAndSave(code1);
        }
        */
    }
}