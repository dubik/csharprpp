using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CSharpRppTest
{
    [TestClass]
    public class NestedTest
    {
        [TestMethod]
        public void SimplestNestedClass()
        {
            const string code = @"
class Bar {
    class Internal
}
";

            var barTy = Utils.ParseAndCreateType(code, "Bar");
            Assert.IsNotNull(barTy);
            Type[] nestedTypes = barTy.GetNestedTypes();
            Assert.AreEqual(1, nestedTypes.Length);
            Assert.AreEqual("Internal", nestedTypes[0].Name);
        }
    }
}