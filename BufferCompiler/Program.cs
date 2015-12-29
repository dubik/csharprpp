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
                if (isEmpty()) {
        QNil
    } else {
      new QCons(f(head()), tail().map(f))
    }
*/
            const string code = @"
abstract class QList[+A] {
  def head: A

  def tail: QList[A]

  def isEmpty: Boolean

  def map[U](f: A => U): QList[U] = {
    tail().map(f)
    QNil
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
      var k = args.length() - 1
      var list: QList[A] = QNil
      while (k >= 0) {
        val it: A = args(k)
        list = new QCons(it, list)
        k = k - 1
      }
      list
    }
  }
}
";

            Diagnostic diagnostic = new Diagnostic();
            CodeGenerator codeGen = RppCompiler.Compile(program => RppCompiler.Parse(code, program), diagnostic, "Sample.dll");
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
