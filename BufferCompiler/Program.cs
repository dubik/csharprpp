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

            const string code1 = @"
class Node[A](val item: A)

class List[A](k: A) extends Node[A](k) {
}

object Main {
    def main: List[Int] = new List(13)
}
";

            const string code2 = @"
class Properties[A](var item: A) {

}

object Main {
  def main: Properties[Int] = {
    val p = new Properties(13)
    p.item = 27
    p
  }
}
";
            Diagnostic diagnostic = new Diagnostic();
            CodeGenerator codeGen = RppCompiler.Compile(program => RppCompiler.Parse(code2, program), diagnostic, "Sample.dll");
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

class Properties
{
    public int Count { get; set; }
    public string Name { get; set; }

    private int _length;

    public int Length
    {
        get { return _length; }
        set { _length = value; }
    }

    public int Combine()
    {
        return Count + Length;
    }
}