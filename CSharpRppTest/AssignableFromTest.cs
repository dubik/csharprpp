using System.Runtime.Remoting;
using CSharpRpp.TypeSystem;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static CSharpRpp.TypeSystem.RppTypeSystem;

namespace CSharpRppTest
{
    [TestClass]
    public class AssignableFromTest
    {
        [TestMethod]
        public void TestAssignablableFromNull()
        {
            RType type = new RType("Foo");
            Assert.IsFalse(type.IsAssignable(null));
        }

        [TestMethod]
        public void TestAssignableFromSameType()
        {
            RType type = new RType("Foo");
            Assert.IsTrue(type.IsAssignable(type));
        }

        [TestMethod]
        public void TestAssignableFromSameType2()
        {
            RType type1 = new RType("Foo");
            RType type2 = new RType("Foo");
            Assert.IsFalse(type1.IsAssignable(type2)); // Same types should point to the same type object
            Assert.IsTrue(type1.IsAssignable(type1));
        }

        [TestMethod]
        public void TestAssignableForPrimitiveTypes()
        {
            Assert.IsFalse(IntTy.IsAssignable(FloatTy));
            Assert.IsFalse(FloatTy.IsAssignable(DoubleTy));
            Assert.IsTrue(UnitTy.IsAssignable(UnitTy));
            Assert.IsTrue(IntTy.IsAssignable(IntTy));
        }

        [TestMethod]
        public void TestSubclassOfWithBaseClass()
        {
            RType baseType = new RType("Bar");
            RType type = new RType("Foo", RTypeAttributes.Class, baseType);
            Assert.IsTrue(baseType.IsAssignable(type));
        }

        [TestMethod]
        public void TestSubclassOfWithBaseBaseClass()
        {
            RType barTy = new RType("Bar");
            RType zooTy = new RType("Zoo", RTypeAttributes.Class, barTy);
            RType fooTy = new RType("Foo", RTypeAttributes.Class, zooTy);
            Assert.IsTrue(barTy.IsAssignable(fooTy));
        }

        [TestMethod]
        public void TestSubclassOfWithInterface()
        {
            RType iBarTy = new RType("IBar", RTypeAttributes.Interface);
            RType fooTy = new RType("Foo");
            fooTy.AddInterfaceImplementation(iBarTy);
            Assert.IsTrue(iBarTy.IsAssignable(fooTy));
        }

        [TestMethod]
        public void TestSpecializedGenericTypesEquality()
        {
            // class Foo[T](val id: T)
            // class SecondFoo[A](id: Int, val name: A) extends Foo[Int](id)
            RType fooTy = new RType("Foo");
            fooTy.DefineGenericParameters("T");
            RType intFooTy = fooTy.MakeGenericType(IntTy);

            RType secondTy = new RType("SecondFoo");
            secondTy.DefineGenericParameters("A");
            secondTy.BaseType = intFooTy;

            Assert.IsTrue(secondTy.IsGenericType);
            Assert.IsTrue(secondTy.IsGenericTypeDefinition);

            Assert.IsTrue(intFooTy.IsAssignable(secondTy));
        }


        [TestMethod]
        public void TestOneGenericContainerWith2DistinctClasses()
        {
            // class List[A]
            // class Apple
            // class Orange
            // listOfApples = new List[Apple]()
            // listOfOranges = new List[Orange]()

            RType listTy = new RType("List");
            listTy.DefineGenericParameters("A");

            RType appleTy = new RType("Apple");
            RType listOfApplesTy = listTy.MakeGenericType(appleTy);

            RType orangeTy = new RType("Orange");
            RType listOfOrangesTy = listTy.MakeGenericType(orangeTy);

            Assert.IsFalse(listOfOrangesTy.IsAssignable(listOfApplesTy));
            Assert.IsFalse(listOfApplesTy.IsAssignable(listOfOrangesTy));

            Assert.IsTrue(listOfOrangesTy.IsAssignable(listOfOrangesTy));
        }

        [TestMethod]
        [TestCategory("Covariance")]
        public void TestGenericContainerAndInvariantClasses()
        {
            RType listOfFruits, listOfApples;
            CreateTypes(RppGenericParameterVariance.Invariant, out listOfFruits, out listOfApples);

            // List[Fruit] = List[Apple]
            Assert.IsFalse(listOfFruits.IsAssignable(listOfApples));
            // List[Apple] = List[Fruit]
            Assert.IsFalse(listOfApples.IsAssignable(listOfFruits));
        }

        [TestMethod]
        [TestCategory("Covariance")]
        public void TestGenericContainerAndCovariantClasses()
        {
            RType listOfFruits, listOfApples;
            CreateTypes(RppGenericParameterVariance.Covariant, out listOfFruits, out listOfApples);

            // List[Fruit] = List[Apple]
            Assert.IsTrue(listOfFruits.IsAssignable(listOfApples));
            // List[Apple] = List[Fruit]
            Assert.IsFalse(listOfApples.IsAssignable(listOfFruits));
        }

        [TestMethod]
        [TestCategory("Covariance")]
        public void TestGenericContainerAndContravariantClasses()
        {
            RType listOfFruits, listOfApples;
            CreateTypes(RppGenericParameterVariance.Contravariant, out listOfFruits, out listOfApples);

            // List[Fruit] = List[Apple]
            Assert.IsFalse(listOfFruits.IsAssignable(listOfApples));
            // List[Apple] = List[Fruit]
            Assert.IsTrue(listOfApples.IsAssignable(listOfFruits));
        }

        /// <summary>
        /// Creates 2 types, List[Fruit] and List[Apple], where:
        /// <code>
        /// class List[A]
        /// class Fruit
        /// class Apple extends Fruit
        /// </code>
        /// </summary>
        /// <param name="variance">variance type for type argument <code>'A'</code></param>
        /// <param name="listOfFruits"></param>
        /// <param name="listOfApples"></param>
        private static void CreateTypes(RppGenericParameterVariance variance, out RType listOfFruits, out RType listOfApples)
        {
            RType listTy = new RType("List");
            RppGenericParameter[] genericParameters = listTy.DefineGenericParameters("A");
            genericParameters[0].Variance = variance;
            RType fruitTy = new RType("Fruit");

            RType listOfFruitsTy = listTy.MakeGenericType(fruitTy);

            RType appleTy = new RType("Apple", RTypeAttributes.Class, fruitTy);
            listOfApples = listTy.MakeGenericType(appleTy);

            listOfFruits = listOfFruitsTy;
        }

        [TestMethod]
        public void ExtendingSpecializedGenericAndDefineOneMoreGenericParameter()
        {
            // class Foo[T]
            // class SecondFoo[A] extends Foo[Int]

            // Foo[Int] = SecondFoo[String]
            RType fooTy = new RType("Foo");
            fooTy.DefineGenericParameters("T");

            RType intFooTy = fooTy.MakeGenericType(IntTy);
            RType secondFooTy = new RType("SecondFoo", RTypeAttributes.Class, intFooTy);

            Assert.IsTrue(intFooTy.IsAssignable(secondFooTy));
        }
    }
}