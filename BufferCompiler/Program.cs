using System;
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
    if (isEmpty()) {
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

            const string code1 = @"
object Main {
    def invoke() : Int = {
        val p : Int = 10
        var ret : Int = 0
        while(p >= 0 && ret < 5)
        {
            ret = ret + 1
            p = p - 1
        }
        ret
    }

    def allTrue : Boolean = true || true

    def book(a: Int, b: Int, c: Int) : Int = if(a > b && b > c && c > 5) a else b
    def bookSimple(a: Int, b: Int, c: Int) : Boolean = a > b && b > c && c > 5

    def and2(x: Boolean, y: Boolean) : Boolean = x || y
    def and3(x: Boolean, y: Boolean, z: Boolean): Boolean = x || y || z;
}
";
            const string code2 = @"
object Main {
    def and2(x: Boolean, y: Boolean) : Boolean = x && y
    def and3(x: Boolean, y: Boolean, z: Boolean): Boolean = x && y && z;
}
";

            const string code3 = @"
object Main {
    def allTrue : Boolean = true && true
    def condSimple(a: Int, b: Int, c: Int) : Boolean = a > b && b > c && c > 5
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

    class Test
    {
        public static int Calc()
        {
            int k = 0;
            int i = 10;
            while (i >= 1)
            {
                k = k + 2;
                i = i + 1;
            }
            return k;
        }

        public static int C()
        {
            int p = 10;
            int ret = 0;
            while (p <= 100)
            {
                ret = ret + 1;
                p = p + 1;
            }

            return ret;
        }

        public static bool BookSimple(int a, int b, int c)
        {
            return a > b && b > c && c > 5;
        }

        public static int BookSimpleInt(int a, int b, int c)
        {
            if (a > b && b > c && c > 5)
            {
                return a;
            }
            else
            {
                return b;
            }
        }

    }
}