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
            string[] array = { "hello", "moika", "terve" };
            for (int i = 0; i < array.Length; i++)
            {
                Utils.InvokeStatic(mainTy, "write", new object[] { array, i, (i + 23).ToString()});
            }

            CollectionAssert.AreEqual(new[] { "23", "24", "25" }, array);
        }

    }
}