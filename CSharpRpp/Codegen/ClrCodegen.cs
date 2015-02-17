using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using JetBrains.Annotations;

namespace CSharpRpp.Codegen
{
    internal class CodeGenerator
    {
        private readonly RppProgram _program;
        private readonly Dictionary<RppClass, TypeBuilder> _typeBuilders;
        private Dictionary<RppFunc, MethodBuilder> _funcBuilders;
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

            StubsCreator stubsCreator = new StubsCreator(_typeBuilders);
            _program.Accept(stubsCreator);
        }

        public void Generate()
        {
            ConstructorGenerator.GenerateConstructors(_typeBuilders);
            ClrCodegen codegen = new ClrCodegen();
        }

        private void CreateModule()
        {
            _assemblyName = new AssemblyName(_program.Name);
            _assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(_assemblyName, AssemblyBuilderAccess.Save);
            _moduleBuilder = _assemblyBuilder.DefineDynamicModule(_program.Name, _program.Name + ".exe");
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
            return _funcBuilders.Values.First(func => func.Name == "main").GetBaseDefinition();
        }
    }

    internal class TypeCreator : RppNodeVisitor
    {
        private readonly ModuleBuilder _module;
        private readonly Dictionary<RppClass, TypeBuilder> _typeBuilders;

        public TypeCreator([NotNull] ModuleBuilder module, [NotNull] Dictionary<RppClass, TypeBuilder> typeBuilders)
        {
            _module = module;
            _typeBuilders = typeBuilders;
        }

        public override void VisitEnter(RppClass node)
        {
            TypeBuilder classType = _module.DefineType(node.Name);
            _typeBuilders.Add(node, classType);
            node.RuntimeType = classType;
        }
    }

    internal class StubsCreator : RppNodeVisitor
    {
        private RppClass _class;
        private Dictionary<RppFunc, MethodBuilder> _funcBuilders;

        public StubsCreator(Dictionary<RppFunc, MethodBuilder> funcBuilders)
        {
            _funcBuilders = funcBuilders;
        }

        public override void VisitEnter(RppClass node)
        {
            _class = node;
        }

        public override void VisitExit(RppClass node)
        {
            _class = null;
        }

        public override void VisitEnter(RppFunc node)
        {
            TypeBuilder builder = _class.RuntimeType as TypeBuilder;
            Debug.Assert(builder != null, "builder != null");

            MethodAttributes attrs = MethodAttributes.Private;

            if (node.IsPublic)
            {
                attrs = MethodAttributes.Public;
            }

            if (node.IsStatic)
            {
                attrs |= MethodAttributes.Static;
            }

            MethodBuilder methodBuilder = builder.DefineMethod(node.Name, attrs);
            node.Builder = methodBuilder;
            _funcBuilders.Add(node, methodBuilder);
        }
    }

    internal class ConstructorGenerator
    {
        public static void GenerateConstructors(IEnumerable<KeyValuePair<RppClass, TypeBuilder>> classes)
        {
            ClrCodegen codegen = new ClrCodegen();
            foreach (var pair in classes)
            {
                RppClass clazz = pair.Key;
                TypeBuilder typeBuilder = pair.Value;
                ConstructorBuilder builder = GenerateConstructor(typeBuilder);
                ILGenerator body = builder.GetILGenerator();
            }
        }

        private static ConstructorBuilder GenerateConstructor(TypeBuilder typeBuilder)
        {
            ConstructorBuilder constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, Type.EmptyTypes);
            return constructorBuilder;
        }
    }

    internal class ClrCodegen : RppNodeVisitor
    {
        private readonly Dictionary<string, TypeBuilder> _typeMap = new Dictionary<string, TypeBuilder>();

        private TypeBuilder _currentClass;
        private RppClass _currentRppClass;
        private ILGenerator _il;
        private MethodInfo _mainFunc;

        public override void VisitEnter(RppClass node)
        {
            Console.WriteLine("Genering class: " + node.Name);
            _currentRppClass = node;
            _currentClass = node.RuntimeType as TypeBuilder;
            _typeMap.Add(node.Name, _currentClass);
        }

        public override void VisitExit(RppClass node)
        {
            var t = _currentClass.CreateType();
            Console.WriteLine("Generated class");
        }

        public override void VisitEnter(RppFunc node)
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

        public override void VisitExit(RppFunc node)
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
            _il.Emit(OpCodes.Call, node.Function.RuntimeType);
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
            // ConstructorInfo constructorInfo = node.RefClass.RuntimeType.GetConstructor(Type.EmptyTypes);
            // node.RefClass.GetConstructor();
            // Debug.Assert(constructorInfo != null, "constructorInfo != null");
            // _il.Emit(OpCodes.Newobj, constructorInfo);
        }

        public void Visit(RppAssignOp node)
        {
        }
    }
}