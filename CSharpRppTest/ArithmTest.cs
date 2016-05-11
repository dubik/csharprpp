using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static CSharpRppTest.Utils;

namespace CSharpRppTest
{
    [TestClass]
    public class ArithmTest
    {
        [TestMethod]
        public void AddEqualInt()
        {
            const string code = @"
object Main {
    def main(k: Int) : Int = {
        var p = 13
        p += k
        p
    }
}
";
            Type mainTy = ParseAndCreateType(code, "Main$");
            object res = InvokeStatic(mainTy, "main", 31);
            Assert.AreEqual(44, res);
        }

        [TestMethod]
        public void ComplexAssignmentOperatorTest()
        {
            const string code = @"
object Main {
    def main(k: Int) : Int = {
        var p = 13
        p += k
        p -= 1
        p *= 2
        p /= 2
        p
    }
}
";
            Type mainTy = ParseAndCreateType(code, "Main$");
            object res = InvokeStatic(mainTy, "main", 31);
            Assert.AreEqual(43, res);
        }

    }
}