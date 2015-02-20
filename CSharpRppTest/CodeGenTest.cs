using System;
using System.Reflection;
using Antlr.Runtime;
using CSharpRpp;
using CSharpRpp.Codegen;
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
            var fooTy = ParseAndCreateType(code, "Foo");
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
            var fooTy = ParseAndCreateType(code, "Foo");
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
            var fooTy = ParseAndCreateType(code, "Foo");
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
            var fooTy = ParseAndCreateType(code, "Foo");
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
            var fooTy = ParseAndCreateType(code, "Foo");
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
            var fooTy = ParseAndCreateType(code, "Foo");
            MethodInfo readK = fooTy.GetMethod("readK", BindingFlags.Public | BindingFlags.Instance);
            object foo = Activator.CreateInstance(fooTy, new object[] {27});
            object res = readK.Invoke(foo, null);
            Assert.AreEqual(27, res);
        }

        private static Type ParseAndCreateType(string code, string typeName)
        {
            RppProgram program = Parse(code);
            Assert.IsNotNull(program);
            var fooTy = CodeGenAndGetType(program, typeName);
            return fooTy;
        }

        private static Type CodeGenAndGetType(RppProgram program, string typeName)
        {
            var assembly = CodeGen(program);
            Type arrayTy = assembly.GetType(typeName);
            Assert.IsNotNull(arrayTy);
            return arrayTy;
        }

        private static Assembly CodeGen(RppProgram program)
        {
            RppScope scope = new RppScope(null);
            CodeGenerator generator = new CodeGenerator(program);
            program.PreAnalyze(scope);
            generator.PreGenerate();
            program.Analyze(scope);
            generator.Generate();
            return generator.Assembly;
        }

        private static RppProgram Parse(string code)
        {
            RppParser parser = CreateParser(code);
            RppProgram compilationUnit = parser.CompilationUnit();
            compilationUnit.Name = "TestedAssembly";
            return compilationUnit;
        }

        private static RppParser CreateParser(string code)
        {
            ANTLRStringStream input = new ANTLRStringStream(code);
            RppLexer lexer = new RppLexer(input);
            CommonTokenStream tokenStream = new CommonTokenStream(lexer);
            RppParser parser = new RppParser(tokenStream);
            return parser;
        }
    }
}