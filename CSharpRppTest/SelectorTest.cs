﻿using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CSharpRppTest
{
    #pragma warning disable 649
    sealed class Option
    {
        public int K;
    }
    #pragma warning restore 649

    [TestClass]
    public class SelectorTest
    {
        /*
        [TestMethod]
        public void SelectorShouldHaveProperType()
        {
            RppNativeClass kClass = new RppNativeClass(typeof(Option));
            RppObjectType kClassType = new RppObjectType(kClass);
            RppVar optionVar = new RppVar(MutabilityFlag.MfVal, "myOption", kClassType, RppEmptyExpr.Instance);
            RppScope scope = new RppScope(null);
            optionVar.Analyze(scope);
            RppSelector fieldSelector = new RppSelector(new RppId("myOption"), new RppId("K"));
            fieldSelector.Analyze(scope);
            Assert.IsNotNull(fieldSelector.Type, "field selector should have the same type as k");
            Assert.AreEqual(typeof(int), fieldSelector.Type.Runtime);
        }
        */
    }
}
