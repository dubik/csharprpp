using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CSharpRppTest
{
    [TestClass]
    public class ExpressionTest
    {
        [TestMethod]
        public void TestIf()
        {
            const string code = @"
object Main
{
    def main(k : Int) : Int = if(k > 0) 13 else 23
}
";
            Type mainTy = Utils.ParseAndCreateType(code, "Main$");
            object res = Utils.InvokeStatic(mainTy, "main", new object[] {2});
            Assert.AreEqual(13, res);
            res = Utils.InvokeStatic(mainTy, "main", new object[] {-3});
            Assert.AreEqual(23, res);
            res = Utils.InvokeStatic(mainTy, "main", new object[] {0});
            Assert.AreEqual(23, res);
        }

        [TestMethod]
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

        [TestMethod]
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

        [TestMethod]
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

        [TestMethod]
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

        [TestMethod]
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

        [TestMethod]
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

        [TestMethod]
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
                object res = Utils.InvokeStatic(mainTy, "read", new object[] {array, i});
                Assert.AreEqual(array[i], res);
            }
        }

        [TestMethod]
        public void ReadStringFromArray()
        {
            const string code = @"
object Main {
    def read(args: Array[String], index: Int) : String = {
        args(index)
    }
}
";
            Type mainTy = Utils.ParseAndCreateType(code, "Main$");
            string[] array = {"Hello", "Terve", "Moi"};
            for (int i = 0; i < array.Length; i++)
            {
                object res = Utils.InvokeStatic(mainTy, "read", new object[] {array, i});
                Assert.AreEqual(array[i], res);
            }
        }

        [TestMethod]
        public void WriteIntToArray()
        {
            const string code = @"
object Main {
    def write(args: Array[Int], index: Int, value: Int) : Unit = {
        args(index) = value
    }
}
";
            Type mainTy = Utils.ParseAndCreateType(code, "Main$");
            int[] array = {1, 3, 5};
            for (int i = 0; i < array.Length; i++)
            {
                Utils.InvokeStatic(mainTy, "write", new object[] {array, i, i + 23});
            }

            CollectionAssert.AreEqual(new[] {23, 24, 25}, array);
        }

        [TestMethod]
        public void WriteStringToArray()
        {
            const string code = @"
object Main {
    def write(args: Array[String], index: Int, value: String) : Unit = {
        args(index) = value
    }
}
";
            Type mainTy = Utils.ParseAndCreateType(code, "Main$");
            string[] array = {"hello", "moika", "terve"};
            for (int i = 0; i < array.Length; i++)
            {
                Utils.InvokeStatic(mainTy, "write", new object[] {array, i, (i + 23).ToString()});
            }

            CollectionAssert.AreEqual(new[] {"23", "24", "25"}, array);
        }


        [TestMethod]
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
                object res = Utils.InvokeStatic(mainTy, "readInt", new object[] {intArray, i});
                Assert.AreEqual(intArray[i], res);
            }

            string[] stringArray = {"Hello", "Terve", "Moi"};
            for (int i = 0; i < stringArray.Length; i++)
            {
                object res = Utils.InvokeStatic(mainTy, "readString", new object[] {stringArray, i});
                Assert.AreEqual(stringArray[i], res);
            }
        }

        [TestMethod]
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

        [TestMethod]
        public void LogicalAndVariablesAndParameters()
        {
            const string code = @"
object Main {
    def and2(x: Boolean, y: Boolean) : Boolean = x && y
    def and3(x: Boolean, y: Boolean, z: Boolean): Boolean = x && y && z;
}
";
            Type mainTy = Utils.ParseAndCreateType(code, "Main$");
            Assert.IsTrue((bool) Utils.InvokeStatic(mainTy, "and2", new object[] {true, true}));
            Assert.IsFalse((bool) Utils.InvokeStatic(mainTy, "and2", new object[] {true, false}));
            Assert.IsFalse((bool) Utils.InvokeStatic(mainTy, "and2", new object[] {false, true}));
            Assert.IsFalse((bool) Utils.InvokeStatic(mainTy, "and3", new object[] {true, false, true}));
            Assert.IsFalse((bool) Utils.InvokeStatic(mainTy, "and3", new object[] {false, false, true}));
        }

        [TestMethod]
        public void TestMinAndMax()
        {
            const string code = @"
object Main {
    def max(x: Int, y: Int) : Int = if(x > y) x else y
    def min(x: Int, y: Int) : Int = if(x < y) x else y
}
";
            Type mainTy = Utils.ParseAndCreateType(code, "Main$");
            Assert.AreEqual(13, Utils.InvokeStatic(mainTy, "max", new object[] {13, 1}));
            Assert.AreEqual(13, Utils.InvokeStatic(mainTy, "max", new object[] {13, -1}));
            Assert.AreEqual(-1, Utils.InvokeStatic(mainTy, "min", new object[] {13, -1}));
            Assert.AreEqual(1, Utils.InvokeStatic(mainTy, "min", new object[] {13, 1}));
        }

        [TestMethod]
        public void ComplexLogicalExpressions()
        {
            const string code = @"
object Main {
    def logic1(x: Int, y: Int) : Boolean = x == 0 && y > 0 
    def logic2(x: Int, y: Int) : Boolean = x < 0 || y > 0
}
";
            Type mainTy = Utils.ParseAndCreateType(code, "Main$");
            Assert.IsTrue((bool) Utils.InvokeStatic(mainTy, "logic1", new object[] {0, 1}));
            Assert.IsFalse((bool) Utils.InvokeStatic(mainTy, "logic1", new object[] {0, 0}));
            Assert.IsFalse((bool) Utils.InvokeStatic(mainTy, "logic1", new object[] {0, -10}));

            Assert.IsTrue((bool) Utils.InvokeStatic(mainTy, "logic2", new object[] {-10, 1}));
            Assert.IsFalse((bool) Utils.InvokeStatic(mainTy, "logic2", new object[] {30, -10}));
            Assert.IsFalse((bool) Utils.InvokeStatic(mainTy, "logic2", new object[] {0, 0}));
        }

        [TestMethod]
        public void MoreComplexLogicalExpressions()
        {
            const string code = @"
object Main {
    def condSimple(a: Int, b: Int, c: Int) : Boolean = a > b && b > c && c > 5
}
";
            Type mainTy = Utils.ParseAndCreateType(code, "Main$");
            Assert.IsTrue((bool) Utils.InvokeStatic(mainTy, "condSimple", new object[] {8, 7, 6}));
            Assert.IsFalse((bool) Utils.InvokeStatic(mainTy, "condSimple", new object[] {8, 7, 5}));
            Assert.IsFalse((bool) Utils.InvokeStatic(mainTy, "condSimple", new object[] {0, 0, 0}));
        }
    }
}