﻿using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using CSharpRpp.Codegen;
using CSharpRpp.Reporting;
using CSharpRpp.Symbols;
using CSharpRpp.TypeSystem;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CSharpRppTest
{
    [TestClass]
    public class TypeSystemTest
    {
        private ModuleBuilder _module;

        [TestMethod]
        public void PrimitiveTypeEquality()
        {
            var t = new RType("Int");
            var t1 = new RType("Int");
            Assert.AreEqual(t, t1);
        }

        [TestMethod]
        public void TestVisitors()
        {
            const string code = @"
class Foo
class Bar extends Foo
object Main
";
            var program = Utils.Parse(code);

            SymbolTable scope = new SymbolTable();
            RppTypeSystem.PopulateBuiltinTypes(scope);

            Type2Creator typeCreator = new Type2Creator();
            program.Accept(typeCreator);
            program.PreAnalyze(scope);
            InheritanceConfigurator2 configurator = new InheritanceConfigurator2();
            program.Accept(configurator);
            var creator = new CreateRType(new Diagnostic());
            program.Accept(creator);
            var classes = program.Classes.Where(c => !c.Name.Contains("Function")).ToArray(); // Remove Functions* from runtime
            var fooType = classes[0].Type;
            var barType = classes[1].Type;
            var mainType = classes[2].Type;

            Assert.IsTrue(fooType.IsClass);
            Assert.IsFalse(fooType.IsAbstract);
            Assert.IsFalse(fooType.IsArray);
            Assert.IsFalse(fooType.IsGenericType);
            Assert.IsFalse(fooType.IsPrimitive);
            Assert.IsFalse(fooType.IsSealed);

            Assert.IsTrue(mainType.IsObject);
        }

        [TestMethod]
        public void TypeDefinitionForClass()
        {
            var t = new RType("Foo", RTypeAttributes.Class);
            Assert.IsTrue(t.IsClass);
            Assert.IsFalse(t.IsAbstract);
            Assert.IsFalse(t.IsArray);
            Assert.IsFalse(t.IsGenericType);
            Assert.IsFalse(t.IsPrimitive);
            Assert.IsFalse(t.IsSealed);
        }

        [TestMethod]
        public void TypeDefinitionForSealedClass()
        {
            var t = new RType("Foo", RTypeAttributes.Class | RTypeAttributes.Sealed);
            Assert.IsTrue(t.IsClass);
            Assert.IsTrue(t.IsSealed);
        }

        [TestMethod]
        public void TypeDefinitionForAbstractClass()
        {
            var t = new RType("Foo", RTypeAttributes.Class | RTypeAttributes.Abstract);
            Assert.IsTrue(t.IsClass);
            Assert.IsTrue(t.IsAbstract);
        }

        [TestMethod]
        public void InflateSimpleGeneric()
        {
            /*
            class Foo[A,B]{
                def get(x: A): B
            }
            */
            RType fooTy = new RType("Foo");
            RppGenericParameter[] gp = fooTy.DefineGenericParameters(new[] {"A", "B"});
            fooTy.DefineMethod("get", RMethodAttributes.Public, gp[0].Type, new[] {new RppParameterInfo(gp[0].Type), new RppParameterInfo(gp[1].Type)});

            // Foo[Int, Float]
            RType specializedFooTy = fooTy.MakeGenericType(new[] {RppTypeSystem.IntTy, RppTypeSystem.FloatTy});
            // get(x: Int) : Float
            RppMethodInfo getMethod = specializedFooTy.Methods[0];
            Assert.AreEqual(RppTypeSystem.IntTy, getMethod.ReturnType);
            Assert.AreEqual(RppTypeSystem.IntTy, getMethod.Parameters[0].Type);
            Assert.AreEqual(RppTypeSystem.FloatTy, getMethod.Parameters[1].Type);
        }

        [TestInitialize]
        public void SetUp()
        {
            _module = CreateModule();
        }

        [TestMethod]
        public void CreateSimpleNativeTypeFromRType()
        {
            RType simpleType = new RType("Foo", RTypeAttributes.Class | RTypeAttributes.Public);
            simpleType.InitializeNativeType(_module);
        }

        private static ModuleBuilder CreateModule()
        {
            var assemblyName = new AssemblyName("TestAssembly");
            var assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave);
            return assemblyBuilder.DefineDynamicModule("TestAssembly", "TestAssembly.dll");
        }
    }
}