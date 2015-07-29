using System;
using System.Reflection;
using System.Reflection.Emit;

namespace CLRCodeGen
{
    class ICodegenContext
    {
    }

    class Node
    {
        public void Analyze()
        {
        }

        public void Codegen()
        {
        }
    }

    class Program
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

            TypeBuilder stackTypeBuilder = moduleBuilder.DefineType("Stack", TypeAttributes.Public);
            var genericBuilder = stackTypeBuilder.DefineGenericParameters(new[] {"T"});
            //genericBuilder[0].SetGenericParameterAttributes();
            stackTypeBuilder.CreateType();


            assemblyBuilder.Save(assemblyName.Name + ".dll", PortableExecutableKinds.Required32Bit, ImageFileMachine.I386);
        }

        private static void GenCode(MethodBuilder builder, Type objectToCreate)
        {
            ILGenerator il = builder.GetILGenerator();
            il.Emit(OpCodes.Newobj, objectToCreate.GetConstructor(new Type[] {}));
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

        public static void Print()
        {
            Console.WriteLine("Moika");

            string res = "hello";
            Console.WriteLine(res);
        }

        private class Moika
        {
            private string some;

            public Moika(string some)
            {
                this.some = some;
            }

            public void println()
            {
                Console.WriteLine(some);
            }

            public static void printSomething(string some)
            {
                Console.WriteLine(some);
            }
        }

        private static void Main(String[] args)
        {
            CreateMethodSlowely();
        }
    }
}