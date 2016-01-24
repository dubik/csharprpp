using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static CSharpRppTest.Utils;

namespace CSharpRppTest
{
    [TestClass]
    public class CaseClassTest
    {
        [TestMethod]
        public void TestCompanionObjectGeneration()
        {
            const string code = @"
case class Foo(k : Int)

object Main {
    def main : Foo = Foo(238)
}
";
            Type mainTy = ParseAndCreateType(code, "Main$");
            object res = InvokeStatic(mainTy, "main");
            Assert.IsNotNull(res);
            Assert.AreEqual(238, res.GetPropertyValue("k"));
        }

        [TestMethod, Ignore]
        public void TestUnapply()
        {
            const string code = @"
case class Foo(k: Int)

object Main {
    def main : Option[Int] = {
        val foo = Foo(13)
        Foo.unapply(foo)
    }
}
";
            Type mainTy = ParseAndCreateType(code, "Main$");
            object res = InvokeStatic(mainTy, "main");
            Assert.IsNotNull(res);
        }
    }
}