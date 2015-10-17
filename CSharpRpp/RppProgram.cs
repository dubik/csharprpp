using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using JetBrains.Annotations;
using RppRuntime;

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

        public IEnumerable<RppClass> Classes => _classes.AsEnumerable();

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)] private IList<RppClass> _classes = new List<RppClass>();
        private readonly CodegenContext _context = new CodegenContext();

        public void Add(RppClass clazz)
        {
            _classes.Add(clazz);
        }

        public void PreAnalyze(RppScope scope)
        {
            _context.CreateAssembly(Name);
            _context.CreateModuleBuilder();

            BootstrapRuntime(scope);

            _classes.ForEach(c => scope.Add(c.Type2));

            NodeUtils.PreAnalyze(scope, _classes);
        }

        public override void Accept(IRppNodeVisitor visitor)
        {
            visitor.Visit(this);
            _classes.ForEach(clazz => clazz.Accept(visitor));
        }

        private void BootstrapRuntime([NotNull] RppScope scope)
        {
            foreach (MethodInfo methodInfo in typeof (Runtime).GetMethods(BindingFlags.Static))
            {
                methodInfo.GetBaseDefinition();
            }
        }

        public override IRppNode Analyze(RppScope scope)
        {
            _classes = NodeUtils.Analyze(scope, _classes);
            return this;
        }
    }
}