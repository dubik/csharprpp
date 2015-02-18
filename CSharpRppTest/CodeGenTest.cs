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
            RppProgram program = Parse(code);
            Assert.IsNotNull(program);

            var fooTy = CodeGenAndGetType(program, "Foo");
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
            RppProgram program = Parse(code);
            Assert.IsNotNull(program);
            var fooTy = CodeGenAndGetType(program, "Foo");
            object foo = Activator.CreateInstance(fooTy, new object[] {10});
            Assert.IsNotNull(foo);
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
            Assembly assembly = generator.Assembly;
            return assembly;
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