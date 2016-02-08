using System.IO;
using System.Reflection;
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
abstract class QList[+A] {
  def head: A

  def tail: QList[A]

  def isEmpty: Boolean

  def map[U](f: A => U): QList[U] = {
    if (isEmpty()) {
        new QNil[U]()
    } else {
      new QCons(f(head()), tail().map(f))
    }
  }
}

class QNil[A] extends QList[A] {
  override def head: A = throw new Exception(""Not implemented"")

  override def tail: QList[A] = throw new Exception(""Not implemented"")

  override def isEmpty: Boolean = true
}

class QCons[A](val _head: A, val _tail: QList[A]) extends QList[A] {
  override def head: A = _head

  override def tail: QList[A] = _tail

  override def isEmpty: Boolean = false
}

object QList {
  def apply[A](args: A*): QList[A] = {
    if (args.length() == 0) {
      new QNil[A]()
    } else {
      var k = args.length() - 1
      var list: QList[A] = new QNil[A]()
      while (k >= 0) {
        val it: A = args(k)
        list = new QCons[A](it, list)
        k = k - 1
      }
      list
    }
  }
}
";
*/
            const string code1 = @"
abstract class Expr()
case class Mult(val left: Expr, val right: Expr) extends Expr
case class Number(val value: Int) extends Expr

object Main {
    def main : Expr = simplify(Mult(Number(1), Number(5)))

    def simplify(e: Expr): Expr = e match {
        case Mult(Number(0), right) => Number(0)
        case Mult(left, Number(0)) => Number(0)
        case Mult(Number(1), right) => simplify(right)
        case Mult(left, Number(1)) => simplify(left)
        case Mult(left, right) => Mult(simplify(left), simplify(right))
        case _ => e
    }
}
";
            Diagnostic diagnostic = new Diagnostic();

            CodeGenerator codeGen = RppCompiler.Compile(program => RppCompiler.Parse(code1, program), diagnostic, GetStdlibAssembly(), "Sample.dll");
            if (codeGen == null)
            {
                diagnostic.Report();
            }
            else
            {
                codeGen.Save("Sample.dll");
            }
        }

        public static Assembly GetStdlibAssembly()
        {
            var location = Assembly.GetAssembly(typeof (Program)).Location;
            string directory = Path.GetDirectoryName(location);
            return Assembly.LoadFile(directory + @"\RppStdlib.dll");
        }
    }
}