using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using CSharpRpp.TypeSystem;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static CSharpRpp.ListExtensions;
using static CSharpRpp.TypeSystem.RppTypeSystem;

namespace CSharpRppTest
{
    [TestClass]
    public class GenericsTest
    {
        [TestMethod]
        [TestCategory("Generics")]
        public void SimplestPossibleGeneric()
        {
            const string code = @"
class Foo[T]
";
            Type fooTy = Utils.ParseAndCreateType(code, "Foo");
            Assert.IsNotNull(fooTy);
            Assert.IsTrue(fooTy.IsGenericType);
        }

        [TestMethod]
        [TestCategory("Generics")]
        public void SimplestPossibleGenericWithGenericField()
        {
            const string code = @"
class Foo[T](val k: T)
";
            Type fooTy = Utils.ParseAndCreateType(code, "Foo");
            Assert.IsNotNull(fooTy);
            Assert.IsTrue(fooTy.IsGenericType);
        }

        [TestMethod]
        [TestCategory("Generics")]
        public void ExtendingSimpleGenericClass()
        {
            const string code = @"
class Bar[T]
class Foo[T] extends Bar[T]
";
            Type fooTy = Utils.ParseAndCreateType(code, "Foo");
            Assert.IsNotNull(fooTy);
            Assert.IsTrue(fooTy.IsGenericType);
        }

        [TestMethod]
        [TestCategory("Generics")]
        public void ExtendingSimpleGenericClassWithPredefinedGenericArgument()
        {
            const string code = @"
class Bar[T]
class Foo[T] extends Bar[Int]
";
            Type fooTy = Utils.ParseAndCreateType(code, "Foo");
            Assert.IsNotNull(fooTy);
            Assert.IsTrue(fooTy.IsGenericType);
            Assert.AreEqual(typeof(int), fooTy.BaseType?.GenericTypeArguments[0]);
        }

        [TestMethod]
        [TestCategory("Generics")]
        public void ExtendingClassWithPredefinedGenericArgument()
        {
            const string code = @"
class Bar[T]
class Foo extends Bar[Int]
";
            Type fooTy = Utils.ParseAndCreateType(code, "Foo");
            Assert.IsNotNull(fooTy);
            Assert.IsFalse(fooTy.IsGenericType);
            Assert.AreEqual(typeof(int), fooTy.BaseType?.GenericTypeArguments[0]);
        }

        [TestMethod]
        [TestCategory("Generics")]
        public void GenericStaticFunction()
        {
            const string code = @"
object Bar
{
    def func[A](x: A) : A = x

    def main(name: String) : String = func[String](name)
}
";
            Type barTy = Utils.ParseAndCreateType(code, "Bar$");
            var res = Utils.InvokeStatic(barTy, "main", new object[] {"hello"});
            Assert.AreEqual("hello", res);
        }

        [TestMethod]
        [TestCategory("Generics")]
        public void SimpleGenericClass()
        {
            const string code = @"
class Foo[T](val id: T)

object Bar
{
    def main : Foo[Int] = new Foo[Int](10)
}
";
            Type barTy = Utils.ParseAndCreateType(code, "Bar$");
            var res = Utils.InvokeStatic(barTy, "main");
            Assert.IsNotNull(res);
        }

        [TestMethod]
        [TestCategory("Generics")]
        public void ExtendingSpecializedGeneric()
        {
            const string code = @"
class Foo[T](val id: T)

class SecondFoo(id: Int) extends Foo[Int](id)

object Bar
{
    def main : Foo[Int] = new SecondFoo(10)
}
";
            Type barTy = Utils.ParseAndCreateType(code, "Bar$");
            var res = Utils.InvokeStatic(barTy, "main");
            Assert.IsNotNull(res);
        }

        [TestMethod]
        [TestCategory("Generics")]
        public void ExtendingSpecializedGenericAndDefineOneMoreGenericParameter()
        {
            const string code = @"
class Foo[T](val id: T)

class SecondFoo[A](id: Int, val name: A) extends Foo[Int](id)

object Bar
{
    def main : Foo[Int] = new SecondFoo[String](10, ""Hello"")
}
";
            Type barTy = Utils.ParseAndCreateType(code, "Bar$");
            var res = Utils.InvokeStatic(barTy, "main");
            Assert.IsNotNull(res);
        }

        [TestMethod]
        [TestCategory("Generics")]
        public void ClosureLikeClass()
        {
            const string code = @"
abstract class Func[R, T]
{
    def apply(arg : T) : R
}

class MyClosure extends Func[Int, Float]
{
    override def apply(f: Float) : Int = {
        10
    }
}

object Bar {
    def main : Int = {
        val f : Func[Int, Float] = new MyClosure()
        f.apply(12.3)
    }
}
";
            Type barTy = Utils.ParseAndCreateType(code, "Bar$");
            var res = Utils.InvokeStatic(barTy, "main");
            Assert.IsNotNull(res);
        }

