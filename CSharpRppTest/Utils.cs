using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Antlr.Runtime;
using CSharpRpp;
using CSharpRpp.Codegen;
using CSharpRpp.Native;
using CSharpRpp.Semantics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CSharpRppTest
{
    class Utils
    {
        public static IEnumerable<Type> ParseAndCreateTypes(string code, IEnumerable<string> typesNames)
        {
            RppProgram program = Parse(code);
            var assembly = CodeGen(program, null);
            return typesNames.Select(assembly.GetType);
        }

        public static Type ParseAndCreateType(string code, string typeName, Type additionalType = null)
        {
            RppProgram program = Parse(code);
            Assert.IsNotNull(program);
            var fooTy = CodeGenAndGetType(program, typeName, additionalType);
            return fooTy;
        }

        public static Type CodeGenAndGetType(RppProgram program, string typeName, Type additionalType)
        {
            var assembly = CodeGen(program, additionalType);
            Type arrayTy = assembly.GetType(typeName);
            Assert.IsNotNull(arrayTy);
            return arrayTy;
        }

        public static RppProgram ParseAndAnalyze(string code)
        {
            RppProgram program = Parse(code);
            CodeGen(program, null);
            return program;
        }

        public static Assembly CodeGen(RppProgram program, Type additionalType)
        {
            RppScope scope = new RppScope(null);
            if (additionalType != null)
            {
                scope.Add(new RppNativeClass(additionalType));
            }

            CodeGenerator generator = new CodeGenerator(program);
            program.PreAnalyze(scope);
            generator.PreGenerate();
            program.Analyze(scope);
            SemanticAnalyzer semantic = new SemanticAnalyzer();
            program.Accept(semantic);
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

        public static object InvokeStatic(Type type, string name)
        {
            return InvokeStatic(type, name, null);
        }

        public static object InvokeStatic(Type type, string name, object[] @params)
        {
            var instance = GetObjectInstance(type);
            MethodInfo method = type.GetMethod(name, BindingFlags.Public | BindingFlags.Instance);
            return method.Invoke(instance, @params);
        }

        public static object GetObjectInstance(Type type)
        {
            var instanceField = type.GetField("_instance");
            return instanceField.GetValue(null);
        }
    }
}