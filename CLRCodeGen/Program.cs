using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using FieldAttributes = System.Reflection.FieldAttributes;
using MethodAttributes = System.Reflection.MethodAttributes;
using TypeAttributes = System.Reflection.TypeAttributes;

namespace CLRCodeGen
{
    internal class ICodegenContext
    {
    }

    internal class Node
    {
        public void Analyze()
        {
        }

        public void Codegen()
        {
        }
    }

    public class Foo
    {
        public int k;

        public virtual int func()
        {
            return 13;
        }
    }

    public class Bar : Foo
    {
        public override int func()
        {
            int k = 10;
            int p = k;

            return 27 + k + p;
        }
    }

    public interface Function<in T, out Res>
    {
        Res Apply(T p);
    }

    class MyFoo<A, C, P>
    {
        class MyBar<Z>
        {
            private C myField;

            public void func<B>(B someB)
            {
                var f = new Func<object, C>(b =>
                                            {
                                                Console.WriteLine(someB);
                                                return myField;
                                            });
            }
        }

        public static void Create()
        {
            MyBar<int> b = new MyBar<int>();
        }
    }

    internal class MyFooMain
    {
        public static void main()
        {
            MyFoo<int, float, float> a = new MyFoo<int, float, float>();
        }
    }


    /*
    class Foo[A]
    {
        def func(k: A) : Unit = {
            var f: (A) => Int = (x: A) => 10
        }
    }
    */

    public class MyMain<A>
    {
        private class Closure : Function<A, int>
        {
            public int Apply(A p)
            {
                throw new NotImplementedException();
            }
        }

        public void Create()
        {
            Function<A, int> k = new Closure();
        }
    }

    internal class Program
    {
        private static void DoSomething()
        {
            AssemblyName assemblyName = new AssemblyName("sampleAssembl");
            AssemblyBuilder assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name, assemblyName.Name + ".dll");

            TypeBuilder typeBuilder = moduleBuilder.DefineType("Program", TypeAttributes.Public);

            ConstructorBuilder constructorBuilder = typeBuilder.DefineDefaultConstructor(MethodAttributes.Public);
            Type ty = typeBuilder.CreateType();
            assemblyBuilder.Save(assemblyName.Name + ".dll");
        }

        private static void CreateExe()
        {
            AssemblyName assemblyName = new AssemblyName("sampleAssembl");
            AssemblyBuilder assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name, assemblyName.Name + ".exe");

            TypeBuilder typeBuilder = moduleBuilder.DefineType("Program", TypeAttributes.Public);

            MethodBuilder mainMethod = typeBuilder.DefineMethod("Main", MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard,
                typeof (int),
                new[] {typeof (String[])});
            ILGenerator il = mainMethod.GetILGenerator();
            il.UsingNamespace("System");
            il.EmitWriteLine("Moikka");
            il.Emit(OpCodes.Ret);

            typeBuilder.CreateType();

