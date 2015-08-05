using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RppRuntime;

namespace CSharpRppTest
{
    [TestClass]
    public class TypeTest
    {
        [TestMethod]
        public void FigureOutTypeForVarDeclarationWithIntInitExpression()
        {
            const string code = @"
object Foo
{
    def main() : Int = {
        val k = 10
        k
    }
}
";
            var fooTy = Utils.ParseAndCreateType(code, "Foo$");
            var mainMethod = fooTy.GetMethod("main", BindingFlags.Public | BindingFlags.Static);
            var res = mainMethod.Invoke(null, null);
            Assert.AreEqual(10, res);
        }

        [TestMethod]
        public void FigureOutTypeForVarDeclarationWithClosureInitExpression()
        {
            const string code = @"
object Foo
{
    def main() : (Int => Int) = {
        val func = (x: Int) => x + 10
        func
    }
}
";
            var fooTy = Utils.ParseAndCreateType(code, "Foo$", typeof(Function1<,>));
            var mainMethod = fooTy.GetMethod("main", BindingFlags.Public | BindingFlags.Static);
            var res = mainMethod.Invoke(null, null);
            Function1<int, int> func = (Function1<int, int>) res;
            var ret = func.apply(13);
            Assert.AreEqual(23, ret);
        }

        [TestMethod]
        public void FigureOutTypeOfClosureBasedVariableType()
        {
            const string code = @"
object Foo
{
    def main() : (Int => Int) = {
        val func : (Int => Int) = (x) => x + 10
        func
    }
}
";
            var fooTy = Utils.ParseAndCreateType(code, "Foo$", typeof(Function1<,>));
            var mainMethod = fooTy.GetMethod("main", BindingFlags.Public | BindingFlags.Static);
            var res = mainMethod.Invoke(null, null);
        }

        [TestMethod]
        public void PassClosureAsAParam()
        {
            const string code = @"
object Bar
{
    def invoker(func: (Int, Int) => Int) : Int = {
        func(10, 24)
    }

    def main() : Int = {
        invoker((x, y) => x + y)
    }
}
";
            var barTy = Utils.ParseAndCreateType(code, "Bar$", typeof(Function2<,,>));
            MethodInfo mainMethod = barTy.GetMethod("main", BindingFlags.Static | BindingFlags.Public);
            var res = mainMethod.Invoke(null, null);
            Assert.AreEqual(34, res);
        }
    }
}
