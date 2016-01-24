using System;
using System.Collections.Generic;
using System.Linq;
using CSharpRpp;
using CSharpRpp.TypeSystem;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static CSharpRpp.ListExtensions;
using static CSharpRpp.TypeSystem.RppTypeSystem;

namespace CSharpRppTest
{
    [TestClass]
    public class TypeInferenceTest
    {
        [TestMethod, TestCategory("Type Inference")]
        public void GenericFunctionWithOneGenericParameter()
        {
            // map(32) -> map[Int](Int): Int;
            TestTypeInference("map[A](x: A) : A", List(CreateUniqueType(), IntTy, IntTy), List(IntTy, IntTy, IntTy));
        }

        [TestMethod, TestCategory("Type Inference")]
        public void ComplexGenericFunction()
        {
            // val k : String = map(13, 2.6)
            TestTypeInference("map[A, B, C](x: A, y: B) : C",
                List(CreateUniqueType(), CreateUniqueType(), CreateUniqueType(), IntTy, FloatTy, StringTy),
                List(IntTy, FloatTy, StringTy, IntTy, FloatTy, StringTy));
        }

        [TestMethod, TestCategory("Type Inference")]
        public void SimpleClosure()
        {
            RType undefined = CreateUniqueType();

            const string method = "map[A, B](x: A => B, y: B) : B";
            // map(x: Int => x, 13)
            var actualParamTypes = List(undefined, undefined, CreateClosureType(IntTy, undefined), IntTy, undefined);
            var expectedInferredTypes = List(IntTy, IntTy, CreateClosureType(IntTy, IntTy), IntTy, IntTy);
            TestTypeInference(method, actualParamTypes, expectedInferredTypes);
        }

        [TestMethod]
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

        [TestMethod]
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

        [TestMethod]
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

        [TestMethod]
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
                genericParams[i].Covariance = RppGenericParameterCovariance.Contravariant;
            }

            genericParams[genericParams.Length - 1].Covariance = RppGenericParameterCovariance.Covariant;
            return closureTy;
        }

        #endregion
    }
}