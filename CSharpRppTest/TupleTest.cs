﻿using System;
using NUnit.Framework;
using static CSharpRppTest.Utils;

namespace CSharpRppTest
{
    [TestFixture]
    public class TupleTest
    {
        [Test, Category("Tuples")]
        public void ExplicitTupleTest()
        {
            const string code = @"
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

        [Test, Category("Tuples")]
        public void ImplicitTupleTest()
        {
            const string code = @"
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