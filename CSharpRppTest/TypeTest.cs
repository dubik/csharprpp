using System.Reflection;
using CSharpRpp.TypeSystem;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static CSharpRpp.TypeSystem.RppTypeSystem;

namespace CSharpRppTest
{
    [TestClass]
    public class TypeTest
    {
        [TestMethod]
        public void FigureOutTypeForVarDeclarationWithIntInitExpression()
        {
            const string code = @"
object Foo
{
    def main() : Int = {
        val k = 10
        k
    }
}
";
            var fooTy = Utils.ParseAndCreateType(code, "Foo$");
            var res = Utils.InvokeStatic(fooTy, "main");
            Assert.AreEqual(10, res);
        }

        [TestMethod]
        [TestCategory("Closures"), TestCategory("Generics")]
        public void FigureOutTypeForVarDeclarationWithClosureInitExpression()
        {
            const string code = @"
object Foo
{
    def main() : (Int => Int) = {
        val func = (x: Int) => x + 10
        func
    }
}
";
            var fooTy = Utils.ParseAndCreateType(code, "Foo$");
            var res = Utils.InvokeStatic(fooTy, "main");
            MethodInfo applyMethod = res.GetType().GetMethod("apply");
            Assert.IsNotNull(applyMethod);
            var ret = applyMethod.Invoke(res, new object[] {13});
            Assert.AreEqual(23, ret);
        }

        [TestMethod]
        [TestCategory("Closures"), TestCategory("Generics")]
        public void FigureOutTypeOfClosureBasedVariableType()
        {
            const string code = @"
object Foo
{
    def main() : (Int => Int) = {
        val func : (Int => Int) = (x) => x + 10
        func
    }
}
";
            var fooTy = Utils.ParseAndCreateType(code, "Foo$");
            Utils.InvokeStatic(fooTy, "main");
        }

        [TestMethod]
        [TestCategory("Closures"), TestCategory("Generics")]
        public void PassClosureAsAParam()
        {
            const string code = @"
object Bar
{
    def invoker(func: (Int, Int) => Int) : Int = {
        func(10, 24)
    }

    def main() : Int = {
        invoker((x, y) => x + y)
    }
}
";
            var barTy = Utils.ParseAndCreateType(code, "Bar$");
            var res = Utils.InvokeStatic(barTy, "main");
            Assert.AreEqual(34, res);
        }

        [TestMethod]
        public void ClosureWhichDoesntReturnAValue()
        {
            const string code = @"
object Foo
{
    def printSomething: Unit = {
        val k = 10
    }

    def main() : Unit = {
        val action = (i: Int) => printSomething()
        action(13)
    }
}
";
            var fooTy = Utils.ParseAndCreateType(code, "Foo$");
            Assert.IsNotNull(fooTy);
            Utils.InvokeStatic(fooTy, "main");
        }

        [TestMethod]
        public void TestIsInstanceOfSimpleClass()
        {
            RType fooTy = new RType("Foo", RTypeAttributes.Class, AnyTy);
            Assert.IsTrue(fooTy.IsInstanceOf(AnyTy));
            Assert.IsFalse(fooTy.IsInstanceOf(StringTy));
        }

        [TestMethod]
        public void TestIsInstanceOfClassWhichImplementsInterface()
        {
            RType interfaceTy = new RType("IBar", RTypeAttributes.Interface);
            RType fooTy = new RType("Foo");
            fooTy.AddInterfaceImplementation(interfaceTy);

            Assert.IsTrue(fooTy.IsInstanceOf(interfaceTy));
            RType anotherInterfaceTy = new RType("IBar", RTypeAttributes.Interface);
            Assert.IsTrue(fooTy.IsInstanceOf(anotherInterfaceTy));

            RType wrongInterfaceTy = new RType("IFoo", RTypeAttributes.Interface);
            Assert.IsFalse(fooTy.IsInstanceOf(wrongInterfaceTy));
        }

        [TestMethod]
        public void FindCommonType()
        {
            RType fooTy = new RType("Foo");
            RType barTy = new RType("Bar");

            RType commonTy = TypeInference.ResolveCommonType(fooTy, barTy);
            Assert.IsNotNull(commonTy);
            Assert.AreEqual(AnyTy, commonTy);
        }
    }
}