            assemblyBuilder.SetEntryPoint(mainMethod.GetBaseDefinition());
            assemblyBuilder.Save(assemblyName.Name + ".exe", PortableExecutableKinds.Required32Bit, ImageFileMachine.I386);
        }

        private static void CreateObjects()
        {
            /*
        private class Foo
        {
            public Bar _child;
        }

        private class Bar
        {
            public Foo _parent;

            public Foo Create()
            {
                return new Foo();
            }
        }
            */
            AssemblyName assemblyName = new AssemblyName("sampleAssembl");
            AssemblyBuilder assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name, assemblyName.Name + ".exe");

            TypeBuilder fooTypeBuilder = moduleBuilder.DefineType("Foo", TypeAttributes.Public);

            TypeBuilder barTypeBuilder = moduleBuilder.DefineType("Bar", TypeAttributes.Public);
            barTypeBuilder.DefineField("_parent", fooTypeBuilder, FieldAttributes.Public);

            fooTypeBuilder.DefineField("_child", barTypeBuilder, FieldAttributes.Public);

            var method = CreateMethod(fooTypeBuilder, barTypeBuilder);
            var mainMethod = CreateMainMethod(fooTypeBuilder);

            barTypeBuilder.CreateType();
            fooTypeBuilder.CreateType();

            GenCode(mainMethod, barTypeBuilder);

            assemblyBuilder.SetEntryPoint(mainMethod.GetBaseDefinition());
            assemblyBuilder.Save(assemblyName.Name + ".exe", PortableExecutableKinds.Required32Bit, ImageFileMachine.I386);
        }

        private static void CreateMethodSlowely()
        {
            AssemblyName assemblyName = new AssemblyName("methodSlowely");
            AssemblyBuilder assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name, assemblyName.Name + ".exe");

            TypeBuilder fooTypeBuilder = moduleBuilder.DefineType("Foo", TypeAttributes.Public);

            MethodBuilder methodBuilder = fooTypeBuilder.DefineMethod("MyMethod", MethodAttributes.Public, CallingConventions.Standard);
            methodBuilder.SetReturnType(typeof (int));
            methodBuilder.SetParameters(new[] {typeof (string)});

            methodBuilder.GetILGenerator().Emit(OpCodes.Ret);
            fooTypeBuilder.CreateType();
            assemblyBuilder.Save(assemblyName.Name + ".dll", PortableExecutableKinds.Required32Bit, ImageFileMachine.I386);
        }

        private static void CreateGenericClass()
        {
            AssemblyName assemblyName = new AssemblyName("genericClass");
            AssemblyBuilder assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name, assemblyName.Name + ".exe");

            // Stack[T]
            TypeBuilder clazz = moduleBuilder.DefineType("Stack", TypeAttributes.Public);
            var genericBuilder = clazz.DefineGenericParameters(new[] {"T"});
            var genericTType = genericBuilder[0].AsType();

            var retType = clazz.MakeGenericType(typeof (int));
            var method = clazz.DefineMethod("create", MethodAttributes.Static | MethodAttributes.Public, CallingConventions.Standard, retType, null);

            var sameMethod = clazz.DefineMethod("same", MethodAttributes.Static | MethodAttributes.Public);
            var sameMethod2 = clazz.DefineMethod("same", MethodAttributes.Static | MethodAttributes.Public);
            sameMethod.SetReturnType(typeof (void));
            sameMethod2.SetParameters(typeof (int));
            sameMethod2.SetReturnType(typeof (int));

            sameMethod.GetILGenerator().Emit(OpCodes.Ret);

            var gen2 = sameMethod2.GetILGenerator();
            gen2.Emit(OpCodes.Ldc_I4_0);
            gen2.Emit(OpCodes.Ret);

            var constr = clazz.DefineDefaultConstructor(MethodAttributes.Public);
            var intParamConstr = clazz.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new[] {genericTType});
            intParamConstr.GetILGenerator().Emit(OpCodes.Ret);

            var specConstr = TypeBuilder.GetConstructor(retType, intParamConstr);

            var gen = method.GetILGenerator();
            gen.Emit(OpCodes.Ldc_I4_3);
            gen.Emit(OpCodes.Newobj, specConstr);
            gen.Emit(OpCodes.Ret);

            clazz.CreateType();

            assemblyBuilder.Save(assemblyName.Name + ".dll", PortableExecutableKinds.Required32Bit, ImageFileMachine.I386);
        }

        private static void CreateGenericClosures()
        {
            AssemblyName assemblyName = new AssemblyName("genericClosure");
            AssemblyBuilder assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Save);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name, assemblyName.Name + ".dll");

            TypeBuilder main = moduleBuilder.DefineType("Main", TypeAttributes.Class | TypeAttributes.Public);

            Type t = typeof (Foo);

            // Defining generic param for Main class
            GenericTypeParameterBuilder[] generics = main.DefineGenericParameters("A");
            t = generics[0]; // [1] Uncomment to enable for nested class

            var iFunctionType = typeof (Function<,>).MakeGenericType(t, t);

            TypeBuilder closure = main.DefineNestedType("Closure", TypeAttributes.Class | TypeAttributes.NestedPrivate, typeof (object));
            closure.AddInterfaceImplementation(iFunctionType);
            GenericTypeParameterBuilder[] closureGenerics = closure.DefineGenericParameters("A");


            MethodBuilder applyMethod = closure.DefineMethod("Apply",
                MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.Public,
                CallingConventions.Standard);
            applyMethod.SetParameters(t);
            applyMethod.SetReturnType(t);

            ILGenerator body = applyMethod.GetILGenerator();
            body.Emit(OpCodes.Ldnull);
            body.Emit(OpCodes.Ret);

            closure.DefineDefaultConstructor(MethodAttributes.Public);

            closure.CreateType();
            main.CreateType();

            assemblyBuilder.Save(assemblyName.Name + ".dll", PortableExecutableKinds.Required32Bit, ImageFileMachine.I386);
        }

        private static void GenCode(MethodBuilder builder, Type objectToCreate)
        {
            ILGenerator il = builder.GetILGenerator();
            il.Emit(OpCodes.Newobj, objectToCreate.GetConstructors()[0]);
            il.Emit(OpCodes.Ret);
        }

        private static MethodBuilder CreateMethod(TypeBuilder builder, Type objectToCreate)
        {
            MethodBuilder method = builder.DefineMethod("Create", MethodAttributes.Public, CallingConventions.Standard, objectToCreate, new Type[] {});
            return method;
        }

        private static MethodBuilder CreateMainMethod(TypeBuilder fooTypeBuilder)
        {
            MethodBuilder mainMethod = fooTypeBuilder.DefineMethod("Main", MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard,
                typeof (int),
                new[] {typeof (String[])});
            ILGenerator il = mainMethod.GetILGenerator();
            il.EmitWriteLine("Moikka");
            il.Emit(OpCodes.Ret);
            return mainMethod;
        }

        private static void CreateGenericType()
        {
            var typeofListOfInts = typeof (IList<int>);
            var typeofList = typeof (IList<>);
            var typeofListOfInt = typeofList.MakeGenericType(new Type[] {typeof (int)});
            bool ass = typeofListOfInt.IsAssignableFrom(typeofListOfInts);
        }

        private static void Main(String[] args)
        {
            CreateGenericClass();
            CecilCodeGen.DoSomething();
            CreateGenericClosures();
        }
    }
}