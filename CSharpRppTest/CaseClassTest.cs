﻿using System;
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

        [TestMethod]
        public void TestNoFieldUnapply()
        {
            const string code = @"
case class Foo

object Main {
    def main : Boolean = {
        val foo = Foo()
        Foo.unapply(foo)
    }

    def main1: Boolean = Foo.unapply(null)
}
";
            Type mainTy = ParseAndCreateType(code, "Main$");
            object res = InvokeStatic(mainTy, "main");
            Assert.IsNotNull(res);
            Assert.AreEqual(true, res);

            res = InvokeStatic(mainTy, "main1");
            Assert.AreEqual(false, res);
        }

        [TestMethod]
        public void TestSingleFieldUnapply()
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

        [TestMethod]
        public void TestMultipleFieldsUnapply()
        {
            const string code = @"
case class Foo(k: Int, s: String)

object Main {
    def main : Option[(Int, String)] = {
        val foo = Foo(13, ""Hello"")
        Foo.unapply(foo)
    }
}
";
            Type mainTy = ParseAndCreateType(code, "Main$");
            object res = InvokeStatic(mainTy, "main");
            Assert.IsNotNull(res);
            object tuple = res.GetPropertyValue("x");
            Assert.IsNotNull(tuple);
            Assert.AreEqual(13, tuple.GetPropertyValue("_1"));
            Assert.AreEqual("Hello", tuple.GetPropertyValue("_2"));
        }
    }
}