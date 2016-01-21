using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static CSharpRppTest.Utils;

namespace CSharpRppTest
{
    [TestClass]
    public class TupleTest
    {
        [TestMethod, TestCategory("Tuples")]
        public void ExplicitTupleTest()
        {
            const string code = @"
class Tuple2[+T1, +T2](val _1: T1, val _2: T2)

object Main {
    def main : Tuple2[Int, String] = new Tuple2[Int, String](13, ""Hello"")
}
";
            Type mainTy = ParseAndCreateType(code, "Main$");
            object res = InvokeStatic(mainTy, "main");
            Assert.IsNotNull(res);
        }
    }
}
