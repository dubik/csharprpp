﻿using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using CSharpRpp;
using CSharpRpp.Codegen;
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
            var creator = new CreateRType();
            program.Accept(creator);
            var classes = program.Classes.ToArray();
            var fooType = classes[0].Type2;
            var barType = classes[1].Type2;
            var mainType = classes[2].Type2;

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
        public void TestExtendGenericClass()
        {
            const string code = @"
class Foo[A]

class Bar extends Foo[Int]
";
            var program = Utils.Parse(code);
            var crea = new CreateRType();
            program.Accept(crea);

            // Analyze
        }

        [TestMethod]
        public void TypeCreation()
        {
            const string code = @"
class Foo
{
    def length(k: Int) : Int = 13
}
";
            RppProgram program = Utils.Parse(code);
            CreateRType creator = new CreateRType();
            program.Accept(creator);
            RppScope scope = new RppScope(null);
            program.PreAnalyze(scope);
            program.Analyze(scope);
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