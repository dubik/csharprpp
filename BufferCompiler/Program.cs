using CSharpRpp;
using CSharpRpp.Codegen;
using CSharpRpp.Reporting;

namespace BufferCompiler
{
    public class Program
    {
        public static void Main()
        {
            /*
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
*/
            /*
                        const string code1 = @"
            abstract class Option[A]
            {
                def isEmpty : Boolean
                def get: A
                def map[B](f: (A) => B): Option[B] = if(isEmpty()) None else new Some(f(get()))
                def flatMap[B](f: (A) => Option[B]): Option[B] = if(isEmpty()) None else f(get())
            }

            class Some[A](val x: A) extends Option[A]
            {
                override def isEmpty : Boolean = false
                override def get : A = x
            }

            object None extends Option[Nothing]
            {
                override def isEmpty : Boolean = true
                override def get : Nothing = throw new Exception(""Nothing to get"")
            }

            object Main
            {
                def main : Unit = {
                    val k = new Some(123)
                }
            }
            ";
            */
            const string code1 = @"
class Option[A](val item: A)

object Main {
    def main: Option[Int] = new Option(132)
}
";
            Diagnostic diagnostic = new Diagnostic();
            CodeGenerator codeGen = RppCompiler.Compile(program => RppCompiler.Parse(code1, program), diagnostic, "Sample.dll");
            if (codeGen == null)
            {
                diagnostic.Report();
            }
            else
            {
                codeGen.Save("Sample.dll");
            }
        }
    }
}