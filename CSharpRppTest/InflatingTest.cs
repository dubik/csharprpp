using System;
using System.Linq;
using CSharpRpp;
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

        [TestMethod]
        public void InflatedGenericClassWithSecondGenericArgument()
        {
            RType barTy = new RType("Bar");
            barTy.DefineGenericParameters("A");

            RType fooTy = new RType("Foo");
            RppGenericParameter[] fooGenericParams = fooTy.DefineGenericParameters("X", "Y");
            fooTy.BaseType = barTy.MakeGenericType(fooGenericParams[1].Type);

            RType intFloatFooTy = fooTy.MakeGenericType(IntTy, FloatTy);
            RType baseType = intFloatFooTy.BaseType;

            Assert.AreEqual("Bar[Float]", baseType?.ToString());
        }

        internal class MyBaseClass<T>
        {
        }

        internal class MyClass<B> : MyBaseClass<B>
        {
        }

        [TestMethod]
        public void InflatingNativeBaseType()
        {
            RType myClassTy = new RType("MyClass", typeof(MyClass<>), CreateType);
            Assert.IsNotNull(myClassTy);
            RType myBaseClassTy = myClassTy.BaseType;
            Assert.IsNotNull(myBaseClassTy);
            CollectionAssert.AreEqual(myClassTy.GenericParameters.Select(gp => gp.Type).ToList(), myBaseClassTy.GenericArguments.ToList());
        }

        private static RType CreateType(Type type)
        {
            string typeName = type.Name;

            if (type.IsConstructedGenericType)
            {
                var resType = ConstructSpecializedTypeFromGenericTypeDefinition(typeName, type);
                return resType;
            }

            return new RType(type.Name, type, CreateType);
        }

        /// <summary>
        /// Creates wrapper around native type by inflating wrapper of generic type definition of specified type.
        /// This will initialize properly generic arguments for the returned type. Let say native type looks like this:
        /// <code>
        /// class Foo&lt;B&gt; : Bar&lt;B&gt;
        /// </code>
        /// B is a generic parameter but for Bar that is a generic argument. We can't inherit generic class we have to
        /// specialized it with generic parameter.
        /// </summary>
        /// <param name="typeName">name of the created wrapped type</param>
        /// <param name="type">specialized native type</param>
        /// <returns></returns>
        private static RType ConstructSpecializedTypeFromGenericTypeDefinition(string typeName, Type type)
        {
            Type nativeTypeDefinition = type.GetGenericTypeDefinition();
            RType typeDefinition = new RType(typeName, nativeTypeDefinition, CreateType);
            RType[] genericArguments = type.GenericTypeArguments.Select(typeArg => new RType(typeArg.Name, typeArg, CreateType)).ToArray();
            RType resType = typeDefinition.MakeGenericType(genericArguments);
            return resType;
        }
    }
}