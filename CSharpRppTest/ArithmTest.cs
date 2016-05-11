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

        [TestMethod]
        public void BitwiseAnd()
        {
            const string code = @"
object Main {
    def main(k: Int) : Int = {
        var mask: Int = 7
        k & mask
    }
}
";
            Type mainTy = ParseAndCreateType(code, "Main$");
            object res = InvokeStatic(mainTy, "main", 14);
            Assert.AreEqual(14 & 7, res);
        }

        [TestMethod]
        public void BitwiseOr()
        {
            const string code = @"
object Main {
    def main(k: Int) : Int = {
        var p: Int = 7
        k | p
    }
}
";
            Type mainTy = ParseAndCreateType(code, "Main$");
            object res = InvokeStatic(mainTy, "main", 14);
            Assert.AreEqual(14 | 7, res);
        }

        [TestMethod]
        public void ComplexBitwiseOperators()
        {
            const string code = @"
object Main {
    def main(k: Int) : Int = {
        var p: Int = 7
        var l = 0
        l |= p
        l &= k
        l ^= 1
        l
    }
}
";
            Type mainTy = ParseAndCreateType(code, "Main$");
            object res = InvokeStatic(mainTy, "main", 14);

            const int p = 7;
            int l = 0;
            l |= p;
            l &= 14;
            l ^= 1;
            Assert.AreEqual(l, res);
        }
    }
}