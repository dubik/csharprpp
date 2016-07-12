using System;
using NUnit.Framework;

namespace CSharpRppTest
{
    [TestFixture]
    public class ExpressionTest
    {
        [Test]
        public void TestIf()
        {
            const string code = @"
object Main
{
    def main(k : Int) : Int = if(k > 0) 13 else 23
}
";
            Type mainTy = Utils.ParseAndCreateType(code, "Main$");
            object res = Utils.InvokeStatic(mainTy, "main", 2);
            Assert.AreEqual(13, res);
            res = Utils.InvokeStatic(mainTy, "main", -3);
            Assert.AreEqual(23, res);
            res = Utils.InvokeStatic(mainTy, "main", 0);
            Assert.AreEqual(23, res);
        }

        [Test]
        public void TestIfWithFunctionAsCondition()
        {
            const string code = @"
object Main
{
    def hasNext: Boolean = false
    def main: Int = {
        if(hasNext()) {
            throw new Exception
        }

        13
    }
}
";
            Type mainTy = Utils.ParseAndCreateType(code, "Main$");
            object res = Utils.InvokeStatic(mainTy, "main");
            Assert.AreEqual(13, res);
        }

        [Test]
        public void TestOneLineIf()
        {
            const string code = @"
object Main
{
    def main(flag: Boolean): Int = {
        if(flag)
            throw new Exception(""flag is wrong"")

        13
    }
}
";
            Type mainTy = Utils.ParseAndCreateType(code, "Main$");
            try
            {
                object res = Utils.InvokeStatic(mainTy, "main", true);
                Assert.Fail("Should throw exception");
            }
                // ReSharper disable once EmptyGeneralCatchClause
            catch (Exception exc)
            {
                Assert.AreEqual("flag is wrong", exc.InnerException.Message);
            }
        }

        [Test]
        public void PopSimpleIntIfFuncReturnsUnit()
        {
            const string code = @"
object Main
{
    def main : Unit = 10
}
";
            Type mainTy = Utils.ParseAndCreateType(code, "Main$");
            object res = Utils.InvokeStatic(mainTy, "main");
            Assert.IsNull(res);
        }

        [Test]
        public void PopSimpleIntFromBlockExprIfFuncReturnsUnit()
        {
            const string code = @"
object Main
{
    def main : Unit = {
        10
    }
}
";
            Type mainTy = Utils.ParseAndCreateType(code, "Main$");
            object res = Utils.InvokeStatic(mainTy, "main");
            Assert.IsNull(res);
        }

        [Test]
        public void PopResultOfFuncIfFuncReturnsUnit()
        {
            const string code = @"
object Main
{
    def calc : Int = 13
    def main : Unit = calc()
}
";
            Type mainTy = Utils.ParseAndCreateType(code, "Main$");
            object res = Utils.InvokeStatic(mainTy, "main");
            Assert.IsNull(res);
        }

        [Test]
        public void UnitFuncShouldntPopIfLastExprAlreadyUnit()
        {
            const string code = @"
object Main
{
    def calc : Unit = 13
    def main : Unit = calc()
}";
            Type mainTy = Utils.ParseAndCreateType(code, "Main$");
            object res = Utils.InvokeStatic(mainTy, "main");
            Assert.IsNull(res);
        }

        [Test]
        public void UnitFuncShouldntPopForVarDeclaration()
        {
            const string code = @"
object Main
{
    def main : Unit = {
        val k : Int = 1
    }
}";
            Type mainTy = Utils.ParseAndCreateType(code, "Main$");
            object res = Utils.InvokeStatic(mainTy, "main");
            Assert.IsNull(res);
        }

        [Test]
        public void TestEqualOperator()
        {
            const string code = @"
object Foo {
    def main : Int = {
        val k = 13
        if(k == 10) {
            3
        } else {
            5
        }
    }
}";
            Type mainTy = Utils.ParseAndCreateType(code, "Foo$");
            object res = Utils.InvokeStatic(mainTy, "main");
            Assert.AreEqual(5, res);
        }

        [Test]
        public void ReadIntFromArray()
        {
            const string code = @"
object Main {
    def read(args: Array[Int], index: Int) : Int = {
        args(index)
    }
}
";
            Type mainTy = Utils.ParseAndCreateType(code, "Main$");
            int[] array = {1, 3, 5};
            for (int i = 0; i < array.Length; i++)
            {
                object res = Utils.InvokeStatic(mainTy, "read", array, i);
                Assert.AreEqual(array[i], res);
            }
        }


