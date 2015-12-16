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
class Tuple1[A](val item1: A)
class Tuple2[A,B](val item1: A, val item2: B)

object Tuple {
  def create[T1](arg1: T1) : Tuple1[T1] = new Tuple1[T1](arg1)
  def create[T1, T2](arg1: T1, arg2: T2) : Tuple2[T1, T2] = new Tuple2[T1, T2](arg1, arg2)
}

object Main {
    def main: Tuple1[Int] = {
        13
    }
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