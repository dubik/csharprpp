using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using JetBrains.Annotations;

namespace CSharpRpp.Codegen
{
    class CodeGenerator
    {
        public static void Generate()
        {

            //TypeCreator creator = new TypeCreator();
            ClrCodegen codegen = new ClrCodegen();
        }
    }

    class TypeCreator : RppNodeVisitor
    {
        private ModuleBuilder _module;
        private Dictionary<RppClass, TypeBuilder> _typeBuilders;

        public TypeCreator([NotNull] ModuleBuilder module, [NotNull] Dictionary<RppClass, TypeBuilder> typeBuilders)
        {
            _module = module;
            _typeBuilders = typeBuilders;
        }

        public override void VisitEnter(RppClass node)
        {
            _typeBuilders.Add(node, _module.DefineType(node.Name));
            
        }
    }

    class ClrCodegen : IRppNodeVisitor
    {
        private AssemblyName _assemblyName;
        private AssemblyBuilder _assemblyBuilder;
        private ModuleBuilder _moduleBuilder;
        private readonly Dictionary<string, TypeBuilder> _typeMap = new Dictionary<string, TypeBuilder>();

        private TypeBuilder _currentClass;
        private RppClass _currentRppClass;
        private ILGenerator _il;
        private MethodInfo _mainFunc;

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

            _il = builder.GetILGenerator();

            if (node.Name == "main")
            {
                _mainFunc = builder.GetBaseDefinition();
            }
        }

        private static void CodegenParams([NotNull] IEnumerable<IRppParam> paramList, [NotNull] MethodBuilder methodBuilder)
        {
            Type[] parameterTypes = paramList.Select(param => param.RuntimeType).ToArray();
            methodBuilder.SetParameters(parameterTypes);
        }

        public void VisitExit(RppFunc node)
        {
            GenerateRet(node, _il);


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
            LocalBuilder localVar = _il.DeclareLocal(node.RuntimeType);

            if (!(node.InitExpr is RppEmptyExpr))
            {
                _il.Emit(OpCodes.Stloc, localVar);
            }
        }

        public void VisitEnter(RppBlockExpr node)
        {
            Console.WriteLine("Block expr");
        }

        public void VisitExit(RppBlockExpr node)
        {
        }

        private readonly Dictionary<string, OpCode> OpToIL = new Dictionary<string, OpCode>
        {
            {"+", OpCodes.Add},
            {"-", OpCodes.Sub},
            {"*", OpCodes.Mul},
            {"/", OpCodes.Div}
        };

        public void Visit(BinOp node)
        {
            OpCode opCode;
            if (OpToIL.TryGetValue(node.Op, out opCode))
            {
                _il.Emit(opCode);
            }
            else
            {
                throw new Exception("Can't generate code for: " + node.Op);
            }
        }

        public void Visit(RppInteger node)
        {
            _il.Emit(OpCodes.Ldc_I4, node.Value);
        }

        public void Visit(RppString node)
        {
            _il.Emit(OpCodes.Ldstr, node.Value);
        }

        public void Visit(RppFuncCall node)
        {
            // TODO we should keep references to functions by making another pass of code gen before
            // real code generation
            _il.Emit(OpCodes.Call, node.Function.RuntimeFuncInfo);
        }

        public void Visit(RppSelector node)
        {
            throw new NotImplementedException();
        }

        public void Visit(RppId node)
        {
            node.Ref.Accept(this);
        }

        public void Visit(RppParam node)
        {
            _il.Emit(OpCodes.Ldarg, node.Index);
        }

        public void Visit(RppNew node)
        {
            ConstructorInfo constructorInfo = node.RefClass.RuntimeType.GetConstructor(Type.EmptyTypes);
            Debug.Assert(constructorInfo != null, "constructorInfo != null");
            _il.Emit(OpCodes.Newobj, constructorInfo);
        }

        public void Save()
        {
            if (_mainFunc != null)
            {
                _assemblyBuilder.SetEntryPoint(_mainFunc, PEFileKinds.ConsoleApplication);
                _assemblyBuilder.Save(_assemblyName.Name + ".exe");
            }
            else
            {
                _assemblyBuilder.Save(_assemblyName.Name + ".dll");
            }
        }
    }
}