        [Test]
        public void ReadGenericFromArray()
        {
            const string code = @"
object Main {
    def read[A](args: Array[A], index: Int) : A = {
        args(index)
    }

    def readInt(args: Array[Int], index: Int) : Int =
        read[Int](args, index)

    def readString(args: Array[String], index: Int) : String =
        read[String](args, index)
}
";
            Type mainTy = Utils.ParseAndCreateType(code, "Main$");

            int[] intArray = {1, 3, 5};
            for (int i = 0; i < intArray.Length; i++)
            {
                object res = Utils.InvokeStatic(mainTy, "readInt", intArray, i);
                Assert.AreEqual(intArray[i], res);
            }

            string[] stringArray = {"Hello", "Terve", "Moi"};
            for (int i = 0; i < stringArray.Length; i++)
            {
                object res = Utils.InvokeStatic(mainTy, "readString", stringArray, i);
                Assert.AreEqual(stringArray[i], res);
            }
        }

        [Test]
        public void LogicalAndConstrants()
        {
            const string code = @"
object Main {
    def allTrue : Boolean = true && true
    def trueAndFalse : Boolean = true && false
    def falseAndTrue : Boolean = false && true
}
";
            Type mainTy = Utils.ParseAndCreateType(code, "Main$");
            Assert.IsTrue((bool) Utils.InvokeStatic(mainTy, "allTrue"));
            Assert.IsFalse((bool) Utils.InvokeStatic(mainTy, "trueAndFalse"));
            Assert.IsFalse((bool) Utils.InvokeStatic(mainTy, "falseAndTrue"));
        }

        [Test]
        public void LogicalAndVariablesAndParameters()
        {
            const string code = @"
object Main {
    def and2(x: Boolean, y: Boolean) : Boolean = x && y
    def and3(x: Boolean, y: Boolean, z: Boolean): Boolean = x && y && z;
}
";
            Type mainTy = Utils.ParseAndCreateType(code, "Main$");
            Assert.IsTrue((bool) Utils.InvokeStatic(mainTy, "and2", true, true));
            Assert.IsFalse((bool) Utils.InvokeStatic(mainTy, "and2", true, false));
            Assert.IsFalse((bool) Utils.InvokeStatic(mainTy, "and2", false, true));
            Assert.IsFalse((bool) Utils.InvokeStatic(mainTy, "and3", true, false, true));
            Assert.IsFalse((bool) Utils.InvokeStatic(mainTy, "and3", false, false, true));
        }

        [Test]
        public void LogicalOrVariablesAndParameters()
        {
            const string code = @"
object Main {
    def or2(x: Boolean, y: Boolean) : Boolean = x || y
    def or3(x: Boolean, y: Boolean, z: Boolean): Boolean = x || y || z;
    def ifOr2(x: Boolean, y: Boolean) : Boolean = if(x || y) true else false
    def ifOr3(x: Boolean, y: Boolean, z: Boolean): Boolean = if(x || y || z) true else false;
}
";
            Type mainTy = Utils.ParseAndCreateType(code, "Main$");
            Assert.IsTrue((bool) Utils.InvokeStatic(mainTy, "or2", true, true));
            Assert.IsTrue((bool) Utils.InvokeStatic(mainTy, "or2", true, false));
            Assert.IsTrue((bool) Utils.InvokeStatic(mainTy, "or2", false, true));
            Assert.IsFalse((bool) Utils.InvokeStatic(mainTy, "or2", false, false));
            Assert.IsTrue((bool) Utils.InvokeStatic(mainTy, "or3", true, false, true));
            Assert.IsTrue((bool) Utils.InvokeStatic(mainTy, "or3", false, false, true));
            Assert.IsFalse((bool) Utils.InvokeStatic(mainTy, "or3", false, false, false));

            Assert.IsTrue((bool) Utils.InvokeStatic(mainTy, "ifOr2", true, true));
            Assert.IsTrue((bool) Utils.InvokeStatic(mainTy, "ifOr2", true, false));
            Assert.IsTrue((bool) Utils.InvokeStatic(mainTy, "ifOr2", false, true));
            Assert.IsFalse((bool) Utils.InvokeStatic(mainTy, "ifOr2", false, false));
            Assert.IsTrue((bool) Utils.InvokeStatic(mainTy, "ifOr3", true, false, true));
            Assert.IsTrue((bool) Utils.InvokeStatic(mainTy, "ifOr3", false, false, true));
            Assert.IsFalse((bool) Utils.InvokeStatic(mainTy, "ifOr3", false, false, false));
        }

