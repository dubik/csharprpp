using CSharpRpp;
using CSharpRpp.Codegen;
using CSharpRpp.Reporting;

namespace BufferCompiler
{
    public class Program
    {
        public static void Main()
        {
            const string code = @"
abstract class QList[+A] {
  def head: A

  def tail: QList[A]

  def isEmpty: Boolean

  def map[U](f: A => U): QList[U] = {
    if (isEmpty) {
        QNil
    } else {
      new QCons(f(head()), tail().map(f))
    }
  }
}

object QNil extends QList[Nothing] {
  override def head: Nothing = throw new Exception(""Not implemented"")

  override def tail: QList[Nothing] = throw new Exception(""Not implemented"")

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
      QNil
    } else {
      var k = args.length - 1
      var list: QList[A] = QNil
      while (k >= 0) {
        list = new QCons(args(k), list)
        k -= 1
      }
      list
    }
  }
}
";
            const string code1 = @"
object Foo {
    def main : Int = {
        val k = 13
        if(k == 10) {
            3
        } else {
            5
        }
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