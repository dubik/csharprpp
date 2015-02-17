using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using JetBrains.Annotations;

namespace CSharpRpp.Codegen
{
    class ClrCodegen : RppNodeVisitor
    {
        private ILGenerator _body;

        public ClrCodegen()
        {
        }

        /// <summary>
        /// Creates ClrCodegen with specific ILGenerator in case code is needed to be inserted into
        /// specific function.
        /// </summary>
        /// <param name="body"></param>
        public ClrCodegen(ILGenerator body)
        {
            _body = body;
        }

        public override void VisitEnter(RppClass node)
        {
            Console.WriteLine("Genering class: " + node.Name);
        }

        public override void VisitExit(RppClass node)
        {
            var clazz = node.RuntimeType as TypeBuilder;
            Debug.Assert(clazz != null, "clazz != null");
            clazz.CreateType();
            Console.WriteLine("Generated class");
        }

        public override void VisitEnter(RppFunc node)
        {
            Console.WriteLine("Generating func: " + node.Name);

            MethodBuilder builder = node.Builder;
            Debug.Assert(builder != null, "builder != null");
            builder.SetReturnType(node.RuntimeReturnType);

            CodegenParams(node.Params, builder);

            _body = builder.GetILGenerator();
        }

        private static void CodegenParams([NotNull] IEnumerable<IRppParam> paramList, [NotNull] MethodBuilder methodBuilder)
        {
            Type[] parameterTypes = paramList.Select(param => param.RuntimeType).ToArray();
            methodBuilder.SetParameters(parameterTypes);
        }

        public override void VisitExit(RppFunc node)
        {
            GenerateRet(node, _body);


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

        public override void Visit(RppVar node)
        {
            LocalBuilder localVar = _body.DeclareLocal(node.RuntimeType);

            if (!(node.InitExpr is RppEmptyExpr))
            {
                _body.Emit(OpCodes.Stloc, localVar);
            }
        }

        public override void VisitEnter(RppBlockExpr node)
        {
            Console.WriteLine("Block expr");
        }

        public override void VisitExit(RppBlockExpr node)
        {
        }

        private readonly Dictionary<string, OpCode> OpToIL = new Dictionary<string, OpCode>
        {
            {"+", OpCodes.Add},
            {"-", OpCodes.Sub},
            {"*", OpCodes.Mul},
            {"/", OpCodes.Div}
        };

        public override void Visit(BinOp node)
        {
            OpCode opCode;
            if (OpToIL.TryGetValue(node.Op, out opCode))
            {
                _body.Emit(opCode);
            }
            else
            {
                throw new Exception("Can't generate code for: " + node.Op);
            }
        }

        public override void Visit(RppInteger node)
        {
            _body.Emit(OpCodes.Ldc_I4, node.Value);
        }

        public override void Visit(RppString node)
        {
            _body.Emit(OpCodes.Ldstr, node.Value);
        }

        public override void Visit(RppFuncCall node)
        {
            // TODO we should keep references to functions by making another pass of code gen before
            // real code generation
            if (node.Name == "ctor()")
            {
                ConstructorInfo constructor = typeof (Object).GetConstructor(Type.EmptyTypes);
                Debug.Assert(constructor != null, "constructor != null");
                _body.Emit(OpCodes.Call, constructor);
            }
            else
            {
                _body.Emit(OpCodes.Call, node.Function.RuntimeType);
            }
        }

        public override void Visit(RppSelector node)
        {
            throw new NotImplementedException();
        }

        public override void Visit(RppId node)
        {
            node.Ref.Accept(this);
        }

        public override void Visit(RppParam node)
        {
            _body.Emit(OpCodes.Ldarg, node.Index);
        }

        public override void Visit(RppNew node)
        {
            // ConstructorInfo constructorInfo = node.RefClass.RuntimeType.GetConstructor(Type.EmptyTypes);
            // node.RefClass.GetConstructor();
            // Debug.Assert(constructorInfo != null, "constructorInfo != null");
            // _body.Emit(OpCodes.Newobj, constructorInfo);
        }

        public override void Visit(RppAssignOp node)
        {
            Console.WriteLine(node.Left);
        }
    }
}