        [TestMethod]
        public void GenericInNestedClasses()
        {
            const string code = @"
class First[A, B] {
  class Second[C, D] {
  }
}
";
            Type firstTy = Utils.ParseAndCreateType(code, "First");
            Assert.IsNotNull(firstTy);
            Assert.AreEqual(2, firstTy.GetGenericArguments().Length);
            CollectionAssert.AreEqual(new[] {"A", "B"}, firstTy.GetGenericArguments().Select(t => t.Name).ToList());
            Type secondTy = firstTy.GetNestedType("Second");
            CollectionAssert.AreEqual(new[] {"A", "B", "C", "D"}, secondTy.GetGenericArguments().Select(t => t.Name).ToList());
        }

        [TestMethod]
        public void ExtendingRuntimeInterface()
        {
            const string code = @"
class Foo extends Function2[Int, Int, Int]
{
    def apply(x: Int, y: Int) : Int = x + y
}
";
            Type fooTy = Utils.ParseAndCreateType(code, "Foo");
            Assert.IsNotNull(fooTy);
            object func = Activator.CreateInstance(fooTy);
            Function2<int, int, int> f = (Function2<int, int, int>) func;
            int res = f.apply(17, 21);
            Assert.AreEqual(38, res);
        }

        [TestMethod]
        public void CallObjectsGenericFunction()
        {
            const string code = @"
object Foo
{
    def func[A](x: A) : A = x
}

object Main
{
    def main() : Int = Foo.func[Int](132)
}
";
            Type mainTy = Utils.ParseAndCreateType(code, "Main$");
            object res = Utils.InvokeStatic(mainTy, "main");
            Assert.AreEqual(132, res);
        }

        [TestMethod]
        public void CallApplyGenericFunction()
        {
            const string code = @"
object Foo
{
    def apply[A](x: A) : A = x
}

object Main
{
    def main() : Int = Foo[Int](132)
}
";
            Type mainTy = Utils.ParseAndCreateType(code, "Main$");
            object res = Utils.InvokeStatic(mainTy, "main");
            Assert.AreEqual(132, res);
        }


        /**
            class List[A]
            class Fruit
            class Apple extends Fruit
        */

        [TestMethod]
        [TestCategory("Covariance")]
        public void DefaultTypesEquality()
        {
            RType listOfFruitsTy;
            RType listOfApplesTy;
            CreateTypes(RppGenericParameterVariance.Invariant, out listOfFruitsTy, out listOfApplesTy);

            // List[Fruit] = List[Apple]
            Assert.IsFalse(listOfFruitsTy.IsAssignable(listOfApplesTy));

            // List[Apple] = List[Fruit]
            Assert.IsFalse(listOfApplesTy.IsAssignable(listOfFruitsTy));
        }

        [TestMethod]
        [TestCategory("Covariance")]
        public void CovariantTypesEquality()
        {
            RType listOfFruitsTy;
            RType listOfApplesTy;
            CreateTypes(RppGenericParameterVariance.Covariant, out listOfFruitsTy, out listOfApplesTy);

            // List[Fruit] = List[Apple]
            Assert.IsTrue(listOfFruitsTy.IsAssignable(listOfApplesTy));

            // List[Apple] = List[Fruit]
            Assert.IsFalse(listOfApplesTy.IsAssignable(listOfFruitsTy));
        }

        [TestMethod]
        [TestCategory("Covariance")]
        public void ContravariantTypesEquality()
        {
            RType listOfFruitsTy;
            RType listOfApplesTy;
            CreateTypes(RppGenericParameterVariance.Contravariant, out listOfFruitsTy, out listOfApplesTy);

            // List[Fruit] = List[Apple]
            Assert.IsFalse(listOfFruitsTy.IsAssignable(listOfApplesTy));

            // List[Apple] = List[Fruit]
            Assert.IsTrue(listOfApplesTy.IsAssignable(listOfFruitsTy));
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
            RType listTy = new RType("List", RTypeAttributes.Interface);
            RppGenericParameter[] genericParameters = listTy.DefineGenericParameters("A");
            genericParameters[0].Variance = variance;
            RType fruitTy = new RType("Fruit");

            RType listOfFruitsTy = listTy.MakeGenericType(fruitTy);

            RType appleTy = new RType("Apple", RTypeAttributes.Class, fruitTy, null);
            listOfApples = listTy.MakeGenericType(appleTy);

            listOfFruits = listOfFruitsTy;
        }

