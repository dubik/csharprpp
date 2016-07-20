using System;
using System.Collections.Generic;
using System.Linq;
using CSharpRpp;
using CSharpRpp.Exceptions;
using CSharpRpp.TypeSystem;
using NUnit.Framework;
using static CSharpRpp.ListExtensions;
using static CSharpRpp.TypeSystem.RppTypeSystem;

namespace CSharpRppTest
{
    [TestFixture]
    public class TypeInferenceTest
    {
        [Test, Category("Type Inference")]
        public void GenericFunctionWithOneGenericParameter()
        {
            // map(32) -> map[Int](Int): Int;
            TestTypeInference("map[A](x: A) : A", List(CreateUniqueType(), IntTy, IntTy), List(IntTy, IntTy, IntTy));
        }

        [Test, Category("Type Inference")]
        public void ComplexGenericFunction()
        {
            // val k : String = map(13, 2.6)
            TestTypeInference("map[A, B, C](x: A, y: B) : C",
                List(CreateUniqueType(), CreateUniqueType(), CreateUniqueType(), IntTy, FloatTy, StringTy),
                List(IntTy, FloatTy, StringTy, IntTy, FloatTy, StringTy));
        }

        [Test, Category("Type Inference")]
        public void SimpleClosure()
        {
            RType undefined = CreateUniqueType();

            const string method = "map[A, B](x: A => B, y: B) : B";
            // map(x: Int => x, 13)
            var actualParamTypes = List(undefined, undefined, CreateClosureType(IntTy, undefined), IntTy, undefined);
            var expectedInferredTypes = List(IntTy, IntTy, CreateClosureType(IntTy, IntTy), IntTy, IntTy);
            TestTypeInference(method, actualParamTypes, expectedInferredTypes);
        }

        [Test]
        public void SimpleGenericFunctionCallInference()
        {
            const string code = @"
object Main {
    def func[A](x: A) : A = x

    def main : Int = func(13)
}
";
            Type mainTy = Utils.ParseAndCreateType(code, "Main$");
            object res = Utils.InvokeStatic(mainTy, "main");
            Assert.AreEqual(13, res);
        }

        [Test]
        public void InferenceOfFuncCallWhichHasAnotherFuncCallAsParameter()
        {
            const string code = @"
class MTuple1[A](val item1: A)
class MTuple2[A,B](val item1: A, val item2: B)

object MTuple {
  def create[T1](arg1: T1) : MTuple1[T1] = new MTuple1[T1](arg1)
  def create[T1, T2](arg1: T1, arg2: T2) : MTuple2[T1, T2] = new MTuple2[T1, T2](arg1, arg2)
}

object Main {
    def main: MTuple1[MTuple2[Int, Float]] = {
        val t = MTuple.create(MTuple.create(13, 24.34))
        t
    }
}
";
            Type mainTy = Utils.ParseAndCreateType(code, "Main$");
            object res = Utils.InvokeStatic(mainTy, "main");
            Assert.IsNotNull(res);
            Assert.AreEqual("MTuple1", res.GetType().Name);
            object item1 = res.GetPropertyValue("item1");
            Assert.IsNotNull(item1);
            Assert.AreEqual(13, item1.GetPropertyValue("item1"));
            Assert.AreEqual(24.34f, item1.GetPropertyValue("item2"));
        }

        [Test]
        public void InfereTypesWhenObjectIsInstantiated()
        {
            const string code = @"
class TOption[A](val item: A)

object Main {
    def main: TOption[Int] = new TOption(132)
}
";
            Type mainTy = Utils.ParseAndCreateType(code, "Main$");
            object res = Utils.InvokeStatic(mainTy, "main");
            Assert.IsNotNull(res);
        }

        [Test]
        public void InfereTypesWhenClassIsInstantiatedFromGenericMethod()
        {
            const string code = @"
class Node[A](val item: A)

object Main {
    def main[A](p: A): Node[A] = new Node(p)
    def mainInt: Node[Int] = main[Int](123)
}
";
            Type mainTy = Utils.ParseAndCreateType(code, "Main$");
            object res = Utils.InvokeStatic(mainTy, "mainInt");
            Assert.IsNotNull(res);
        }

