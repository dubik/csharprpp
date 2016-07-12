using System.Reflection;
using NUnit.Framework;

namespace CSharpRppTest
{
    [TestFixture]
    public class MethodDispatchTest
    {
        [Test]
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
            object res = Utils.InvokeStatic(fooTy, "callWithNoArg");
            Assert.AreEqual(10, res);
            res = Utils.InvokeStatic(fooTy, "callWithOneArg");
            Assert.AreEqual(13, res);
        }

        [Test]
        [Category("Generics")]
        public void DefineGenericFunc()
        {
            const string code = @"
object Foo
{
    def same[T](x : T) : T = x
}
";
            var fooTy = Utils.ParseAndCreateType(code, "Foo$");
            MethodInfo same = fooTy.GetMethod("same", BindingFlags.Instance | BindingFlags.Public);
            MethodInfo intSame = same.MakeGenericMethod(typeof (int));
            object res = intSame.Invoke(Utils.GetObjectInstance(fooTy), new object[] {12});
            Assert.AreEqual(res, 12);
        }
    }
}