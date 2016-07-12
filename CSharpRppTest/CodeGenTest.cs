using System;
using System.Reflection;
using NUnit.Framework;

namespace CSharpRppTest
{
    [TestFixture]
    public class CodeGenTest
    {
        [Test]
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

        [Test]
        public void OneClassParamConstructor()
        {
            const string code = @"
class Foo(val k: Int)
{
}
";
            var fooTy = Utils.ParseAndCreateType(code, "Foo");
            object foo = Activator.CreateInstance(fooTy, 10);
            Assert.IsNotNull(foo);
            Assert.AreEqual(10, fooTy.GetProperty("k").GetValue(foo));
        }

        [Test]
        public void TestSimpleExpression()
        {
            const string code = @"
object Foo
{
    def calculate(x : Int, y : Int) : Int = x + y
}
";
            var fooTy = Utils.ParseAndCreateType(code, "Foo$");
            object res = Utils.InvokeStatic(fooTy, "calculate", new object[] {2, 7});
            Assert.IsNotNull(res);
            Assert.AreEqual(9, res);
        }

        [Test]
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
            var fooTy = Utils.ParseAndCreateType(code, "Foo$");
            object res = Utils.InvokeStatic(fooTy, "calculate");
            Assert.IsNotNull(res);
            Assert.AreEqual(13, res);
        }

        [Test]
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
            object foo = Activator.CreateInstance(fooTy, 27);
            object res = readK.Invoke(foo, null);
            Assert.AreEqual(27, res);
        }

        [Test]
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

        [Test]
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
            var barTy = Utils.ParseAndCreateType(code, "Bar$");
            object res = Utils.InvokeStatic(barTy, "create");
            Assert.IsNotNull(res);
        }

        [Test]
        public void TestNewOperatorWithArgs()
        {
            const string code = @"
class Foo(val k : Int)
{
}

object Bar
{
    def create : Foo = {
        new Foo(10)
    }
}
";
            var barTy = Utils.ParseAndCreateType(code, "Bar$");
            object fooInstance = Utils.InvokeStatic(barTy, "create");
            Assert.IsNotNull(fooInstance);
            object res = fooInstance.GetPropertyValue("k");
            Assert.AreEqual(10, res);
        }

        [Test]
        public void CallFuncOfInstance()
        {
            const string code = @"
class Foo(val k : Int)
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
            var barTy = Utils.ParseAndCreateType(code, "Bar$");
            object fooInstance = Utils.InvokeStatic(barTy, "create");
            Assert.IsNotNull(fooInstance);
        }

        [Test]
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
            var barTy = Utils.ParseAndCreateType(code, "Bar$");
            object res = Utils.InvokeStatic(barTy, "concat", new object[] {new[] {10, 20}});
            Assert.AreEqual(2, res);
        }

        [Test]
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
            var barTy = Utils.ParseAndCreateType(code, "Bar$");
            object res = Utils.InvokeStatic(barTy, "invokeConcat");
            Assert.AreEqual(2, res);
        }

        [Test]
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
            var barTy = Utils.ParseAndCreateType(code, "Bar$");
            object res = Utils.InvokeStatic(barTy, "invoke");
            Assert.AreEqual(10, res);
        }

        [Test]
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
            var barTy = Utils.ParseAndCreateType(code, "Bar$");
            object res = Utils.InvokeStatic(barTy, "invoke");
            Assert.AreEqual(10.10f, res);
        }

        [Test]
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
            var barTy = Utils.ParseAndCreateType(code, "Bar$");
            object res = Utils.InvokeStatic(barTy, "invoke");
            Assert.AreEqual(10, res);
        }

        [Test]
        public void ComplexWhile()
        {
            const string code = @"
object Bar
{
    def invoke() : Int = {
        val p : Int = 10
        var ret : Int = 0
        while(p >= 0 && ret < 5)
        {
            ret = ret + 1
            p = p - 1
        }
        ret
    }
}
";
            var barTy = Utils.ParseAndCreateType(code, "Bar$");
            object res = Utils.InvokeStatic(barTy, "invoke");
            Assert.AreEqual(5, res);
        }


        [Test]
        public void FuncAsWhileCondition()
        {
            const string code = @"
object Bar
{
    def hasNext() : Boolean = false

    def invoke() : Int = {
        var ret : Int = 13
        while(hasNext())
        {
            ret = 0
        }
        ret
    }
}
";
            var barTy = Utils.ParseAndCreateType(code, "Bar$");
            object res = Utils.InvokeStatic(barTy, "invoke");
            Assert.AreEqual(13, res);
        }

        [Test]
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
            var barTy = Utils.ParseAndCreateType(code, "Bar$");
            object res = Utils.InvokeStatic(barTy, "invoke");
            Assert.AreEqual(2, res);
        }

        [Test]
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
            var barTy = Utils.ParseAndCreateType(code, "Bar$");
            object res = Utils.InvokeStatic(barTy, "invoke");
            Assert.AreEqual(10, res);
        }

        [Test]
        public void TestCompanionObjectWithoutArgs()
        {
            const string code = @"
class Foo
{
}

object Foo
{
    def apply() : Foo = new Foo
}

object Bar
{
    def create() : Foo = Foo()
}
";
            var barTy = Utils.ParseAndCreateType(code, "Bar$");
            object res = Utils.InvokeStatic(barTy, "create");
            Assert.IsNotNull(res);
            Assert.AreEqual("Foo", res.GetType().Name);
        }

        [Test]
        public void TestCompanionObjectWithOneArg()
        {
            const string code = @"
class Foo(val id: Int)
{
}

object Foo
{
    def apply(id: Int) : Foo = new Foo(id)
}

object Bar
{
    def create() : Int = {
        val foo : Foo = Foo(10)
        foo.id
    }
}
";
            var barTy = Utils.ParseAndCreateType(code, "Bar$");
            object res = Utils.InvokeStatic(barTy, "create");
            Assert.IsNotNull(res);
            Assert.AreEqual(10, res);
        }
    }
}