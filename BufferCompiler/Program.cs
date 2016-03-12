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
object Main {
    def myPrt(str: String) : Unit = {
    }

    def another : Unit = {
        myPrt(""Hello"")
    }

    def main(argv: Array[String]) : Unit = {
        Console.println(""Hello"")
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