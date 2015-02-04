using System;
using System.Reflection;
using System.Reflection.Emit;

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

        private class Foo
        {
            public Bar _child;
        }

        private class Bar
        {
            public Foo _parent;
        }

        private static void CreateObjects()
        {
            AssemblyName assemblyName = new AssemblyName("sampleAssembl");
            AssemblyBuilder assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name, assemblyName.Name + ".exe");

            TypeBuilder fooTypeBuilder = moduleBuilder.DefineType("Foo", TypeAttributes.Public);

            TypeBuilder barTypeBuilder = moduleBuilder.DefineType("Bar", TypeAttributes.Public);
            barTypeBuilder.DefineField("_parent", fooTypeBuilder, FieldAttributes.Public);

            fooTypeBuilder.DefineField("_child", barTypeBuilder, FieldAttributes.Public);

            MethodBuilder mainMethod = fooTypeBuilder.DefineMethod("Main", MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard,
                typeof (int),
                new[] {typeof (String[])});
            ILGenerator il = mainMethod.GetILGenerator();
            il.EmitWriteLine("Moikka");
            il.Emit(OpCodes.Ret);

            fooTypeBuilder.CreateType();
            barTypeBuilder.CreateType();

            assemblyBuilder.SetEntryPoint(mainMethod.GetBaseDefinition());
            assemblyBuilder.Save(assemblyName.Name + ".exe", PortableExecutableKinds.Required32Bit, ImageFileMachine.I386);
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
            /*
            int l = args.Length;
            String item = args[0];
            CreateObjects();
            CreateCall();
             */
            roundToEven();
        }

        private static void roundToEven()
        {
            double k1 = roundToEven(0);
            double k2 = roundToEven(1);
            double k3 = roundToEven(2);
            double k4 = roundToEven(3);
        }

        private static double roundToEven(double v)
        {
            double val = (int) Math.Round(v, MidpointRounding.ToEven);
            if (val % 2 != 0)
            {
                return val + 1;
            }

            return val;
        }

        private static void CreateCall()
        {
            Moika m = new Moika("Hello");
            m.println();
            Moika.printSomething("Moika");
            Moika p = new Moika("Wr");
            {
                Moika l = new Moika("adf");
            }
            {
                int l = 10;
            }
        }
    }
}