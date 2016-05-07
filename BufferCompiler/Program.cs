using System.Diagnostics;
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
            const string code = @"
abstract class XIterator[+A] {
  def hasNext: Boolean

  def next(): A

  def copy(): XIterator[A]

  protected def foreach(f: A => Unit): Unit =
    while (hasNext())
      f(next())

  def toList: XList[A] = {
    var res = XList[A]()
    foreach((item) => {
      res = new XCons(item, res)
    })
    res.reverse()
  }

  def count: Int = {
    var c = 0
    foreach(item => c = c + 1)
    c
  }

  def toArray[B <: A]: Array[B] = {
    val res = new Array[B](count())
    var index = 0
    foreach((item) => {
      res(index) = next()
      index = index + 1
    })
    res
  }
}

abstract class XIterable[+A] {
  def iterator: XIterator[A]
}

class XListIterator[A](var list: XList[A]) extends XIterator[A] {
  override def hasNext: Boolean = !list.isEmpty()

  override def next(): A = {
    if (list.isEmpty)
      throw new Exception

    val item = list.head()
    list = list.tail()
    item
  }

  override def copy(): XIterator[A] = new XListIterator[A](list)
}

class XMapIterator[A, U](val iter: XIterator[A], val f: A => U) extends XIterator[U] {
  override def hasNext(): Boolean = iter.hasNext()

  override def next(): U = f(iter.next())

  override def count(): Int = iter.count()

  override def copy(): XIterator[U] = new XMapIterator(iter.copy(), f)
}

abstract class XList[+A] extends XIterable[A] {
  def head: A
  def tail: XList[A]
  def isEmpty: Boolean
  def asStream: XIterator[A] = iterator()
  override def iterator: XIterator[A] = new XListIterator[A](this)

  def reverse: XList[A] = {
    val iter = iterator()
    val k: A = iter.next()
    var res = XList[A]()

    while (iter.hasNext()) {
      res = new XCons[A](iter.next(), res)
    }
    res
  }

    def map[U](f: A => U): XList[U] = XFunc.map[A, U](iterator, f).toList
}

object XFunc {
  def map[A, U](iter: XIterator[A], f: A => U): XIterator[U] = new XMapIterator(iter, f)
}

object XNil extends XList[Nothing] {
  override def isEmpty: Boolean = true
  override def head: Nothing = throw new Exception
  override def tail: XList[Nothing] = throw new Exception
}

class XCons[A](val _head: A, val _tail: XList[A]) extends XList[A] {
  override def isEmpty: Boolean = false
  override def head: A = _head
  override def tail: XList[A] = _tail
}

object XList {
  def apply[A](args: A*): XList[A] = {
    if (args.length() == 0) {
      XNil
    } else {
      var k = args.length() - 1
      var list: XList[A] = XNil
      while (k >= 0) {
        val it: A = args(k)
        list = new XCons[A](it, list)
        k = k - 1
      }
      list
    }
  }
}

object Main {
    def main: Unit = {
        val nums = XList[Int](1, 2, 3, 4, 5)
        nums.map(x => x * 2)
    }
}
";
            const string code1 = @"
class XList[X]
class XCons[A](val head: A) extends XList[A]

class XConsApp[A] {
  def make(a: A) : XList[A] = new XCons(a)
}";

            Diagnostic diagnostic = new Diagnostic();
            CodeGenerator codeGen = RppCompiler.Compile(program => RppCompiler.Parse(code1, program), diagnostic, GetStdlibAssembly(), "Sample.dll");
            if (diagnostic.HasError())
            {
                diagnostic.Report();
            }
            else
            {
                Debug.Assert(codeGen != null, "codeGen != null");
                codeGen.Save(ApplicationType.Library);
            }
        }

        public static Assembly GetStdlibAssembly()
        {
            var location = Assembly.GetAssembly(typeof(Program)).Location;
            string directory = Path.GetDirectoryName(location);
            return Assembly.LoadFile(directory + @"\RppStdlib.dll");
        }
    }
}