using System;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CSharpRppTest
{
    [TestClass]
    public class ExceptionsTest
    {
        [TestMethod]
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


        [TestMethod]
        [ExpectedException(typeof (Exception))]
        public void ThrowSystemException()
        {
            const string code = @"
object Foo
{
    def main : Unit = throw new Exception
}
";
            var fooTy = Utils.ParseAndCreateType(code, "Foo$", typeof(Exception));
            Assert.IsNotNull(fooTy);
            MethodInfo mainMethod = fooTy.GetMethod("main", BindingFlags.Static | BindingFlags.Public);
            Assert.IsNotNull(mainMethod);
            try
            {
                mainMethod.Invoke(null, null);
            }
            catch (Exception e)
            {
                throw e.InnerException;
            }
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void ThrowSystemExceptionWithMessage()
        {
            const string code = @"
object Foo
{
    def main : Unit = throw new Exception(""Hello"")
}
";
            var fooTy = Utils.ParseAndCreateType(code, "Foo$", typeof(Exception));
            Assert.IsNotNull(fooTy);
            MethodInfo mainMethod = fooTy.GetMethod("main", BindingFlags.Static | BindingFlags.Public);
            Assert.IsNotNull(mainMethod);
            try
            {
                mainMethod.Invoke(null, null);
            }
            catch (Exception e)
            {
                Assert.AreEqual("Hello", e.InnerException.Message);
                throw e.InnerException;
            }
        }

    }
}