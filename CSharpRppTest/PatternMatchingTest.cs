using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static CSharpRppTest.Utils;

namespace CSharpRppTest
{
    [TestClass]
    public class PatternMatchingTest
    {
        [TestMethod, TestCategory("PatternMatching")]
        public void IntegerPatternMatching()
        {
            const string code = @"
object Main {
    def main : Int = {
        val k = 13
        k match {
            case 13 => 24
            case _ => 27
        }
    }
}
";
            Type mainTy = ParseAndCreateType(code, "Main$");
            object res = InvokeStatic(mainTy, "main");
            Assert.AreEqual(24, res);
        }

        [TestMethod, TestCategory("PatternMatching")]
        public void StringPatternMatching()
        {
            const string code = @"
object Main {
    def main : String = {
        val k = ""Hello""
        k match {
            case ""Hello"" => ""World""
            case _ => ""Moi""
        }
    }
}
";
            Type mainTy = ParseAndCreateType(code, "Main$");
            object res = InvokeStatic(mainTy, "main");
            Assert.AreEqual("World", res);
        }

        [TestMethod, TestCategory("PatternMatching")]
        public void InstanceOfPatternMatching()
        {
            const string code = @"
class Foo(val length: Int)

object Main {
    def main : Int = {
        val k = new Foo(3)
        k match {
            case x: Foo => x.length
            case _ => 0
        }
    }
}
";
            Type mainTy = ParseAndCreateType(code, "Main$");
            object res = InvokeStatic(mainTy, "main");
            Assert.AreEqual(3, res);
        }

        [TestMethod, TestCategory("PatternMatching")]
        public void ConstructorPatternMatching()
        {
            const string code = @"
case class Foo(val length: Int, val value: Int)

object Main {
    def main : Int = {
        val k = new Foo(3, 27)
        k match {
            case Foo(x, y) => x
            case _ => 0
        }
    }
}
";
            Type mainTy = ParseAndCreateType(code, "Main$");
            object res = InvokeStatic(mainTy, "main");
            Assert.AreEqual(3, res);
        }

        [TestMethod, TestCategory("PatternMatching")]
        public void ConstructorPatternMatchingWithOneClassParam()
        {
            const string code = @"
case class Foo(val length: Int)

object Main {
    def main : Int = {
        val k = new Foo(3)
        k match {
            case Foo(x) => x
            case _ => 0
        }
    }
}
";
            Type mainTy = ParseAndCreateType(code, "Main$");
            object res = InvokeStatic(mainTy, "main");
            Assert.AreEqual(3, res);
        }

        [TestMethod, TestCategory("PatternMatching")]
        public void ConstructorPatternMatchingWithoutClassParams()
        {
            const string code = @"
case class Foo

object Main {
    def main : Int = {
        val k = new Foo
        k match {
            case Foo() => 13
            case _ => 0
        }
    }
}
";
            Type mainTy = ParseAndCreateType(code, "Main$");
            object res = InvokeStatic(mainTy, "main");
            Assert.AreEqual(13, res);
        }

        [TestMethod, TestCategory("PatternMatching")]
        public void ConstructorPatternMatchingWithOneClassParamAndBindedToVariable()
        {
            const string code = @"
case class Foo(val length: Int)

object Main {
    def main : Int = {
        val k = new Foo(3)
        k match {
            case y@Foo(x) => y.length
            case _ => 0
        }
    }
}
";
            Type mainTy = ParseAndCreateType(code, "Main$");
            object res = InvokeStatic(mainTy, "main");
            Assert.AreEqual(3, res);
        }

        [TestMethod, TestCategory("PatternMatching")]
        public void ConstructorPatternForMatchingHierarchies()
        {
            const string code = @"
abstract class Expr()
case class Number(val value: Int) extends Expr
case class Str(val value: String) extends Expr

object Main {
    def main1: Expr = matchExpr(Number(31))

    def main2: Expr = matchExpr(Str(""Hello""))

    def matchExpr(e: Expr): Expr = e match {
        case Number(x) => Number(x * 29)
        case Str(x) => Str(""Matched"")
        case _ => e
    }
}
";
            Type mainTy = ParseAndCreateType(code, "Main$");
            object res = InvokeStatic(mainTy, "main1");
            Assert.IsNotNull(res);
            Assert.AreEqual(31 * 29, res.GetPropertyValue("value"));

            res = InvokeStatic(mainTy, "main2");
            Assert.IsNotNull(res);
            Assert.AreEqual("Matched", res.GetPropertyValue("value"));
        }

        [TestMethod, TestCategory("PatternMatching")]
        public void ComplexConstructorPatternMatching()
        {
            const string code = @"
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
            Type mainTy = ParseAndCreateType(code, "Main$");
            object res = InvokeStatic(mainTy, "main");
            Assert.IsNotNull(res);
            Assert.AreEqual(5, res.GetPropertyValue("value"));
        }
    }
}