using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using JetBrains.Annotations;

namespace CSharpRpp.Codegen
{
    public sealed class CodeGenerator
    {
        public string AssemblyName { get; set; }
        public Assembly Assembly => _assemblyBuilder;
        public ModuleBuilder Module { get; private set; }

        private readonly RppProgram _program;
        private readonly Dictionary<RppFunc, MethodBuilder> _funcBuilders;

        private AssemblyName _assemblyName;
        private AssemblyBuilder _assemblyBuilder;

        public CodeGenerator(RppProgram program, string assemblyName)
        {
            AssemblyName = Path.GetFileNameWithoutExtension(assemblyName);
            _program = program;

            _funcBuilders = new Dictionary<RppFunc, MethodBuilder>();

            CreateModule();
        }

        public void Generate()
        {
            GenerateMethodBodies();
        }

        private void GenerateMethodBodies()
        {
            ClrCodegen codegen = new ClrCodegen();
            _program.Accept(codegen);
        }

        private void CreateModule()
        {
            _assemblyName = new AssemblyName(AssemblyName);
            _assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(_assemblyName, AssemblyBuilderAccess.RunAndSave);
            Module = _assemblyBuilder.DefineDynamicModule(_assemblyName.Name, _assemblyName.Name + ".dll");
        }

        public void Save(string fullPath)
        {
            var mainFunc = FindMain();
            if (mainFunc != null)
            {
                MethodInfo wrappedMain = WrapMain(mainFunc);
                _assemblyBuilder.SetEntryPoint(wrappedMain, PEFileKinds.ConsoleApplication);
            }

            SaveAssembly(fullPath, _assemblyBuilder);
        }

        private static void SaveAssembly(string fullPath, AssemblyBuilder assemblyBuilder)
        {
            // assemblyBuilder.Save doesn't want to save with full path, so saving to current directory and then
            // moving file to final, weird
            string fileName = Path.GetFileName(fullPath);

            if (File.Exists(fileName))
            {
                File.Delete(fullPath);
            }

            assemblyBuilder.Save(fileName);

            if (fileName != fullPath)
            {
                string targetDirectory = Path.GetDirectoryName(fullPath);
                Debug.Assert(targetDirectory != null, "targetDirectory != null");
                Directory.CreateDirectory(targetDirectory);
                Debug.Assert(fileName != null, "fileName != null");

                if (!File.Exists(fullPath))
                {
                    File.Move(fileName, fullPath);
                }
            }
        }

        public bool HasMain()
        {
            return _funcBuilders.Values.Any(f => f.Name == "main");
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

            TypeBuilder wrappedMainType = Module.DefineType("<>RppApp");
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