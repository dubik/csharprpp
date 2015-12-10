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
class Item

class Box[A](val v: A) {
  def map[B](f: A => B): B = f(v)
}

class Package(val v: Item)

object Main
{
    def main() : Int = {
        val item = new Item
        val box = new Box[Item](item)
        box.map[Package](x => new Package(x.v))
        13
    }
}

";
*/
            const string code1 = @"
class Foo
{
    def calculate[A](x: A => A, y: A) : A = x(y)
}

object Main
{
    def calculate[A](x: A) : A = x
    def main() : Unit = {
        val f = new Foo
        f.calculate[Int](x => x, 13)
    }
}
";

            /**
            def func[A](x: A): Boolean

            func(13)

            [[Generic], (Arguments), (Return)]

            Constraints:
            [[Undefined], (x: Int), (Undefined)]
            [[A], (x: A), (Boolean)]

            FirstPass:
            [[Undefined], (x: Int), (Boolean)]
            [[Int], (x: Int), (Boolean)]

            SecondPass:
            [[Int], (x: Int), (Boolean)]
            [[Int], (x: Int), (Boolean)]

            func[Int](13)

            -------------------

            def func[A, B](x: A => B, y : A): B
            int k = func(x => 13, 24)

            [[Generic], (Arguments), (Return)]
            [[A, B], (A => B, A), (B)]
            [[Undefined1, Undefined2], (Undefined1 => Int, Int), (Int)]

            FirstPass:


    */

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