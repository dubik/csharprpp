using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using CSharpRpp.Codegen;
using CSharpRpp.Reporting;
using CSharpRpp.Symbols;
using CSharpRpp.TypeSystem;
using NUnit.Framework;
using static CSharpRpp.ListExtensions;
using static CSharpRpp.TypeSystem.RppTypeSystem;

namespace CSharpRppTest
{
    [TestFixture]
    public class TypeSystemTest
    {
        private ModuleBuilder _module;

        [Test]
        public void PrimitiveTypeEquality()
        {
            var t = new RType("Int");
            var t1 = new RType("Int");
            Assert.AreEqual(t, t1);
        }

        [Test]
        public void TestVisitors()
        {
            const string code = @"
class Foo
class Bar extends Foo
object Main
";
            var program = Utils.Parse(code);

            SymbolTable scope = new SymbolTable();
            PopulateBuiltinTypes(scope);

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

        [Test]
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

        [Test]
        public void TypeDefinitionForSealedClass()
        {
            var t = new RType("Foo", RTypeAttributes.Class | RTypeAttributes.Sealed);
            Assert.IsTrue(t.IsClass);
            Assert.IsTrue(t.IsSealed);
        }

        [Test]
        public void TypeDefinitionForAbstractClass()
        {
            var t = new RType("Foo", RTypeAttributes.Class | RTypeAttributes.Abstract);
            Assert.IsTrue(t.IsClass);
            Assert.IsTrue(t.IsAbstract);
        }

        [Test]
        public void InflateSimpleGeneric()
        {
            /*
            class Foo[A,B]{
                def get(x: A, y: B): B
            }
            */
            RType fooTy = new RType("Foo");
            RppGenericParameter[] gp = fooTy.DefineGenericParameters("A", "B");
            fooTy.DefineMethod("get", RMethodAttributes.Public, gp[0].Type, new[] {new RppParameterInfo(gp[0].Type), new RppParameterInfo(gp[1].Type)});

            // Foo[Int, Float]
            RType specializedFooTy = fooTy.MakeGenericType(IntTy, FloatTy);
            // get(x: Int) : Float
            RppMethodInfo getMethod = specializedFooTy.Methods[0];
            Assert.AreEqual(IntTy, getMethod.ReturnType);
            Assert.AreEqual(IntTy, getMethod.Parameters[0].Type);
            Assert.AreEqual(FloatTy, getMethod.Parameters[1].Type);
        }

        [Test]
        public void InflateClassWithGenericBaseType()
        {
            /*
                class Foo[A, B] {
                    def get(x: A) : B
                }
                class Bar[A, B, C] extends Foo[A, B]
                {
                    def map(x: A, y: B) : C
                }
            */
            RType fooTy = new RType("Foo");
            {
                RppGenericParameter[] gp = fooTy.DefineGenericParameters("X", "Y");
                fooTy.DefineMethod("get", RMethodAttributes.Public, gp[1].Type, new[] {new RppParameterInfo(gp[0].Type)});
            }

            RType barTy = new RType("Bar");
            RppGenericParameter[] barGp = barTy.DefineGenericParameters("A", "B", "C");
            barTy.DefineMethod("map", RMethodAttributes.Public, barGp[2].Type, new[]
            {
                new RppParameterInfo(barGp[0].Type),
                new RppParameterInfo(barGp[1].Type)
            });
            barTy.BaseType = fooTy.MakeGenericType(barGp[0].Type, barGp[1].Type);

            RType specilizedBarTy = barTy.MakeGenericType(IntTy, FloatTy, StringTy);
            Assert.IsNotNull(specilizedBarTy.BaseType);
            IReadOnlyCollection<RType> barGenericArguments = specilizedBarTy.GenericArguments;
            CollectionAssert.AreEqual(new[] {IntTy, FloatTy, StringTy}, barGenericArguments.ToList());
            var fooGenericArguments = specilizedBarTy.BaseType.GenericArguments;
            CollectionAssert.AreEqual(new[] {IntTy, FloatTy}, fooGenericArguments.ToList());
            RppMethodInfo getMethod = specilizedBarTy.BaseType.Methods[0];
            Assert.AreEqual(FloatTy, getMethod.ReturnType);
            Assert.AreEqual(IntTy, getMethod.Parameters[0].Type);
        }

        [SetUp]
        public void SetUp()
        {
            _module = CreateModule();
        }

        [Test]
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

        [Test]
        public void LinearizeSimpleClass()
        {
            RType fooTy = new RType("Foo");
            List<RType> hierarchy = fooTy.LinearizeHierarchy().ToList();
            CollectionAssert.AreEqual(List(fooTy, AnyTy), hierarchy);
        }

        [Test]
        public void LinearizeSimpleInterface()
        {
            RType fooTy = new RType("IFoo", RTypeAttributes.Interface);
            List<RType> hierarchy = fooTy.LinearizeHierarchy().ToList();
            CollectionAssert.AreEqual(List(fooTy), hierarchy);
        }

        [Test]
        public void OnlyOneInstanceOfTypeIsCreated()
        {
            SymbolTable scope = new SymbolTable();
            PopulateBuiltinTypes(scope);

            RType fooTy = GetOrCreateType("Foo");
            RType otherFooTy = GetOrCreateType("Foo");
            Assert.IsTrue(ReferenceEquals(fooTy, otherFooTy));
        }
    }
}