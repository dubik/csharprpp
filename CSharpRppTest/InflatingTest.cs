using System;
using CSharpRpp.TypeSystem;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static CSharpRpp.TypeSystem.RppTypeSystem;

namespace CSharpRppTest
{
    [TestClass]
    public class InflatingTest
    {
        private class Foo<A>
        {
        }

        [TestMethod]
        public void InflateSimpleClass()
        {
            RType fooTy = new RType("Foo");
            fooTy.DefineGenericParameters("A");

            RType intFooTy = fooTy.MakeGenericType(IntTy);
            RType stringFooTy = fooTy.MakeGenericType(StringTy);
            Assert.AreEqual("Foo[Int]", intFooTy.ToString());
            Assert.AreEqual("Foo[String]", stringFooTy.ToString());

            Type pFooTy = typeof(Foo<>);
            CheckGenericProperties(pFooTy, fooTy);
            CheckGenericProperties(pFooTy.MakeGenericType(typeof(int)), intFooTy);
            CheckGenericProperties(pFooTy.MakeGenericType(typeof(string)), stringFooTy);
        }

        private void CheckGenericProperties(Type expectedGenericProperties, RType actualGenericProperties)
        {
            Assert.AreEqual(expectedGenericProperties.IsGenericType, actualGenericProperties.IsGenericType);
            Assert.AreEqual(expectedGenericProperties.IsConstructedGenericType, actualGenericProperties.IsConstructedGenericType);
            Assert.AreEqual(expectedGenericProperties.IsGenericTypeDefinition, actualGenericProperties.IsGenericTypeDefinition);
            Assert.AreEqual(expectedGenericProperties.IsGenericParameter, actualGenericProperties.IsGenericParameter);
            Assert.AreEqual(expectedGenericProperties.ContainsGenericParameters, actualGenericProperties.ContainsGenericParameters);
        }

        private class Foo<A, B>
        {
        }

        [TestMethod]
        public void InflateTwoGenericParameters()
        {
            RType fooTy = new RType("Foo");
            fooTy.DefineGenericParameters("A", "B");

            RType intStrFooTy = fooTy.MakeGenericType(IntTy, StringTy);
            RType floatstringFooTy = fooTy.MakeGenericType(FloatTy, StringTy);
            Assert.AreEqual("Foo[Int, String]", intStrFooTy.ToString());
            Assert.AreEqual("Foo[Float, String]", floatstringFooTy.ToString());

            Type pFooTy = typeof(Foo<,>);
            CheckGenericProperties(pFooTy, fooTy);
            CheckGenericProperties(pFooTy.MakeGenericType(typeof(int), typeof(string)), intStrFooTy);
            CheckGenericProperties(pFooTy.MakeGenericType(typeof(float), typeof(string)), floatstringFooTy);
        }

        [TestMethod]
        public void InflateWithGenericAttribute()
        {
            RType barTy = new RType("Bar");
            RppGenericParameter xParam = barTy.DefineGenericParameters("X")[0];

            RType fooTy = new RType("Foo");
            fooTy.DefineGenericParameters("A");

            RType xFooTy = fooTy.MakeGenericType(xParam.Type);

            Assert.AreEqual("Foo[!X]", xFooTy.ToString());

            Assert.IsTrue(xFooTy.IsConstructedGenericType);
            Assert.IsTrue(xFooTy.ContainsGenericParameters);
        }

        [TestMethod]
        public void MakeGenericTypesCachesInflatedType()
        {
            RType fooTy = new RType("Foo");
            fooTy.DefineGenericParameters("A");

            var intFooTy1 = fooTy.MakeGenericType(IntTy);
            var intFooTy2 = fooTy.MakeGenericType(IntTy);
            Assert.AreSame(intFooTy1, intFooTy2, "All same types should have the same reference because type comparision relies on that");
            var stringFooTy = fooTy.MakeGenericType(StringTy);
            Assert.AreNotSame(intFooTy1, stringFooTy);
        }

        [TestMethod]
        public void InheritSpecializedType()
        {
            // class Bar[A]
            // class Foo[T] extends Bar[Int]
            // Foo[String]....
            RType barTy = new RType("Bar");
            barTy.DefineGenericParameters("A");
            RType intBarTy = barTy.MakeGenericType(IntTy);

            RType fooTy = new RType("Foo", RTypeAttributes.Class, intBarTy);
            fooTy.DefineGenericParameters("T");

            RType stringFooTy = fooTy.MakeGenericType(StringTy);

            string stringFooTyStr = stringFooTy.ToString();
            Assert.AreEqual("Foo[String]", stringFooTyStr);

            string baseTypeStr = stringFooTy.BaseType?.ToString();
            Assert.AreEqual("Bar[Int]", baseTypeStr);
        }
    }
}