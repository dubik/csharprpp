using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace CSharpRpp
{
    public class CodegenContext
    {
        public AssemblyName AssemblyName;
        public AssemblyBuilder AssemblyBuilder { get; private set; }
        public ModuleBuilder ModuleBuilder { get; private set; }

        public void CreateAssembly(string name)
        {
            
            AssemblyName = new AssemblyName(name);
            AssemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Save);
        }

        public void CreateModuleBuilder()
        {
            ModuleBuilder = AssemblyBuilder.DefineDynamicModule(AssemblyName.Name, ssemblyName.Name + ".exe");
        }
    }

    public class RppProgram : RppNode
    {
        private IList<RppClass> _classes = new List<RppClass>();

        public void Add(RppClass clazz)
        {
            _classes.Add(clazz);
        }

        public override void PreAnalyze(RppScope scope)
        {
            NodeUtils.PreAnalyze(scope, _classes);
        }

        public override IRppNode Analyze(RppScope scope)
        {
            _classes = NodeUtils.Analyze(scope, _classes);
            return this;
        }

        public override void Codegen(CodegenContext ctx)
        {
            

            ctx.ModuleBuilder = moduleBuilder;

            _classes.ForEach(clazz => clazz.Codegen(ctx));
        }
    }
}