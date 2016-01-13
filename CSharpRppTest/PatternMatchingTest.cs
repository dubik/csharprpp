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
            case  x: Foo => x.length
            case _ => 0
        }
    }
}
";
            Type mainTy = ParseAndCreateType(code, "Main$");
            object res = InvokeStatic(mainTy, "main");
            Assert.AreEqual(3, res);
        }

        [TestMethod, TestCategory("PatternMatching"), Ignore]
        public void ConstructorPatternMatching()
        {
            const string code = @"
class Foo(val length: Int, val value: Int)

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

    }
}