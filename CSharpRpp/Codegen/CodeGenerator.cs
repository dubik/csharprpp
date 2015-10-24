using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using JetBrains.Annotations;

namespace CSharpRpp.Codegen
{
    public sealed class CodeGenerator
    {
        public Assembly Assembly => _assemblyBuilder;
        public ModuleBuilder Module => _moduleBuilder;

        private readonly RppProgram _program;
        private readonly Dictionary<RppClass, TypeBuilder> _typeBuilders;
        private readonly Dictionary<RppFunc, MethodBuilder> _funcBuilders;

        private AssemblyName _assemblyName;
        private AssemblyBuilder _assemblyBuilder;
        private ModuleBuilder _moduleBuilder;

        public CodeGenerator(RppProgram program)
        {
            _program = program;

            _typeBuilders = new Dictionary<RppClass, TypeBuilder>();
            _funcBuilders = new Dictionary<RppFunc, MethodBuilder>();

            CreateModule();
        }

        public void PreGenerate()
        {
            //TypeCreator creatorCreator = new TypeCreator(_moduleBuilder, _typeBuilders);
            //_program.Accept(creatorCreator);
        }

        public void Generate()
        {
            // Setup parent classes
            //InheritanceConfigurator configurator = new InheritanceConfigurator();
            //_program.Accept(configurator);

            // GenerateMethodStubs();

            //ConstructorGenerator.GenerateFields(_typeBuilders);
            //ConstructorGenerator.GenerateConstructors(_typeBuilders);

            GenerateMethodBodies();
        }

        private void GenerateMethodBodies()
        {
            ClrCodegen codegen = new ClrCodegen();
            _program.Accept(codegen);
        }

        private void GenerateMethodStubs()
        {
            StubsCreator stubsCreator = new StubsCreator(_funcBuilders);
            _program.Accept(stubsCreator);
        }

        private void CreateModule()
        {
            _assemblyName = new AssemblyName(_program.Name);
            _assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(_assemblyName, AssemblyBuilderAccess.RunAndSave);
            _moduleBuilder = _assemblyBuilder.DefineDynamicModule(_program.Name, _program.Name + ".dll");
        }

        public void Save()
        {
            var mainFunc = FindMain();
            if (mainFunc != null)
            {
                MethodInfo wrappedMain = WrapMain(mainFunc);
                _assemblyBuilder.SetEntryPoint(wrappedMain, PEFileKinds.ConsoleApplication);
                _assemblyBuilder.Save(_assemblyName.Name + ".exe");
            }
            else
            {
                _assemblyBuilder.Save(_assemblyName.Name + ".dll");
            }
        }

        private MethodInfo FindMain()
        {
            MethodBuilder mainFunc = _funcBuilders.Values.FirstOrDefault(func => func.Name == "main");
            return mainFunc?.GetBaseDefinition();
        }

        private MethodInfo WrapMain(MethodInfo mainFunc)
        {
            ValidateMainFunc(mainFunc);
            return GeneratedWrappedMainCode(mainFunc);
        }

        /// <summary>
        /// Creates type RppApp with one static method which calls app's main method.
        /// In Rpp there are actually no static methods so we need to generate one
        /// for an entry point.
        /// </summary>
        /// <param name="targetMainFunc">app's main method</param>
        /// <returns>static method which delegates calls to app's main method</returns>
        [NotNull]
        private MethodBuilder GeneratedWrappedMainCode(MethodInfo targetMainFunc)
        {
            Type declaringType = targetMainFunc.DeclaringType;
            Debug.Assert(declaringType != null, "declaringType != null");
            FieldInfo instanceField = declaringType.GetField("_instance");

            TypeBuilder wrappedMainType = _moduleBuilder.DefineType("<>RppApp");
            MethodBuilder wrappedMain = wrappedMainType.DefineMethod("Main", MethodAttributes.Static | MethodAttributes.Public, typeof (int),
                new[] {typeof (string[])});
            wrappedMain.DefineParameter(1, ParameterAttributes.None, "args");
            var body = wrappedMain.GetILGenerator();

            body.Emit(OpCodes.Ldsfld, instanceField);

            ParameterInfo[] parameters = targetMainFunc.GetParameters();
            if (parameters.Any())
            {
                body.Emit(OpCodes.Ldarg_0);
            }

            body.Emit(OpCodes.Callvirt, targetMainFunc);

            if (targetMainFunc.ReturnType == typeof (void))
            {
                body.Emit(OpCodes.Ldc_I4_0);
            }

            body.Emit(OpCodes.Ret);
            wrappedMainType.CreateType();
            return wrappedMain;
        }

        /// <summary>
        /// Main method should have Unit or Int return type and shouldn't have parameters or
        /// it should be just one - array of strings.
        /// </summary>
        /// <param name="mainFunc">app's main function to validate</param>
        private static void ValidateMainFunc([NotNull] MethodInfo mainFunc)
        {
            if (mainFunc.ReturnType != typeof (void) && mainFunc.ReturnType != typeof (int))
            {
                throw new Exception("Main doesn't match signature of entry point");
            }

            var parameters = mainFunc.GetParameters();
            if (parameters.Length != 0 && parameters.Length != 1)
            {
                throw new Exception("Main doesn't match signature of entry point");
            }

            if (parameters.Length == 1 && parameters[0].ParameterType != typeof (string[]))
            {
                throw new Exception("First parameter of main should be Int");
            }
        }
    }
}