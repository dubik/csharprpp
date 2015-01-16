﻿using System;
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
            AssemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(AssemblyName, AssemblyBuilderAccess.Save);
        }

        public void CreateModuleBuilder()
        {
            ModuleBuilder = AssemblyBuilder.DefineDynamicModule(AssemblyName.Name, AssemblyName.Name + ".exe");
        }
    }

    public class RppProgram : RppNode, IMethodProvider
    {
        private IList<RppClass> _classes = new List<RppClass>();
        private readonly CodegenContext context = new CodegenContext();

        public void Add(RppClass clazz)
        {
            _classes.Add(clazz);
        }

        public override void PreAnalyze(RppScope scope)
        {
            context.CreateAssembly("sampleAssembly");
            context.CreateModuleBuilder();

            NodeUtils.PreAnalyze(scope, _classes);
        }

        public override IRppNode Analyze(RppScope scope)
        {
            _classes = NodeUtils.Analyze(scope, _classes);
            return this;
        }

        public void CodegenType(RppScope scope)
        {
            _classes.ForEach(clazz => clazz.CodegenType(scope, context.ModuleBuilder));
        }

        public void CodegenMethodStubs(CodegenContext ctx)
        {
        }

        public override void Codegen(CodegenContext ctx)
        {
            _classes.ForEach(clazz => clazz.Codegen(ctx));
        }
    }
}