        [TestMethod]
        [TestCategory("Generics"), TestCategory("MethodInflation")]
        public void InflateMinimalMethod()
        {
            RType fooTy = new RType("Foo");
            // func(): Unit
            RppMethodInfo method = new RppMethodInfo("func", fooTy, RMethodAttributes.Public, UnitTy, new RppParameterInfo[0]);
            RppMethodInfo inflatedMethod = method.MakeGenericType(new RType[0]);
            Assert.AreEqual(method, inflatedMethod);
        }

        [TestMethod]
        [TestCategory("Generics"), TestCategory("MethodInflation")]
        public void InflateReturnTypeOfSmallMethod()
        {
            RType fooTy = new RType("Foo");

            // func[A](): A
            RppMethodInfo method = new RppMethodInfo("func", fooTy, RMethodAttributes.Public, UnitTy, new RppParameterInfo[0]);
            RppGenericParameter[] genericParams = method.DefineGenericParameters(new[] {"A"});
            method.ReturnType = genericParams[0].Type;
            RppMethodInfo inflatedMethod = method.MakeGenericType(new[] {IntTy});
            Assert.AreEqual(IntTy, inflatedMethod.ReturnType);
        }

        [TestMethod]
        [TestCategory("Generics"), TestCategory("MethodInflation")]
        public void InflateReturnTypeAndOneParamOfSmallMethod()
        {
            RType fooTy = new RType("Foo");

            // func[A](x: A): A
            RppMethodInfo method = new RppMethodInfo("func", fooTy, RMethodAttributes.Public, UnitTy, new RppParameterInfo[0]);
            RppGenericParameter[] genericParams = method.DefineGenericParameters(new[] {"A"});
            method.Parameters = new[] {new RppParameterInfo("x", genericParams[0].Type)};
            method.ReturnType = genericParams[0].Type;
            RppMethodInfo inflatedMethod = method.MakeGenericType(new[] {IntTy});
            Assert.AreEqual(IntTy, inflatedMethod.ReturnType);
            Assert.AreEqual(IntTy, inflatedMethod.Parameters[0].Type);
        }

        [TestMethod]
        [TestCategory("Generics")]
        public void InflateSimpleGenericType()
        {
            /*
             class Foo[A] {
                def func: A
             }
             */
            RType fooTy = new RType("Foo");
            RType aTy = fooTy.DefineGenericParameters("A").First().Type;
            RppMethodInfo method = fooTy.DefineMethod("foo", RMethodAttributes.Public);
            method.ReturnType = aTy;
            RType intFooTy = fooTy.MakeGenericType(IntTy);
            Assert.AreEqual(IntTy, intFooTy.Methods[0].ReturnType);
        }

        [TestMethod]
        public void InflateBaseClass()
        {
            /*
             class Bar[A] {
               def func: A
             }
             class Foo[X] extends Bar[X]
             */
            RType barTy = new RType("Bar");
            RType aTy = barTy.DefineGenericParameters("A").First().Type;
            RppMethodInfo method = barTy.DefineMethod("foo", RMethodAttributes.Public);
            method.ReturnType = aTy;

            RType fooTy = new RType("Foo");
            RType xTy = fooTy.DefineGenericParameters("X").First().Type;
            fooTy.BaseType = barTy.MakeGenericType(xTy);

            RType specializedType = fooTy.MakeGenericType(IntTy);
            RType specifliedBaseType = specializedType.BaseType;
            Debug.Assert(specifliedBaseType != null, "specifliedBaseType != null");
            var specializedMethods = specifliedBaseType.Methods;
            Assert.AreEqual(IntTy, specializedMethods[0].ReturnType);
        }

        [TestMethod]
        public void InflateTypeTwice()
        {
            RType barTy = new RType("Bar");
            RType bTy = barTy.DefineGenericParameters("B")[0].Type;

            RType fooTy = new RType("Foo");
            RppGenericParameter[] fooTyGenericParams = fooTy.DefineGenericParameters("F");
            RType fTy = fooTyGenericParams[0].Type;

            // Bar[B], Foo[F], B = F, e.g. Bar[F]
            RType barOfFTy = barTy.MakeGenericType(fTy);
            Assert.IsTrue(barOfFTy.IsGenericType);
            var genericArguments = barOfFTy.GenericArguments;
            CollectionAssert.AreEqual(List(fTy), genericArguments.ToList());
            var genericParameters = barOfFTy.GenericParameters.Select(gp => gp.Type).ToList();
            CollectionAssert.AreEqual(List(bTy), genericParameters);
        }


