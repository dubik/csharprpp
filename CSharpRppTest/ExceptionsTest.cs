using System;
using System.Reflection;
using NUnit.Framework;

namespace CSharpRppTest
{
    [TestFixture]
    public class ExceptionsTest
    {
        [Test]
        public void DeclareCustomExceptionClass()
        {
            const string code = @"
class MyException extends Exception
";
            var myExceptionTy = Utils.ParseAndCreateType(code, "MyException");
            Assert.IsNotNull(myExceptionTy);
            var inst = Activator.CreateInstance(myExceptionTy);
            Assert.IsTrue(inst is Exception);
        }


        [Test]
        public void ThrowSystemException()
        {
            const string code = @"
object Foo
{
    def main : Unit = throw new Exception
}
";
            var fooTy = Utils.ParseAndCreateType(code, "Foo$");
            Assert.IsNotNull(fooTy);
            var ex = Assert.Throws<TargetInvocationException>(() => Utils.InvokeStatic(fooTy, "main"));
            Assert.IsInstanceOf<Exception>(ex.InnerException);
        }

        [Test]
        public void ThrowSystemExceptionWithMessage()
        {
            const string code = @"
object Foo
{
    def main : Unit = throw new Exception(""Hello"")
}
";
            var fooTy = Utils.ParseAndCreateType(code, "Foo$");
            Assert.IsNotNull(fooTy);
            var ex = Assert.Throws<TargetInvocationException>(() => Utils.InvokeStatic(fooTy, "main"));
            Assert.IsInstanceOf<Exception>(ex.InnerException);
            Assert.AreEqual("Hello", ex.InnerException.Message);
        }
    }
}