        [Test]
        public void TestMinAndMax()
        {
            const string code = @"
object Main {
    def max(x: Int, y: Int) : Int = if(x > y) x else y
    def min(x: Int, y: Int) : Int = if(x < y) x else y
}
";
            Type mainTy = Utils.ParseAndCreateType(code, "Main$");
            Assert.AreEqual(13, Utils.InvokeStatic(mainTy, "max", 13, 1));
            Assert.AreEqual(13, Utils.InvokeStatic(mainTy, "max", 13, -1));
            Assert.AreEqual(-1, Utils.InvokeStatic(mainTy, "min", 13, -1));
            Assert.AreEqual(1, Utils.InvokeStatic(mainTy, "min", 13, 1));
        }

        [Test]
        public void ComplexLogicalExpressions()
        {
            const string code = @"
object Main {
    def logic1(x: Int, y: Int) : Boolean = x == 0 && y > 0 
    def logic2(x: Int, y: Int) : Boolean = x < 0 || y > 0

    def logic3(x: Int, y: Int) : Boolean = x != 0 && y > 0 
}
";
            Type mainTy = Utils.ParseAndCreateType(code, "Main$");
            Assert.IsTrue((bool) Utils.InvokeStatic(mainTy, "logic1", 0, 1));
            Assert.IsFalse((bool) Utils.InvokeStatic(mainTy, "logic1", 0, 0));
            Assert.IsFalse((bool) Utils.InvokeStatic(mainTy, "logic1", 0, -10));

            Assert.IsTrue((bool) Utils.InvokeStatic(mainTy, "logic2", -10, 1));
            Assert.IsFalse((bool) Utils.InvokeStatic(mainTy, "logic2", 30, -10));
            Assert.IsFalse((bool) Utils.InvokeStatic(mainTy, "logic2", 0, 0));

            Assert.IsTrue((bool) Utils.InvokeStatic(mainTy, "logic3", 1, 1));
            Assert.IsFalse((bool) Utils.InvokeStatic(mainTy, "logic3", 0, 1));
        }

        [Test]
        public void MoreComplexLogicalExpressions()
        {
            const string code = @"
object Main {
    def condSimple(a: Int, b: Int, c: Int) : Boolean = a > b && b > c && c > 5
}
";
            Type mainTy = Utils.ParseAndCreateType(code, "Main$");
            Assert.IsTrue((bool) Utils.InvokeStatic(mainTy, "condSimple", 8, 7, 6));
            Assert.IsFalse((bool) Utils.InvokeStatic(mainTy, "condSimple", 8, 7, 5));
            Assert.IsFalse((bool) Utils.InvokeStatic(mainTy, "condSimple", 0, 0, 0));
        }

        [Test]
        public void CallFunctionWithoutParametersWithoutParens()
        {
            const string code = @"
object Main {
    def count: Int = 3
    def main : Int = count
}
";
            Type mainTy = Utils.ParseAndCreateType(code, "Main$");
            var res = Utils.InvokeStatic(mainTy, "main");
            Assert.AreEqual(3, res);
        }

        [Test]
        public void InvertBooleanValue1()
        {
            const string code = @"
object Main {
    def invertShort(x: Boolean) : Boolean = !x
    def invertLong(x: Boolean) : Boolean = ! x
}
";
            Type mainTy = Utils.ParseAndCreateType(code, "Main$");
            Assert.IsTrue((bool) Utils.InvokeStatic(mainTy, "invertShort", false));
            Assert.IsFalse((bool) Utils.InvokeStatic(mainTy, "invertShort", true));
            Assert.IsTrue((bool) Utils.InvokeStatic(mainTy, "invertLong", false));
            Assert.IsFalse((bool) Utils.InvokeStatic(mainTy, "invertLong", true));
        }

        [Test]
        public void InvertBooleanValue2()
        {
            const string code = @"
object Main {
    def invertShort(x: Boolean) : Int = if(!x) 13 else 27
    def invertLong(x: Boolean) : Int = if(! x) 39 else 98
}
";
            Type mainTy = Utils.ParseAndCreateType(code, "Main$");
            Assert.AreEqual(13, Utils.InvokeStatic(mainTy, "invertShort", false));
            Assert.AreEqual(27, Utils.InvokeStatic(mainTy, "invertShort", true));
            Assert.AreEqual(39, Utils.InvokeStatic(mainTy, "invertLong", false));
            Assert.AreEqual(98, Utils.InvokeStatic(mainTy, "invertLong", true));
        }

    }
}