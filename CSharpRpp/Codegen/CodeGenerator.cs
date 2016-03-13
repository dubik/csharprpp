using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using CSharpRpp.TypeSystem;
using JetBrains.Annotations;

namespace CSharpRpp.Codegen
{
    public sealed class CodeGenerator
    {
        public string AssemblyName { get; set; }
        public Assembly Assembly => _assemblyBuilder;
        public ModuleBuilder Module { get; private set; }

        private readonly RppProgram _program;

        private AssemblyName _assemblyName;
        private AssemblyBuilder _assemblyBuilder;

        public MethodInfo MainFunc;
        private readonly string _fileName;

        public CodeGenerator(RppProgram program, string fileName)
        {
            _fileName = fileName;
            AssemblyName = Path.GetFileNameWithoutExtension(fileName);
            _program = program;

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

            MainFunc = FindMain();
        }

        private void CreateModule()
        {
            _assemblyName = new AssemblyName(AssemblyName);
            _assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(_assemblyName, AssemblyBuilderAccess.RunAndSave);
            Module = _assemblyBuilder.DefineDynamicModule(_assemblyName.Name, _fileName, true);
        }

        public void Save()
        {
            if (MainFunc != null)
            {
                MethodInfo wrappedMain = WrapMain(MainFunc);
                _assemblyBuilder.SetEntryPoint(wrappedMain, PEFileKinds.ConsoleApplication);
            }

            SaveAssembly(_fileName, _assemblyBuilder);
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
            return MainFunc != null;
        }

        private class MainFunctionSearcher : RppNodeVisitor
        {
            public readonly List<RppFunc> MainFunctions = new List<RppFunc>();

            public override void VisitEnter(RppFunc node)
            {
                if (node.Name == "main" && node.IsStatic)
                {
                    MainFunctions.Add(node);
                }
            }
        }

        [CanBeNull]
        private MethodInfo FindMain()
        {
            MainFunctionSearcher mainFunctionSearcher = new MainFunctionSearcher();
            _program.Accept(mainFunctionSearcher);

            RppMethodInfo methodInfo = mainFunctionSearcher.MainFunctions.Select(f => f.MethodInfo).FirstOrDefault(func => func.Name == "main");
            MethodBuilder mainFunc = (MethodBuilder) methodInfo?.Native;
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