        [Test]
        public void InfereTypesWhenSubClassOfAnObjectIsInstantiated()
        {
            const string code = @"
class XList[A]
class XCons[A](val head: A) extends XList[A]

object Main {
  def main: XList[Int] = {
    var k = new XCons(13)
    k
  }
}
";
            Type mainTy = Utils.ParseAndCreateType(code, "Main$");
            object res = Utils.InvokeStatic(mainTy, "main");
            Assert.IsNotNull(res);
            object head = res.GetPropertyValue("head");
            Assert.AreEqual(13, head);
        }

        [Test]
        public void InfereTypesWhenSubClassOfAnObjectIsInstantiatedWithGenericParam()
        {
            const string code = @"
class XList[X]
class XCons[Y](val head: Y) extends XList[Y]

class XConsApp[A] {
  def make(a: A) : XList[A] = new XCons(a)
}
";
            Type xconsAppTy = Utils.ParseAndCreateType(code, "XConsApp");
            Assert.IsNotNull(xconsAppTy);
        }

        [Test]
        public void InferTypeUsingReturnType()
        {
            const string code = @"
class Foo[+A](val foo: Foo[A]) {
  def create: Foo[A] = new Foo(this)
}

object Main {
  def create: Foo[Int] = {
    new Foo(null)
  }
}
";
            Assert.Throws<SemanticException>(() => Utils.ParseAndCreateType(code, "Main$"), "type mismatch");
        }

        [Test]
        public void InferSimpleConstructor1()
        {
            const string code = @"
class Foo[A](val v : A)

object Main {
    def main : Int = {
        val k = new Foo(13)
        k.v
    }
}
";
            Type mainTy = Utils.ParseAndCreateType(code, "Main$");
            Assert.IsNotNull(mainTy);
            object res = Utils.InvokeStatic(mainTy, "main");
            Assert.AreEqual(13, res);
        }

        [Test]
        public void InferSimpleConstructor2()
        {
            const string code = @"
class Foo[A](val v : A)

object Main {
    def main : Foo[Int] = new Foo(13)
}
";
            Type mainTy = Utils.ParseAndCreateType(code, "Main$");
            Assert.IsNotNull(mainTy);
            object res = Utils.InvokeStatic(mainTy, "main");
            Assert.AreEqual(13, res.GetPropertyValue("v"));
        }

        #region Utils

        private static void TestTypeInference(string methodCode, IEnumerable<RType> callTypes, IEnumerable<RType> expectedInferredTypes)
        {
            RppMethodInfo mapMethod = CreateMethodType(methodCode);
            IList<RType> mapMethodTypes = InferenceContext.TypesAsList(mapMethod);

            IEnumerable<RType> inferredTypes = TypeInference.InferTypes(callTypes, mapMethodTypes);
            CollectionAssert.AreEqual(expectedInferredTypes.ToList(), inferredTypes.ToList());
        }

        private static RppMethodInfo CreateMethodType(string funcString)
        {
            string code = $"abstract class Main {{\r\n def {funcString}\r\n}}";
            RType mainTy = Utils.ParseAndAnalyze(code).Classes.First().Type;
            Assert.IsNotNull(mainTy);
            RppMethodInfo methodInfo = mainTy.Methods[0];
            Assert.IsNotNull(methodInfo);
            return methodInfo;
        }

        private static int _id;

        private static RType CreateUniqueType()
        {
            return new RType($"Undefined{_id++}");
        }

        private static RType CreateClosureType(params RType[] genericArguments)
        {
            return CreateClosureType(genericArguments.Length - 1).MakeGenericType(genericArguments);
        }

        private static RType CreateClosureType(int argCount)
        {
            RType closureTy = new RType("Function");
            string[] genericNames = Enumerable.Range(0, argCount).Select(r => $"T{r + 1}").Concat("TResult").ToArray();
            RppGenericParameter[] genericParams = closureTy.DefineGenericParameters(genericNames);
            for (int i = 0; i < genericParams.Length - 1; i++)
            {
                genericParams[i].Variance = RppGenericParameterVariance.Contravariant;
            }

            genericParams[genericParams.Length - 1].Variance = RppGenericParameterVariance.Covariant;
            return closureTy;
        }

        #endregion
    }
}