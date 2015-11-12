using System;
using System.Reflection;
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

        [TestMethod]
        public void CallFunctionFromNestedClass()
        {
            const string code = @"
class Bar {
    class Internal {
        def func : Int = 13
    }

    def getValue : Int = {
        val inst = new Internal()
        inst.func()
    }
}
";

            var barTy = Utils.ParseAndCreateType(code, "Bar");
            Assert.IsNotNull(barTy);
            object barInst = Activator.CreateInstance(barTy);
            MethodInfo getValueFunc = barTy.GetMethod("getValue");
            object res = getValueFunc.Invoke(barInst, null);
            Assert.AreEqual(13, res);
        }
    }
}