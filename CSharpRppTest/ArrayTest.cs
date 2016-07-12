using System;
using NUnit.Framework;
using static CSharpRppTest.Utils;

namespace CSharpRppTest
{
    [TestFixture]
    public class ArrayTest
    {
        [Test, Category("Array")]
        public void CreateArray()
        {
            const string code = @"
object Main {
    def main: Array[Int] = new Array[Int](13)
}
";
            var mainTy = ParseAndCreateType(code, "Main$");
            Assert.IsNotNull(mainTy);
            var res = InvokeStatic(mainTy, "main");
            Assert.IsNotNull(res);
        }

        [Test, Category("Array")]
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

        [Test, Category("Array")]
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

        [Test, Category("Array")]
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
    }
}