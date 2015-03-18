using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            _body = node.Builder.GetILGenerator();
        }

        public override void VisitExit(RppFunc node)
        {
            GenerateRet(node, _body);

            Console.WriteLine("Func generated");
        }

        private static void GenerateRet([NotNull] RppFunc node, [NotNull] ILGenerator generator)
        {
            if (node.ReturnType.Runtime == typeof (void) && node.Expr.Type.Runtime != typeof (void))
            {
                generator.Emit(OpCodes.Pop);
            }

            generator.Emit(OpCodes.Ret);
        }

        public override void Visit(RppVar node)
        {
            node.Builder = _body.DeclareLocal(node.Type.Runtime);

            if (!(node.InitExpr is RppEmptyExpr))
            {
                node.InitExpr.Accept(this);
                _body.Emit(OpCodes.Stloc, node.Builder);
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

        public override void Visit(RppFloat node)
        {
            _body.Emit(OpCodes.Ldc_R4, node.Value);
        }

        public override void Visit(RppString node)
        {
            _body.Emit(OpCodes.Ldstr, node.Value);
        }

        public override void Visit(RppArray node)
        {
            var arrayType = node.Type.Runtime.GetElementType();
            Debug.Assert(arrayType == typeof (int));

            LocalBuilder arrVar = _body.DeclareLocal(node.Type.Runtime);
            ClrCodegenUtils.LoadInt(node.Size, _body);
            _body.Emit(OpCodes.Newarr, arrayType);

            ClrCodegenUtils.StoreLocal(arrVar, _body);

            // only int8, int16, int32 are supported as primitive types everything else should be boxed
            bool isElementTypeRef = arrayType != typeof (int);
            OpCode storingOpCode = isElementTypeRef ? OpCodes.Stelem_Ref : OpCodes.Stelem_I4;
            int index = 0;
            foreach (var initializer in node.Initializers)
            {
                ClrCodegenUtils.LoadLocal(arrVar, _body);
                ClrCodegenUtils.LoadInt(index, _body);
                initializer.Accept(this);
                _body.Emit(storingOpCode);
                index++;
            }

            ClrCodegenUtils.LoadLocal(arrVar, _body);
        }

        public override void Visit(RppFuncCall node)
        {
            // TODO we should keep references to functions by making another pass of code gen before
            // real code generation
            if (node.Name == "ctor()")
            {
                _body.Emit(OpCodes.Ldarg_0);
                ConstructorInfo constructor = typeof (Object).GetConstructor(Type.EmptyTypes);
                Debug.Assert(constructor != null, "constructor != null");
                _body.Emit(OpCodes.Call, constructor);
            }
            else
            {
                if (!node.Function.IsStatic)
                {
                    _body.Emit(OpCodes.Ldarg_0); // push this
                }

                node.Args.ForEach(arg => arg.Accept(this));
                _body.Emit(OpCodes.Call, node.Function.RuntimeType);
            }
        }

        public override void Visit(RppMessage node)
        {
            node.Args.ForEach(arg => arg.Accept(this));

            // Normal function call
            if (node.Function.RuntimeType != null)
            {
                _body.Emit(OpCodes.Call, node.Function.RuntimeType);
            }
            else
            {
                // TODO fix this, identify stubs by some other means
                // Function calls for stubs don't have RuntimeType because they are defined dynamically
                if (node.Function.Class != null)
                {
                    if (node.Function.Class.Name == "Array")
                    {
                        if (node.Name == "length")
                        {
                            _body.Emit(OpCodes.Ldlen);
                        }
                    }
                }
            }
        }

        public override void Visit(RppSelector node)
        {
            node.Target.Accept(this);
            node.Path.Accept(this);
        }

        public override void Visit(RppId node)
        {
            if (node.Ref is RppField)
            {
                _body.Emit(OpCodes.Ldarg_0);
                _body.Emit(OpCodes.Ldfld, ((RppField) node.Ref).Builder);
            }
            else if (node.Ref is RppVar)
            {
                _body.Emit(OpCodes.Ldloc, ((RppVar) node.Ref).Builder);
            }
            else if (node.Ref is RppParam)
            {
                node.Ref.Accept(this);
            }
        }

        public override void Visit(RppParam node)
        {
            switch (node.Index)
            {
                case 0:
                    _body.Emit(OpCodes.Ldarg_0);
                    break;
                case 1:
                    _body.Emit(OpCodes.Ldarg_1);
                    break;
                case 2:
                    _body.Emit(OpCodes.Ldarg_2);
                    break;
                case 3:
                    _body.Emit(OpCodes.Ldarg_3);
                    break;
                default:
                    _body.Emit(OpCodes.Ldarg, (short) node.Index);
                    break;
            }
        }

        public override void Visit(RppNew node)
        {
            node.Args.ForEach(arg => arg.Accept(this));
            _body.Emit(OpCodes.Newobj, node.RefClass.Constructor.ConstructorBuilder);
        }

        public override void Visit(RppAssignOp node)
        {
            if (node.Left.Ref is RppField)
            {
                RppField field = (RppField) node.Left.Ref;
                _body.Emit(OpCodes.Ldarg_0);
                node.Right.Accept(this);
                _body.Emit(OpCodes.Stfld, field.Builder);
            }
            else if (node.Left.Ref is RppVar)
            {
            }
            else if (node.Left.Ref is RppParam)
            {
            }
        }

        public override void Visit(RppField node)
        {
            _body.Emit(OpCodes.Ldarg_0);
            _body.Emit(OpCodes.Ldfld, node.Builder);
        }

        public override void Visit(RppBox node)
        {
            node.Expression.Accept(this);
            _body.Emit(OpCodes.Box, node.Expression.Type.Runtime);
        }

        public override void Visit(RppWhile node)
        {
            return;
        }
    }
}