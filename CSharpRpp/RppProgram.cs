using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            AssemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(AssemblyName, AssemblyBuilderAccess.Save);
        }

        public void CreateModuleBuilder()
        {
            ModuleBuilder = AssemblyBuilder.DefineDynamicModule(AssemblyName.Name, AssemblyName.Name + ".exe");
        }
    }

    [DebuggerDisplay("Name = {Name}, Classes = {_classes.Count}")]
    public class RppProgram : RppNode
    {
        public string Name { get; set; }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)] private IList<RppClass> _classes = new List<RppClass>();
        private readonly CodegenContext _context = new CodegenContext();

        public void Add(RppClass clazz)
        {
            _classes.Add(clazz);
        }

        public override void PreAnalyze(RppScope scope)
        {
            _context.CreateAssembly(Name);
            _context.CreateModuleBuilder();

            NodeUtils.PreAnalyze(scope, _classes);
        }

        public override IRppNode Analyze(RppScope scope)
        {
            _classes = NodeUtils.Analyze(scope, _classes);
            return this;
        }

        public void CodegenType(RppScope scope)
        {
            _classes.ForEach(clazz => clazz.CodegenType(scope, _context.ModuleBuilder));
        }

        public void CodegenMethodStubs(RppScope scope, CodegenContext ctx)
        {
            _classes.ForEach(clazz => clazz.CodegenMethodStubs(scope));
        }

        public void Codegen(CodegenContext ctx)
        {
            _classes.ForEach(clazz => clazz.Codegen(ctx));
        }

        public void Save()
        {
            _context.AssemblyBuilder.Save(Name + ".exe");
        }
    }
}