        [TestMethod]
        [TestCategory("Generics")]
        public void TestGenericsConstraint()
        {
            const string code = @"
class Item
class Bag[A <: Item]
";
            Type bagTy = Utils.ParseAndCreateType(code, "Bag");
            Assert.IsTrue(bagTy.ContainsGenericParameters);
            Type[] typeParameters = bagTy.GetGenericArguments();
            Assert.AreEqual("Item", typeParameters[0].BaseType?.Name);
        }

        [TestMethod]
        [TestCategory("Generics")]
        public void ConstraintedMembersShouldBeAvailableToGenericParameter()
        {
            const string code = @"
class Item {
    def read(): String = ""Hello""
}

class Bag[A <: Item] {
    def doSome(f: A): String = {
        f.read()
    }
}

object Main {
    def main: String = {
        val k = new Bag[Item]()
        val item = new Item()
        k.doSome(item)
    }
}
";
            Type bagTy = Utils.ParseAndCreateType(code, "Main$");
            object res = Utils.InvokeStatic(bagTy, "main", null);
            Assert.AreEqual("Hello", res);
        }

        [TestMethod]
        [TestCategory("Generics")]
        public void ConstraintedMembersShouldBeAvailableToMethodGenericParameter()
        {
            const string code = @"
class Item {
    def read(): String = ""Hello""
}
object Main {
    def main[A <: Item](item: A) : String = item.read()

    def execute: String = {
        val item = new Item()
        main[Item](item)
    }
}
";
            Type mainTy = Utils.ParseAndCreateType(code, "Main$");
            object res = Utils.InvokeStatic(mainTy, "execute", null);
            Assert.AreEqual("Hello", res);
        }

        [TestMethod]
        [TestCategory("Generics")]
        public void MixClassAndMethodGenerics()
        {
            const string code = @"
class ObjRef[T](val value: T) {
  def map[U](f: T => U): U = f(value)
}

object Main {
  def main(): Int = {
    val objRef = new ObjRef(13)
    val ret = objRef.map(x => x * 2)
    ret
  }
}
";
            Type mainTy = Utils.ParseAndCreateType(code, "Main$");
            object res = Utils.InvokeStatic(mainTy, "main");
            Assert.AreEqual(26, res);
        }

        [TestMethod]
        [TestCategory("Generics")]
        public void UsingClosuresInFunctionsWithGenericParameters()
        {
            const string code = @"
object XFunc {
  def map[A, U](v: A, f: A => U) : U = f(v)
}

object Main {
  def main: Int = {
    XFunc.map[Int, Int](13, x => x * 2)
  }
}
";
            Type mainTy = Utils.ParseAndCreateType(code, "Main$");
            object res = Utils.InvokeStatic(mainTy, "main");
            Assert.AreEqual(26, res);
        }

        [TestMethod]
        [TestCategory("Generics")]
        public void CallFunctionWithTwoGenericsButInReturnValueUsedOnlyOneGeneric()
        {
            const string code = @"
class Bar[X]

class Foo[A](val k: A) {
  def process[U](x: Bar[A]): Bar[U] = Factory.map[A, U](x)
}

object Factory {
    def map[A, U](x: Bar[A]): Bar[U] = null
}

object Main {
    def main: Any = {
        val f = new Foo(10)
        var b = new Bar[Int]
        f.process[Int](b)
    }
}
";
            Type mainTy = Utils.ParseAndCreateType(code, "Main$");
            object res = Utils.InvokeStatic(mainTy, "main");
            Assert.IsNull(res);
        }

        [Variance("+")]
        internal class Foo<A>
        {
        }

        [TestMethod]
        public void WrapSystemTypeWithVarianceAnnotation()
        {
            RType fooTy = new RType("Foo", typeof(Foo<>), TypeFactory);
            RppGenericParameter genericParameter = fooTy.GenericParameters.First();
            Assert.AreEqual(RppGenericParameterVariance.Covariant, genericParameter.Variance);
        }

        [Variance("+_-")]
        internal class Bar<A, B, C>
        {
        }

        [TestMethod]
        public void ComplexWrapSystemTypeWithVarianceAnnotation()
        {
            RType fooTy = new RType("Bar", typeof(Bar<,,>), TypeFactory);
            var gps = fooTy.GenericParameters.ToArray();
            Assert.AreEqual(RppGenericParameterVariance.Covariant, gps[0].Variance);
            Assert.AreEqual(RppGenericParameterVariance.Invariant, gps[1].Variance);
            Assert.AreEqual(RppGenericParameterVariance.Contravariant, gps[2].Variance);
        }

        private RType TypeFactory(Type type)
        {
            return new RType(type.Name, type, TypeFactory);
        }
    }
}