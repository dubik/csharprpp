using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using JetBrains.Annotations;

namespace CSharpRpp.Codegen
{
    class ClrCodegen : IRppNodeVisitor
    {
        private AssemblyName _assemblyName;
        private AssemblyBuilder _assemblyBuilder;
        private ModuleBuilder _moduleBuilder;
        private readonly Dictionary<string, TypeBuilder> _typeMap = new Dictionary<string, TypeBuilder>();

        private TypeBuilder _currentClass;
        private RppClass _currentRppClass;
        private ILGenerator _currentGenerator;
        private RppFunc _mainFunc;

        public void Visit(RppProgram node)
        {
            _assemblyName = new AssemblyName(node.Name);
            _assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(_assemblyName, AssemblyBuilderAccess.Save);
            _moduleBuilder = _assemblyBuilder.DefineDynamicModule(node.Name, node.Name + ".exe");

            Console.WriteLine("RppProgram: " + node.Name);
        }

        public void VisitEnter(RppClass node)
        {
            Console.WriteLine("Genering class: " + node.Name);
            _currentRppClass = node;
            _currentClass = _moduleBuilder.DefineType(node.Name);
            _typeMap.Add(node.Name, _currentClass);
        }

        public void VisitExit(RppClass node)
        {
            var t = _currentClass.CreateType();
            Console.WriteLine("Generated class");
        }

        public void VisitEnter(RppFunc node)
        {
            Console.WriteLine("Generating func: " + node.Name);

            if (node.Name == "main")
            {
                _mainFunc = node;
            }

            MethodAttributes attrs = MethodAttributes.Private;

            if (node.IsPublic)
            {
                attrs = MethodAttributes.Public;
            }

            if (node.IsStatic)
            {
                attrs |= MethodAttributes.Static;
            }

            MethodBuilder builder = _currentClass.DefineMethod(node.Name, attrs);
            builder.SetReturnType(node.RuntimeReturnType);

            CodegenParams(node.Params, builder);

            _currentGenerator = builder.GetILGenerator();
        }

        private static void CodegenParams([NotNull] IEnumerable<IRppParam> paramList, [NotNull] MethodBuilder methodBuilder)
        {
            Type[] parameterTypes = paramList.Select(param => param.RuntimeType).ToArray();
            methodBuilder.SetParameters(parameterTypes);
        }

        public void VisitExit(RppFunc node)
        {
            GenerateRet(node, _currentGenerator);

            Console.WriteLine("Func generated");
        }

        private static void GenerateRet([NotNull] RppFunc node, [NotNull] ILGenerator generator)
        {
            generator.Emit(OpCodes.Ldc_I4, 10);

            if (node.RuntimeReturnType == typeof (void) && node.Expr.RuntimeType != typeof (void))
            {
                generator.Emit(OpCodes.Pop);
            }

            generator.Emit(OpCodes.Ret);
        }


        public void Visit(RppVar node)
        {
            throw new NotImplementedException();
        }

        public void Visit(RppBlockExpr node)
        {
            Console.WriteLine("Block expr");
        }

        public void Visit(BinOp node)
        {
            throw new System.NotImplementedException();
        }

        public void Visit(RppInteger node)
        {
            throw new System.NotImplementedException();
        }

        public void Visit(RppString node)
        {
            throw new System.NotImplementedException();
        }

        public void Visit(RppFuncCall node)
        {
            throw new System.NotImplementedException();
        }

        public void Visit(RppSelector node)
        {
            throw new System.NotImplementedException();
        }

        public void Visit(RppId node)
        {
            throw new System.NotImplementedException();
        }

        public void Save()
        {
            IRppFunc func = _mainFunc;
            if (func != null)
            {
                _assemblyBuilder.SetEntryPoint(func.RuntimeFuncInfo, PEFileKinds.ConsoleApplication);
                _assemblyBuilder.Save(_assemblyName.Name + ".exe");
            }
            else
            {
                _assemblyBuilder.Save(_assemblyName.Name + ".dll");
            }
        }
    }
}