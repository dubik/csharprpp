using System;
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
    }
}