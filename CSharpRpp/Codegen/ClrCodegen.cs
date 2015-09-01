using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using CSharpRpp.Exceptions;
using CSharpRpp.Expr;
using JetBrains.Annotations;

namespace CSharpRpp.Codegen
{
    class ClrCodegen : RppNodeVisitor
    {
        private ILGenerator _body;

        // inst.myField - selector with RppId, so when it generates field access it wants to
        // load 'this', which is wrong, because 'inst' already loaded
        private bool _inSelector;

        private TypeBuilder _typeBuilder;

        private readonly Dictionary<string, OpCode> _arithmToIl = new Dictionary<string, OpCode>
        {
            {"+", OpCodes.Add},
            {"-", OpCodes.Sub},
            {"*", OpCodes.Mul},
            {"/", OpCodes.Div}
        };

        private bool _logicalGen;
        private Label _trueLabel;
        private Label _exitLabel;
        private LocalBuilder _logicalTemp;
        private int _closureId;

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
            _typeBuilder = node.RuntimeType as TypeBuilder;
            _closureId = 1;
        }

        private readonly Regex _typeExcSplitter = new Regex(@"'(.*?)'", RegexOptions.Singleline);

        public override void VisitExit(RppClass node)
        {
            var clazz = node.RuntimeType as TypeBuilder;
            Debug.Assert(clazz != null, "clazz != null");
            try
            {
                clazz.CreateType();
            } // TODO This is a hack, we should do our own semantic analyzes and find out which methods were not overriden
            catch (TypeLoadException exception)
            {
                MatchCollection groups = _typeExcSplitter.Matches(exception.Message);
                if (groups.Count != 3)
                {
                    throw;
                }

                string msg = $"Method '{groups[0]}' in class '{groups[1]}' does not have an implementation";
                throw new SemanticException(msg);
            }

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

        private readonly Dictionary<string, OpCode> logToIl = new Dictionary<string, OpCode>
        {
            {"&&", OpCodes.Ceq},
            {"||", OpCodes.Sub},
            {"<", OpCodes.Clt},
            {">", OpCodes.Cgt},
            {"==", OpCodes.Ceq},
            {"!=", OpCodes.Ceq}
        };


        public override void Visit(RppLogicalBinOp node)
        {
            bool firstPass = !_logicalGen;

            if (!_logicalGen)
            {
                _logicalGen = true;
                _trueLabel = _body.DefineLabel();
                _exitLabel = _body.DefineLabel();
                _logicalTemp = _body.DeclareLocal(Types.Bool);
            }

            if (node.Op == "||")
            {
                node.Left.Accept(this);
                if (!(node.Left is RppLogicalBinOp))
                {
                    _body.Emit(OpCodes.Brtrue_S, _trueLabel);
                }
                node.Right.Accept(this);

                if (firstPass)
                {
                    _body.Emit(OpCodes.Br_S, _exitLabel);
                    _body.MarkLabel(_trueLabel);
                    _body.Emit(OpCodes.Ldc_I4_1);
                    _body.MarkLabel(_exitLabel);
                    ClrCodegenUtils.StoreLocal(_logicalTemp, _body);
                }
                else
                {
                    _body.Emit(OpCodes.Brtrue_S, _trueLabel);
                }
            }

            if (firstPass)
            {
                ClrCodegenUtils.LoadLocal(_logicalTemp, _body);
            }

            _logicalGen = false;
        }

        public override void Visit(RppRelationalBinOp node)
        {
            LocalBuilder tempVar = _body.DeclareLocal(Types.Bool);
            node.Left.Accept(this);
            node.Right.Accept(this);

            switch (node.Op)
            {
                case "<":
                    _body.Emit(OpCodes.Clt);
                    break;
                case ">":
                    _body.Emit(OpCodes.Cgt);
                    break;
                case "==":
                    _body.Emit(OpCodes.Ceq);
                    break;
            }

            ClrCodegenUtils.StoreLocal(tempVar, _body);
            ClrCodegenUtils.LoadLocal(tempVar, _body);
        }


        public override void Visit(RppArithmBinOp node)
        {
            OpCode opCode;
            if (_arithmToIl.TryGetValue(node.Op, out opCode))
            {
                node.Left.Accept(this);
                node.Right.Accept(this);
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
            var elementType = node.Type.Runtime.GetElementType();
            LocalBuilder arrVar = _body.DeclareLocal(node.Type.Runtime);
            ClrCodegenUtils.LoadInt(node.Size, _body);
            _body.Emit(OpCodes.Newarr, elementType);

            ClrCodegenUtils.StoreLocal(arrVar, _body);

            int index = 0;
            foreach (var initializer in node.Initializers)
            {
                ClrCodegenUtils.LoadLocal(arrVar, _body);
                ClrCodegenUtils.LoadInt(index, _body);
                initializer.Accept(this);
                _body.Emit(OpCodes.Stelem, elementType);
                index++;
            }

            ClrCodegenUtils.LoadLocal(arrVar, _body);
        }

        private static OpCode StoreElementCodeByType(Type type)
        {
            if (type == Types.Int)
            {
                return OpCodes.Stelem_I4;
            }

            if (type == Types.Float)
            {
                return OpCodes.Stelem_R4;
            }

            if (type == Types.Double)
            {
                return OpCodes.Stelem_R4;
            }

            return OpCodes.Stelem_Ref;
        }

        public override void Visit(RppFuncCall node)
        {
            Console.WriteLine("Generating func call");
            // TODO we should keep references to functions by making another pass of code gen before
            // real code generation
            if (node.Name == "ctor()")
            {
                _body.Emit(OpCodes.Ldarg_0);
                ConstructorInfo constructor = typeof (object).GetConstructor(Type.EmptyTypes);
                Debug.Assert(constructor != null, "constructor != null");
                _body.Emit(OpCodes.Call, constructor);
            }
            else
            {
                // TODO Probably makes more sense to make RppConstructorCall ast, instead of boolean
                if (node.IsConstructorCall)
                {
                    _body.Emit(OpCodes.Ldarg_0);
                    node.Args.ForEach(arg => arg.Accept(this));
                    var constructor = node.Function.ConstructorInfo;
                    _body.Emit(OpCodes.Call, constructor);
                }
                else
                {
                    if (!node.Function.IsStatic && !_inSelector)
                    {
                        _body.Emit(OpCodes.Ldarg_0); // load 'this'
                    }

                    if (node.Function.IsStatic)
                    {
                        var instanceField = node.Function.Class.InstanceField.Builder;
                        _body.Emit(OpCodes.Ldsfld, instanceField);
                    }

                    node.Args.ForEach(arg => arg.Accept(this));

                    if (node.Function.IsStub)
                    {
                        // Not real functions, like Array.length
                        CodegenForStub(node.Function);
                    }
                    else
                    {
                        MethodInfo method = node.Function.RuntimeType;
                        if (method.IsGenericMethod)
                        {
                            Type[] typeArgs = node.TypeArgs.Select(type => type.Runtime).ToArray();
                            method = method.MakeGenericMethod(typeArgs);
                        }

                        _body.Emit(OpCodes.Callvirt, method);
                    }
                }
            }
        }

        private void CodegenForStub(IRppFunc function)
        {
            if (function.Class.Name == "Array")
            {
                if (function.Name == "length")
                {
                    _body.Emit(OpCodes.Ldlen);
                    return;
                }
            }

            throw new NotImplementedException("Other funcs are not implemented");
        }

        public override void Visit(RppBaseConstructorCall node)
        {
            _body.Emit(OpCodes.Ldarg_0);
            node.Args.ForEach(arg => arg.Accept(this));

            ConstructorInfo constructor = node.BaseConstructor.ConstructorInfo;
            if (node.BaseClassType.Runtime.IsGenericType)
            {
                constructor = TypeBuilder.GetConstructor(node.BaseClassType.Runtime, constructor);
            }

            Debug.Assert(constructor != null, "constructor != null, we should have figure out which constructor to use before");
            _body.Emit(OpCodes.Call, constructor);
        }

        private RppType _selectorType;

        public override void Visit(RppSelector node)
        {
            node.Target.Accept(this);
            _selectorType = node.Target.Type;
            _inSelector = true;
            node.Path.Accept(this);
            _inSelector = false;
        }

        public override void Visit(RppId node)
        {
            if (node.Ref is RppField)
            {
                if (!_inSelector)
                {
                    _body.Emit(OpCodes.Ldarg_0);
                }


                RppField field = (RppField) node.Ref;
                FieldInfo cilField = field.Builder;
                // TODO this is wierd, we should have all info in the fieldSelector
                if (_selectorType != null && _selectorType.Runtime.IsGenericType)
                {
                    cilField = TypeBuilder.GetField(_selectorType.Runtime, field.Builder);
                }

                Debug.Assert(cilField != null, "cilField != null");
                _body.Emit(OpCodes.Ldfld, cilField);
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
            IRppFunc constructor = node.Constructor;

            if (node.TypeArgs.Any())
            {
                var genericArgs = node.TypeArgs.Select(variant => variant.Runtime).ToArray();
                Type specializedType = node.RefClass.RuntimeType.MakeGenericType(genericArgs);
                var specializedConstr = TypeBuilder.GetConstructor(specializedType, constructor.ConstructorInfo);
                _body.Emit(OpCodes.Newobj, specializedConstr);
            }
            else
            {
                // TODO RppNativeClass don't have constructor builders, they have constructorinfo instead, fix this
                _body.Emit(OpCodes.Newobj, constructor.ConstructorInfo);
            }
        }

        public override void Visit(RppAssignOp node)
        {
            RppId id = node.Left as RppId;

            Debug.Assert(id != null, "id != null");

            if (id.Ref is RppField)
            {
                RppField field = (RppField) id.Ref;
                _body.Emit(OpCodes.Ldarg_0);
                node.Right.Accept(this);
                _body.Emit(OpCodes.Stfld, field.Builder);
            }
            else if (id.Ref is RppVar)
            {
                RppVar var = (RppVar) id.Ref;
                node.Right.Accept(this);
                ClrCodegenUtils.StoreLocal(var.Builder, _body);
            }
            else if (id.Ref is RppParam)
            {
                throw new Exception("Not implemented");
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
            Label enterLoop = _body.DefineLabel();
            Label exitLoop = _body.DefineLabel();
            _body.MarkLabel(enterLoop);
            node.Condition.Accept(this);
            _body.Emit(OpCodes.Brfalse_S, exitLoop);
            node.Body.Accept(this);
            _body.Emit(OpCodes.Br_S, enterLoop);
            _body.MarkLabel(exitLoop);
        }

        public override void Visit(RppIf node)
        {
            Label jumpOverLabel = _body.DefineLabel();
            Label elseLabel = _body.DefineLabel();
            node.Condition.Accept(this);
            _body.Emit(OpCodes.Brfalse, elseLabel);
            node.ThenExpr.Accept(this);
            _body.Emit(OpCodes.Br, jumpOverLabel);
            _body.MarkLabel(elseLabel);
            node.ElseExpr.Accept(this);
            _body.MarkLabel(jumpOverLabel);
        }

        public override void Visit(RppThrow node)
        {
            node.Expr.Accept(this);
            _body.Emit(OpCodes.Throw);
        }

        public override void Visit(RppNull node)
        {
            _body.Emit(OpCodes.Ldnull);
        }

        public override void Visit(RppClosure node)
        {
            Type[] argTypes = node.Bindings.Select(p => p.Type.Runtime).ToArray();
            TypeBuilder closureClass = _typeBuilder.DefineNestedType("c__Closure" + (_closureId++),
                TypeAttributes.AutoClass | TypeAttributes.AnsiClass | TypeAttributes.Sealed | TypeAttributes.NestedPrivate,
                typeof (object),
                new[] {node.Type.Runtime});

            string[] genericTypes = argTypes.Where(arg => arg.IsGenericParameter).Select(arg => arg.Name).ToArray();
            if (genericTypes.Length > 0)
            {
                closureClass.DefineGenericParameters(genericTypes);
            }

            MethodBuilder applyMethod = closureClass.DefineMethod("apply",
                MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.Public,
                CallingConventions.Standard);

            applyMethod.SetParameters(argTypes);
            applyMethod.SetReturnType(node.ReturnType.Runtime);

            int index = 1;
            foreach (var param in node.Bindings)
            {
                applyMethod.DefineParameter(index, ParameterAttributes.None, param.Name);
                param.Index = index++;
            }

            ILGenerator body = applyMethod.GetILGenerator();
            ClrCodegen codegen = new ClrCodegen(body);
            node.Expr.Accept(codegen);
            body.Emit(OpCodes.Ret);


            ConstructorInfo defaultClosureConstructor = closureClass.DefineDefaultConstructor(MethodAttributes.Public);
            Debug.Assert(defaultClosureConstructor != null, "defaultClosureConstructor != null");
            _body.Emit(OpCodes.Newobj, defaultClosureConstructor);
            closureClass.CreateType();
        }

        public override void Visit(RppBooleanLiteral node)
        {
            var boolOpCode = node.Value ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0;
            _body.Emit(boolOpCode);
        }

        public override void Visit(RppFieldSelector fieldSelector)
        {
            if (!_inSelector)
            {
                _body.Emit(OpCodes.Ldarg_0); // load 'this'
            }

            Debug.Assert(fieldSelector.Field != null, "fieldSelector.Field != null");

            FieldInfo field = fieldSelector.Field.Builder;
            Type targetType = fieldSelector.ClassType.Runtime;

            if (field.FieldType.ContainsGenericParameters)
            {
                field = TypeBuilder.GetField(targetType, field);
            }

            Debug.Assert(field != null, "field != null");
            _body.Emit(OpCodes.Ldfld, field);
        }
    }
}