using System;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CSharpRppTest
{
    [TestClass]
    public class CodeGenTest
    {
        [TestMethod]
        public void ClassEmptyConstructor()
        {
            const string code = @"
class Foo
{
}
";
            var fooTy = Utils.ParseAndCreateType(code, "Foo");
            object foo = Activator.CreateInstance(fooTy);
            Assert.IsNotNull(foo);
        }

        [TestMethod]
        public void OneClassParamConstructor()
        {
            const string code = @"
class Foo(val k: Int)
{
}
";
            var fooTy = Utils.ParseAndCreateType(code, "Foo");
            object foo = Activator.CreateInstance(fooTy, new object[] {10});
            Assert.IsNotNull(foo);
            Assert.AreEqual(10, fooTy.GetField("k").GetValue(foo));
        }

        [TestMethod]
        public void TestMainFunc()
        {
            const string code = @"
object Foo
{
    def main(args: Array[String]) : Unit = {
    }
}
";
            var fooTy = Utils.ParseAndCreateType(code, "Foo");
            MethodInfo mainMethod = fooTy.GetMethod("main", BindingFlags.Static | BindingFlags.Public);
            Assert.IsNotNull(mainMethod);
            ParameterInfo[] p = mainMethod.GetParameters();
            Assert.AreEqual(typeof (void), mainMethod.ReturnType);
            Assert.AreEqual(1, p.Length);
            Assert.AreEqual(typeof (String[]), p[0].ParameterType);
        }

        [TestMethod]
        public void TestSimpleExpression()
        {
            const string code = @"
object Foo
{
    def calculate(x : Int, y : Int) : Int = x + y
}
";
            var fooTy = Utils.ParseAndCreateType(code, "Foo");
            MethodInfo calculate = fooTy.GetMethod("calculate", BindingFlags.Static | BindingFlags.Public);
            Assert.IsNotNull(calculate);
            object res = calculate.Invoke(null, new object[] {2, 7});
            Assert.IsNotNull(res);
            Assert.AreEqual(9, res);
        }

        [TestMethod]
        public void TestVarDecl()
        {
            const string code = @"
object Foo
{
    def calculate : Int = {
        var k : Int = 13
        k
    }
}
";
            var fooTy = Utils.ParseAndCreateType(code, "Foo");
            MethodInfo calculate = fooTy.GetMethod("calculate", BindingFlags.Static | BindingFlags.Public);
            Assert.IsNotNull(calculate);
            object res = calculate.Invoke(null, null);
            Assert.IsNotNull(res);
            Assert.AreEqual(13, res);
        }

        [TestMethod]
        public void TestReturnField()
        {
            const string code = @"
class Foo(val k: Int)
{
    def readK() : Int = {
        k
    }
}
";
            var fooTy = Utils.ParseAndCreateType(code, "Foo");
            MethodInfo readK = fooTy.GetMethod("readK", BindingFlags.Public | BindingFlags.Instance);
            object foo = Activator.CreateInstance(fooTy, new object[] {27});
            object res = readK.Invoke(foo, null);
            Assert.AreEqual(27, res);
        }

        [TestMethod]
        public void TestInstanceMethodInvocation()
        {
            const string code = @"
class Foo
{
    def power(k : Int) : Int = {
        k * k
    }

    def calculate(k : Int) : Int = {
        power(k) + 10
    }
}
";
            var fooTy = Utils.ParseAndCreateType(code, "Foo");
            object foo = Activator.CreateInstance(fooTy, null);
            MethodInfo calculate = fooTy.GetMethod("calculate", BindingFlags.Public | BindingFlags.Instance);
            object res = calculate.Invoke(foo, new object[] {3});
            Assert.AreEqual(19, res);
        }

        [TestMethod]
        public void TestNewOperator()
        {
            const string code = @"
class Foo
{
}

object Bar
{
    def create : Foo = {
        new Foo
    }
}
";
            var barTy = Utils.ParseAndCreateType(code, "Bar");
            MethodInfo create = barTy.GetMethod("create", BindingFlags.Static | BindingFlags.Public);
            object res = create.Invoke(null, null);
            Assert.IsNotNull(res);
        }

        [TestMethod]
        public void TestNewOperatorWithArgs()
        {
            const string code = @"
class Foo(k : Int)
{
}

object Bar
{
    def create : Foo = {
        new Foo(10)
    }
}
";
            var barTy = Utils.ParseAndCreateType(code, "Bar");
            MethodInfo create = barTy.GetMethod("create", BindingFlags.Static | BindingFlags.Public);
            object fooInstance = create.Invoke(null, null);
            Assert.IsNotNull(fooInstance);
            object res = fooInstance.GetType().GetField("k").GetValue(fooInstance);
            Assert.AreEqual(10, res);
        }

        [TestMethod]
        public void CallFuncOfInstance()
        {
            const string code = @"
class Foo(k : Int)
{
    def calculate(x : Int) : Int = {
        k + x
    }
}

object Bar
{
    def create : Int = {
        val p : Foo = new Foo(10)
        p.calculate(13)
    }
}
";
            var barTy = Utils.ParseAndCreateType(code, "Bar");
            MethodInfo create = barTy.GetMethod("create", BindingFlags.Static | BindingFlags.Public);
            object fooInstance = create.Invoke(null, null);
            Assert.IsNotNull(fooInstance);
        }

        [TestMethod]
        public void LengthOfVarArgArg()
        {
            const string code = @"
object Bar
{
    def concat(args: Int*) : Int = {
        args.length()
    }
}
";
            var barTy = Utils.ParseAndCreateType(code, "Bar");
            MethodInfo concat = barTy.GetMethod("concat", BindingFlags.Static | BindingFlags.Public);
            object res = concat.Invoke(null, new object[] {new[] {10, 20}});
            Assert.AreEqual(2, res);
        }

        [TestMethod]
        public void CallVarArgFunction()
        {
            const string code = @"
object Bar
{
    def concat(args: Int*) : Int = {
        args.length()
    }

    def invokeConcat() : Int = {
        concat(10, 20)
    }
}
";
            var barTy = Utils.ParseAndCreateType(code, "Bar");
            MethodInfo concat = barTy.GetMethod("invokeConcat", BindingFlags.Static | BindingFlags.Public);
            object res = concat.Invoke(null, null);
            Assert.AreEqual(2, res);
        }


        [TestMethod]
        public void ImplicitBoxing()
        {
            const string code = @"
object Bar
{
    def invoke() : Any = {
        val p : Any = 10
        p
    }
}
";
            var barTy = Utils.ParseAndCreateType(code, "Bar");
            MethodInfo concat = barTy.GetMethod("invoke", BindingFlags.Static | BindingFlags.Public);
            object res = concat.Invoke(null, null);
            Assert.AreEqual(10, res);
        }

        [TestMethod]
        public void GetFloat()
        {
            const string code = @"
object Bar
{
    def invoke() : Float = {
        val p : Float = 10.10
        p
    }
}
";
            var barTy = Utils.ParseAndCreateType(code, "Bar");
            MethodInfo concat = barTy.GetMethod("invoke", BindingFlags.Static | BindingFlags.Public);
            object res = concat.Invoke(null, null);
            Assert.AreEqual(10.10f, res);
        }

        [TestMethod]
        public void SimpleWhile()
        {
            const string code = @"
object Bar
{
    def invoke() : Int = {
        val p : Int = 10
        var ret : Int = 0
        while(p > 0)
        {
            ret = ret + 1
            p = p - 1
        }
        ret
    }
}
";
            var barTy = Utils.ParseAndCreateType(code, "Bar");
            MethodInfo concat = barTy.GetMethod("invoke", BindingFlags.Static | BindingFlags.Public);
            object res = concat.Invoke(null, null);
            Assert.AreEqual(10, res);
        }

        [TestMethod]
        public void AutoBoxingForVarargs()
        {
            const string code = @"
object Bar
{
    def varargs(args: Any*) : Int = {
        args.length()
    }

    def invoke() : Int = {
        varargs(10, 3)
    }
}
";
            var barTy = Utils.ParseAndCreateType(code, "Bar");
            MethodInfo concat = barTy.GetMethod("invoke", BindingFlags.Static | BindingFlags.Public);
            object res = concat.Invoke(null, null);
            Assert.AreEqual(2, res);
        }

        [TestMethod]
        public void InvokeFunctionFromDifferentObject()
        {
            const string code = @"
object Foo
{
    def length() : Int = 10
}

object Bar
{
    def invoke() : Int = {
        Foo.length()
    }
}
";
            var barTy = Utils.ParseAndCreateType(code, "Bar");
            MethodInfo concat = barTy.GetMethod("invoke", BindingFlags.Static | BindingFlags.Public);
            object res = concat.Invoke(null, null);
            Assert.AreEqual(10, res);
        }
    }
}