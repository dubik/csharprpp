using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CSharpRppTest
{
    [TestClass]
    public class MethodDispatchTest
    {
        [TestMethod]
        public void CallOverloadWithOneArg()
        {
            const string code = @"
object Foo
{
    def calculate() : Int = 10
    def calculate(k : Int) : Int = 13

    def callWithNoArg() : Int = calculate()

    def callWithOneArg() : Int = calculate(13)
}
";
            var fooTy = Utils.ParseAndCreateType(code, "Foo$");
            MethodInfo func = fooTy.GetMethod("callWithNoArg", BindingFlags.Static | BindingFlags.Public);
            object res = func.Invoke(null, null);
            Assert.AreEqual(10 , res);

            func = fooTy.GetMethod("callWithOneArg", BindingFlags.Static | BindingFlags.Public);
            res = func.Invoke(null, null);
            Assert.AreEqual(13, res);
        }
    }
}