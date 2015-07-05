using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Antlr.Runtime;
using CSharpRpp;
using CSharpRpp.Codegen;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CSharpRppTest
{
    class Utils
    {
        public static IEnumerable<Type> ParseAndCreateTypes(string code, IEnumerable<string> typesNames)
        {
            RppProgram program = Parse(code);
            var assembly = CodeGen(program);
            return typesNames.Select(assembly.GetType);
        }

        public static Type ParseAndCreateType(string code, string typeName)
        {
            RppProgram program = Parse(code);
            Assert.IsNotNull(program);
            var fooTy = CodeGenAndGetType(program, typeName);
            return fooTy;
        }

        public static Type CodeGenAndGetType(RppProgram program, string typeName)
        {
            var assembly = CodeGen(program);
            Type arrayTy = assembly.GetType(typeName);
            Assert.IsNotNull(arrayTy);
            return arrayTy;
        }

        public static RppProgram ParseAndAnalyze(string code)
        {
            RppProgram program = Parse(code);
            CodeGen(program);
            return program;
        }

        public static Assembly CodeGen(RppProgram program)
        {
            RppScope scope = new RppScope(null);
            CodeGenerator generator = new CodeGenerator(program);
            program.PreAnalyze(scope);
            generator.PreGenerate();
            program.Analyze(scope);
            generator.Generate();
            return generator.Assembly;
        }

        public static RppProgram Parse(string code)
        {
            RppParser parser = CreateParser(code);
            RppProgram compilationUnit = parser.CompilationUnit();
            compilationUnit.Name = "TestedAssembly";
            return compilationUnit;
        }

        public static RppParser CreateParser(string code)
        {
            ANTLRStringStream input = new ANTLRStringStream(code);
            RppLexer lexer = new RppLexer(input);
            CommonTokenStream tokenStream = new CommonTokenStream(lexer);
            RppParser parser = new RppParser(tokenStream);
            return parser;
        }
    }
}