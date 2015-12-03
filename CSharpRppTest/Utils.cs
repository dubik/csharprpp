using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Antlr.Runtime;
using CSharpRpp;
using CSharpRpp.Codegen;
using CSharpRpp.Semantics;
using CSharpRpp.Symbols;
using CSharpRpp.TypeSystem;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CSharpRppTest
{
    internal class Utils
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
            SymbolTable scope = new SymbolTable(null);

            RppTypeSystem.PopulateBuiltinTypes(scope);

            WireRuntime(scope);
            CodeGenerator generator = new CodeGenerator(program, "TestAssembly.dll");

            Type2Creator typeCreator = new Type2Creator();
            program.Accept(typeCreator);

            program.PreAnalyze(scope);

            InheritanceConfigurator2 configurator = new InheritanceConfigurator2();
            program.Accept(configurator);

            CreateRType createRType = new CreateRType();
            program.Accept(createRType);

            program.Analyze(scope);

            SemanticAnalyzer semantic = new SemanticAnalyzer();
            program.Accept(semantic);

            InitializeNativeTypes initializeNativeTypes = new InitializeNativeTypes(generator.Module);
            program.Accept(initializeNativeTypes);
            CreateNativeTypes createNativeTypes = new CreateNativeTypes();
            program.Accept(createNativeTypes);

            generator.Generate();
            return generator.Assembly;
        }

        private static void WireRuntime(SymbolTable scope)
        {
            Assembly runtimeAssembly = GetRuntimeAssembly();
            Type[] types = runtimeAssembly.GetTypes();

            foreach (Type type in types)
            {
                string name = type.Name;
                if (type.Name.Contains("`"))
                {
                    name = name.Substring(0, name.IndexOf('`'));
                }

                RType rType = RppTypeSystem.CreateType(name, type);
                scope.AddType(rType);
            }

            scope.AddType(RppTypeSystem.CreateType("Exception", typeof (Exception)));
        }

        private static Assembly GetRuntimeAssembly()
        {
            return Assembly.GetAssembly(typeof (Runtime));
        }

        public static RppProgram Parse(string code)
        {
            RppParser parser = CreateParser(code);
            RppProgram compilationUnit = new RppProgram();
            parser.CompilationUnit(compilationUnit);
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