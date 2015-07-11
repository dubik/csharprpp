using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace CSharpRpp.Codegen
{
    public sealed class CodeGenerator
    {
        public Assembly Assembly
        {
            get { return _assemblyBuilder; }
        }

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
            TypeCreator creatorCreator = new TypeCreator(_moduleBuilder, _typeBuilders);
            _program.Accept(creatorCreator);
            // Setup parent classes
            InheritanceConfigurator configurator = new InheritanceConfigurator();
            _program.Accept(configurator);
        }

        public void Generate()
        {
            GenerateMethodStubs();

            ConstructorGenerator.GenerateFields(_typeBuilders);
            ConstructorGenerator.GenerateConstructors(_typeBuilders);

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
                _assemblyBuilder.SetEntryPoint(mainFunc, PEFileKinds.ConsoleApplication);
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
            if (mainFunc != null)
            {
                return mainFunc.GetBaseDefinition();
            }

            return null;
        }
    }
}