﻿using System;
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
            Assert.AreEqual(13, res.GetPropertyValue("_1"));
            Assert.AreEqual("Hello", res.GetPropertyValue("_2"));
        }

        [TestMethod, TestCategory("Tuples")]
        public void ImplicitTupleTest()
        {
            const string code = @"
class Tuple2[+T1, +T2](val _1: T1, val _2: T2)

object Main {
    def main : (Int, String) = (13, ""Hello"")
}
";
            Type mainTy = ParseAndCreateType(code, "Main$");
            object res = InvokeStatic(mainTy, "main");
            Assert.IsNotNull(res);
            Assert.AreEqual(13, res.GetPropertyValue("_1"));
            Assert.AreEqual("Hello", res.GetPropertyValue("_2"));
        }
    }
}