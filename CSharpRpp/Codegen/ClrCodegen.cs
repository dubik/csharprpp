using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using CSharpRpp.Exceptions;
using CSharpRpp.Expr;
using CSharpRpp.TypeSystem;
using JetBrains.Annotations;

namespace CSharpRpp.Codegen
{
    class ClrClosureContext
    {
        /// <summary>
        /// Maps local variable builders to fields if ClrCodegen generates code for closure
        /// </summary>
        public Dictionary<LocalBuilder, Tuple<FieldBuilder, Type>> CapturedVars { get; set; }

        public FieldBuilder CapturedThis { get; set; }

        public Dictionary<int, FieldBuilder> CapturedParams { get; set; }
    }

    class ClrCodegen : RppNodeVisitor
    {
        private ILGenerator _body;

        // inst.myField - selector with RppId, so when it generates field access it wants to
        // load 'this', which is wrong, because 'inst' already loaded
        private bool _inSelector;

        private TypeBuilder _typeBuilder;
        private MethodBase _func;

        private static readonly Dictionary<string, OpCode> ArithmToIl = new Dictionary<string, OpCode>
        {
            {"+", OpCodes.Add},
            {"-", OpCodes.Sub},
            {"*", OpCodes.Mul},
            {"/", OpCodes.Div}
        };

        private static readonly Dictionary<string, OpCode> BitwiseArithmToIl = new Dictionary<string, OpCode>
        {
            {"&", OpCodes.And},
            {"|", OpCodes.Or},
            {"^", OpCodes.Xor},
        };

        private static readonly Regex TypeExcSplitter = new Regex(@"'(.*?)'", RegexOptions.Singleline);

        private static int _closureId;

        /// <summary>
        /// Indicates if jump instruction was issued already. When we load value inside logical condition, we need to jump
        /// according to _onTrue. But if we have nested conditions, jump was already done, so no need to issue another.
        /// </summary>
        private bool Jumped { get; set; }

        /// <summary>
        /// Exit label for logical operator evaluation
        /// </summary>
        private readonly Label? _targetLabel;

        /// <summary>
        /// Indicates on which value branch should jump. If it has true, then it will jump if result is true and will
        /// continue with if its false
        /// </summary>
        private readonly bool _onTrue;

        [CanBeNull]
        public ClrClosureContext ClosureContext { get; set; }

        public ClrCodegen()
        {
        }

        /// <summary>
        /// Creates ClrCodegen with specific ILGenerator in case code is needed to be inserted into
        /// specific function.
        /// </summary>
        /// <param name="body"></param>
        public ClrCodegen(ILGenerator body) : this()
        {
            _body = body;
        }

        public ClrCodegen(TypeBuilder builder, ILGenerator body) : this(body)
        {
            _typeBuilder = builder;
        }

        public ClrCodegen(ILGenerator body, Label targetLabel, bool onTrue) : this(body)
        {
            _targetLabel = targetLabel;
            _onTrue = onTrue;
        }

        public ClrCodegen(ILGenerator body, ClrClosureContext closureContext) : this(body)
        {
            ClosureContext = closureContext;
        }

        public override void VisitEnter(RppClass node)
        {
            Console.WriteLine("Genering class: " + node.Name);
            _typeBuilder = node.Type.NativeType as TypeBuilder;
            Debug.Assert(_typeBuilder != null, "_typeBuilder != null");
            _closureId = 1;

            if (node.IsObject())
            {
                CreateStaticConstructor(_typeBuilder, node.InstanceField, node);
            }
        }

        private void CreateStaticConstructor(TypeBuilder typeBuilder, RppField instanceField, RppClass obj)
        {
            FieldInfo instanceFieldInfo = instanceField.FieldInfo.Native;
            ConstructorBuilder staticConstructor = typeBuilder.DefineTypeInitializer();
            _body = staticConstructor.GetILGenerator();
            instanceField.InitExpr.Accept(this);
            _body.Emit(OpCodes.Stsfld, instanceFieldInfo);
            _body.Emit(OpCodes.Ret);
        }

        public override void VisitExit(RppClass node)
        {
            TypeBuilder clazz = node.Type.NativeType as TypeBuilder;
            Debug.Assert(clazz != null, "clazz != null");
            try
            {
                clazz.CreateType();
            } // TODO This is a hack, we should do our own semantic analyzes and find out which methods were not overriden
            catch (TypeLoadException exception)
            {
                MatchCollection groups = TypeExcSplitter.Matches(exception.Message);
                if (groups.Count != 3)
                {
                    throw;
                }

                string msg = $"Method '{groups[0]}' in class '{groups[1]}' does not have an implementation";
                throw new SemanticException(105, msg);
            }

            Console.WriteLine("Generated class");
        }

        public override void VisitEnter(RppFunc node)
        {
            Console.WriteLine("Generating func: " + node.Name);
            _body = GetGenerator(node.MethodInfo.Native);
            _func = node.MethodInfo.Native;
        }

        [NotNull]
        private static ILGenerator GetGenerator([NotNull] MethodBase method)
        {
            if (method is ConstructorBuilder)
            {
                return ((ConstructorBuilder) method).GetILGenerator();
            }

            return ((MethodBuilder) method).GetILGenerator();
        }

        public override void VisitExit(RppFunc node)
        {
            GenerateRet(node, _body);

            Console.WriteLine("Func generated");
        }

        private static void GenerateRet([NotNull] RppFunc func, [NotNull] ILGenerator generator)
        {
            RType funcReturnType = func.ReturnType.Value;
            RType expressionType = func.Expr.Type.Value;

            // nothing is throw, we shouldn't return in that case
            if (!expressionType.Equals(RppTypeSystem.NothingTy))
            {
                if (funcReturnType.Equals(RppTypeSystem.UnitTy) && !expressionType.Equals(RppTypeSystem.UnitTy))
                {
                    generator.Emit(OpCodes.Pop);
                }
                generator.Emit(OpCodes.Ret);
            }
        }

        public override void Visit(RppVar node)
        {
            if (!(node.InitExpr is RppEmptyExpr) && !(node.InitExpr is RppDefaultExpr))
            {
                node.InitExpr.Accept(this);
                ClrVarCodegen.DeclareAndInitialize(node, _body);
            }
            else
            {
                ClrVarCodegen.Declare(node, _body);
            }
        }

        private readonly Stack<Label> _blockExprExitLabels = new Stack<Label>();

        public override void VisitEnter(RppBlockExpr node)
        {
            Console.WriteLine("Block expr");
            if (node.Exitable)
            {
                Label exitLabel = _body.DefineLabel();
                _blockExprExitLabels.Push(exitLabel);
            }
        }

        public override void VisitExit(RppBlockExpr node)
        {
            if (node.Exitable)
            {
                _body.MarkLabel(_blockExprExitLabels.Pop());
            }
        }


        public override void Visit(RppBooleanLiteral node)
        {
            var boolOpCode = node.Value ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0;
            _body.Emit(boolOpCode);
        }

        public override void Visit(RppRelationalBinOp node)
        {
            node.Left.Accept(this);
            node.Right.Accept(this);

            bool shouldJump = _targetLabel.HasValue;
            if (shouldJump)
            {
                OpCode cmpAndJumpOpCode;
                switch (node.Op)
                {
                    case "<":
                        cmpAndJumpOpCode = _onTrue ? OpCodes.Blt : OpCodes.Bge;
                        break;
                    case ">":
                        cmpAndJumpOpCode = _onTrue ? OpCodes.Bgt : OpCodes.Ble;
                        break;
                    case "==":
                        cmpAndJumpOpCode = _onTrue ? OpCodes.Beq : OpCodes.Bne_Un;
                        break;
                    case "!=":
                        cmpAndJumpOpCode = _onTrue ? OpCodes.Bne_Un : OpCodes.Beq;
                        break;
                    case ">=":
                        cmpAndJumpOpCode = _onTrue ? OpCodes.Bge : OpCodes.Blt;
                        break;
                    case "<=":
                        cmpAndJumpOpCode = _onTrue ? OpCodes.Ble : OpCodes.Bgt;
                        break;

                    default:
                        throw new Exception("Don't know how to handle " + node.Op);
                }

                _body.Emit(cmpAndJumpOpCode, _targetLabel.Value);

                Jumped = true;
            }
            else
            {
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
                    case "!=":
                        _body.Emit(OpCodes.Cgt_Un);
                        break;
                    case ">=":
                        _body.Emit(OpCodes.Clt);
                        _body.Emit(OpCodes.Ldc_I4_0);
                        _body.Emit(OpCodes.Ceq);
                        break;
                    case "<=":
                        _body.Emit(OpCodes.Cgt);
                        _body.Emit(OpCodes.Ldc_I4_0);
                        _body.Emit(OpCodes.Ceq);
                        break;
                    default:
                        throw new Exception("Unknown op " + node.Op);
                }
            }
        }

        public override void Visit(RppArithmBinOp node)
        {
            GenerateCodeForArithmOp(node, ArithmToIl);
        }

        public override void Visit(RppBitwiseOp node)
        {
            GenerateCodeForArithmOp(node, BitwiseArithmToIl);
        }

        private void GenerateCodeForArithmOp(RppBinOp node, IReadOnlyDictionary<string, OpCode> arithmToIl)
        {
            OpCode opCode;
            if (arithmToIl.TryGetValue(node.Op, out opCode))
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
            var arrayType = node.Type.Value.NativeType;
            Type elementType = arrayType.GetElementType();
            LocalBuilder arrVar = _body.DeclareLocal(arrayType);
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

        public override void Visit(RppFuncCall node)
        {
            Console.WriteLine("Generating func call");
            // TODO we should keep references to functions by making another pass of code gen before
            // real code generation
            if (node.Name == "ctor()")
            {
                _body.Emit(OpCodes.Ldarg_0);
                ConstructorInfo constructor = typeof(object).GetConstructor(Type.EmptyTypes);
                Debug.Assert(constructor != null, "constructor != null");
                _body.Emit(OpCodes.Call, constructor);
            }
            else
            {
                // TODO Probably makes more sense to make RppConstructorCall ast, instead of boolean
                RppMethodInfo rppMethodInfo = node.Function;
                if (node.IsConstructorCall)
                {
                    _body.Emit(OpCodes.Ldarg_0);
                    node.Args.ForEach(arg => arg.Accept(this));
                    var constructor = rppMethodInfo.Native as ConstructorInfo;
                    _body.Emit(OpCodes.Call, constructor);
                }
                else
                {
                    if (!_inSelector)
                    {
                        if (rppMethodInfo.IsStatic)
                        {
                            var instanceField = rppMethodInfo.DeclaringType.Fields.First(f => f.Name == "_instance");
                            _body.Emit(OpCodes.Ldsfld, instanceField.Native);
                        }
                        else
                        {
                            if (node.IsFromClosure)
                            {
                                _body.Emit(OpCodes.Ldarg_0);
                                Debug.Assert(ClosureContext?.CapturedThis != null, "CapturedThis != null");
                                _body.Emit(OpCodes.Ldfld, ClosureContext.CapturedThis);
                            }
                            else
                            {
                                _body.Emit(OpCodes.Ldarg_0); // load 'this'
                            }
                        }
                    }

                    // Create own code generator for arguments because they can have RppSelectors which may interfer with already existing RppSelector
                    // myField.CallFunc(anotherInstanceFunc()) would set _inSelector to true and no 'this' will be loaded
                    ClrCodegen codegen = new ClrCodegen(_typeBuilder, _body) {ClosureContext = ClosureContext};
                    node.Args.ForEach(arg => arg.Accept(codegen));

                    MethodInfo method = rppMethodInfo.Native as MethodInfo;

                    if (method == null) // This is a stub, so generate code for it
                    {
                        CodegenForStub(rppMethodInfo);
                        return;
                    }

                    Debug.Assert(method != null, "method != null");

                    if (method.IsGenericMethod)
                    {
                        Type[] methodTypeArgs = node.TypeArgs.Select(t => t.Value.NativeType).ToArray();
                        if (methodTypeArgs.Length != 0)
                        {
                            method = method.MakeGenericMethod(methodTypeArgs);
                        }
                    }

                    _body.Emit(OpCodes.Callvirt, method);
                }
            }
        }

        private void CodegenForStub(RppMethodInfo function)
        {
            if (function.DeclaringType.Name == "Array")
            {
                RType elementType = function.DeclaringType.GenericArguments.First();
                switch (function.Name)
                {
                    case "ctor":
                        _body.Emit(OpCodes.Newarr, elementType.NativeType);
                        break;

                    case "length":
                        _body.Emit(OpCodes.Ldlen);
                        break;

                    case "apply":
                    {
                        if (elementType.IsGenericParameter)
                        {
                            _body.Emit(OpCodes.Ldelem, elementType.NativeType);
                        }
                        else
                        {
                            OpCode ldOpCode = ClrCodegenUtils.ArrayLoadOpCodeByType(elementType.NativeType);
                            _body.Emit(ldOpCode);
                        }
                        break;
                    }

                    case "update":
                    {
                        OpCode? ldOpCode = ClrCodegenUtils.ArrayStoreOpCodeByType(elementType.NativeType);
                        if (ldOpCode.HasValue)
                        {
                            _body.Emit(ldOpCode.Value);
                        }
                        else
                        {
                            _body.Emit(OpCodes.Stelem, elementType.NativeType);
                        }
                        break;
                    }

                    default:
                        throw new NotImplementedException("Other funcs are not implemented");
                }
            }
        }

        public override void Visit(RppBaseConstructorCall node)
        {
            _body.Emit(OpCodes.Ldarg_0);
            node.Args.ForEach(arg => arg.Accept(this));

            if (node.BaseConstructor == null)
            {
                _body.Emit(OpCodes.Call, typeof(object).GetConstructor(new Type[0]));
                return;
            }

            ConstructorInfo constructor = node.BaseConstructor.Native as ConstructorInfo;

            Debug.Assert(constructor != null, "constructor != null, we should have figure out which constructor to use before");
            _body.Emit(OpCodes.Call, constructor);
        }

        public override void Visit(RppSelector node)
        {
            node.Target.Accept(this);
            _inSelector = true;
            node.Path.Accept(this);
            _inSelector = false;
        }

        public override void Visit(RppId node)
        {
            if (node.IsField)
            {
                LoadField(node.Field);
            }
            else if (node.IsVar)
            {
                ClrVarCodegen.Load((RppVar) node.Ref, _body, ClosureContext?.CapturedVars);
            }
            else if (node.IsParam)
            {
                ((RppParam) node.Ref).Accept(this);
            }
            else if (node.IsObject)
            {
                if (_typeBuilder == node.Type.Value.NativeType)
                {
                    _body.Emit(OpCodes.Ldarg_0); // we are in one of the methods of object, so first parameter is it's instance
                }
                else
                {
                    FieldInfo cilField = node.Type.Value.Fields.First(f => f.Name == "_instance").Native;
                    _body.Emit(OpCodes.Ldsfld, cilField);
                }
            }
            else
            {
                throw new NotImplementedException("don't know what to do here");
            }
        }

        public override void Visit(RppParam node)
        {
            int index = node.Index;
            FieldBuilder capturedParam;
            if (ClosureContext?.CapturedParams != null && ClosureContext.CapturedParams.TryGetValue(index, out capturedParam))
            {
                LoadArg(0);
                _body.Emit(OpCodes.Ldfld, capturedParam);
            }
            else
            {
                LoadArg(index);
            }
        }

        private void LoadArg(int index)
        {
            switch (index)
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
                    if (index < 256)
                    {
                        _body.Emit(OpCodes.Ldarg_S, (byte) index);
                    }
                    else
                    {
                        _body.Emit(OpCodes.Ldarg, (short) index);
                    }
                    break;
            }
        }

        public override void Visit(RppNew node)
        {
            node.Args.ForEach(arg => arg.Accept(this));
            RppMethodInfo constructor = node.Constructor;

            var methodBase = constructor.Native;
            if (methodBase == null)
            {
                CodegenForStub(constructor);
            }
            else
            {
                ConstructorInfo constructorInfo = methodBase as ConstructorInfo;
                if (constructorInfo == null)
                {
                    Console.WriteLine(methodBase.GetType());
                }
                Debug.Assert(constructorInfo != null);
                _body.Emit(OpCodes.Newobj, constructorInfo);
            }
        }

        public override void Visit(RppAssignOp node)
        {
            RppId id = node.Left as RppId;

            Debug.Assert(id != null, "id != null");

            if (id.IsField)
            {
                _body.Emit(OpCodes.Ldarg_0);
                node.Right.Accept(this);
                _body.Emit(OpCodes.Stfld, id.Field.Native);
            }
            else if (id.IsVar)
            {
                RppVar var = (RppVar) id.Ref;
                node.Right.Accept(this);
                ClrVarCodegen.Store(var, _body, ClosureContext?.CapturedVars);
            }
            else if (id.IsParam)
            {
                throw new Exception("Not implemented");
            }
        }

        public override void Visit(RppBox node)
        {
            node.Expression.Accept(this);
            _body.Emit(OpCodes.Box, node.Expression.Type.Value.NativeType);
        }

        public override void Visit(RppIf node)
        {
            Label exitLabel = _body.DefineLabel();
            Label elseLabel = _body.DefineLabel();

            // if condition is false we jump to else label, otherwise we just fallthrough
            EmitBranchableAndJumpOnResult(node.Condition, elseLabel, false);

            node.ThenExpr.Accept(this);
            _body.Emit(OpCodes.Br, exitLabel);
            _body.MarkLabel(elseLabel);
            node.ElseExpr.Accept(this);
            _body.MarkLabel(exitLabel);
        }

        private bool EmitBranchable(IRppNode expr, Label targetLabel, bool onTrue)
        {
            ClrCodegen nestedCodegen = new ClrCodegen(_body, targetLabel, onTrue) {ClosureContext = ClosureContext};
            expr.Accept(nestedCodegen);
            return nestedCodegen.Jumped;
        }

        private void EmitBranchableAndJumpOnResult(IRppNode expr, Label targetLabel, bool onTrue)
        {
            bool notJumped = !EmitBranchable(expr, targetLabel, onTrue);
            if (notJumped)
            {
                _body.Emit(onTrue ? OpCodes.Brtrue : OpCodes.Brfalse, targetLabel);
            }

            Jumped = true;
        }

        public override void Visit(RppLogicalBinOp node)
        {
            // _targetLabel doesn't contain value if logical op is not used from 'if' or 'while'
            // e.g. k = i > 5 && k < 0
            var targetLabel = _targetLabel ?? _body.DefineLabel();

            if (node.Op == "&&")
            {
                if (_onTrue)
                {
                    Label test_end = _body.DefineLabel();

                    EmitBranchableAndJumpOnResult(node.Left, test_end, false);
                    EmitBranchableAndJumpOnResult(node.Right, targetLabel, true);

                    _body.MarkLabel(test_end);
                }
                else
                {
                    EmitBranchableAndJumpOnResult(node.Left, targetLabel, false);
                    EmitBranchableAndJumpOnResult(node.Right, targetLabel, false);
                }
            }
            else if (node.Op == "||")
            {
                if (_onTrue)
                {
                    EmitBranchableAndJumpOnResult(node.Left, targetLabel, true);
                    EmitBranchableAndJumpOnResult(node.Right, targetLabel, true);
                }
                else
                {
                    Label test_end = _body.DefineLabel();

                    EmitBranchableAndJumpOnResult(node.Left, test_end, true);
                    EmitBranchableAndJumpOnResult(node.Right, targetLabel, false);

                    _body.MarkLabel(test_end);
                }
            }
            else
            {
                throw new NotImplementedException();
            }

            // Create epilogue only is target label wasn't defined, so we return a bool
            if (!_targetLabel.HasValue)
            {
                Label endLabel = _body.DefineLabel();
                _body.Emit(OpCodes.Ldc_I4_1);
                _body.Emit(OpCodes.Br_S, endLabel);
                _body.MarkLabel(targetLabel);
                _body.Emit(OpCodes.Ldc_I4_0);
                _body.MarkLabel(endLabel);
            }
        }

        public override void Visit(RppWhile node)
        {
            Label condition = _body.DefineLabel();
            Label loopBody = _body.DefineLabel();
            Label exitLabel = _body.DefineLabel();

            _body.Emit(OpCodes.Br_S, condition);
            _body.MarkLabel(loopBody);
            node.Body.Accept(this);
            _body.MarkLabel(condition);


            node.Condition.Accept(this);

            _body.Emit(OpCodes.Brtrue, loopBody);
            _body.MarkLabel(exitLabel);
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
            // TODO actually we can use ast classes to create closure, RType with generic types
            // which already has generics manipulation so this is quite bad way of doing it
            TypeBuilder closureClass = _typeBuilder.DefineNestedType("c__Closure" + (_closureId++),
                TypeAttributes.AutoClass | TypeAttributes.AnsiClass | TypeAttributes.Sealed | TypeAttributes.NestedPrivate);

            var rppGenericParameters = node.ClosureType.GenericParameters.ToArray();
            string[] closureGenericArgumentsNames = rppGenericParameters.Select(arg => "T" + arg.Name).ToArray();
            GenericTypeParameterBuilder[] gpBuilders = {};
            if (closureGenericArgumentsNames.Length > 0)
            {
                gpBuilders = closureClass.DefineGenericParameters(closureGenericArgumentsNames);
                for (int i = 0; i < gpBuilders.Length; i++)
                {
                    rppGenericParameters[i].SetGenericTypeParameterBuilder(gpBuilders[i]);
                }
            }

            Type[] argTypes = node.Bindings.Select(p => p.Type.Value.NativeType).ToArray();
            Type parentType = node.Type.Value.NativeType;

            var capturedVars = CreateFieldsForCapturedVars(closureClass, node.CapturedVars);
            var capturedParams = CreateFieldsForCapturedParams(closureClass, node.CapturedParams);
            var capturedThis = node.Context.IsCaptureThis ? CreatedCapturedThis(closureClass) : null;
            Type returnType = node.ReturnType.Value.NativeType;

            // We need to create closure's own generic parameters because we can't reuse function's ones
            /*
                def myFunc[A] = {
                    val k = (x : A) = x
                }

                will be expanded into
                class _Closure[_A] extends Function1[_A, _A]
                {
                    def apply(x: _A) = x
                }

                def myFunc[A] = {
                    val k: _Closure[A] = new _Closure[A]()
                }

                So _Closure needs to have it's own generic parameter _A which will be substituted with A

                So we make an array with params:
                [T1, T2, ..., TRes] then find generic params, then define generic params for closure, then
                replace them with [_T1, _T2, ...., _TRes] (excluding non generic params)
            */
            Type[] closureSignature = argTypes.Concat(returnType).ToArray();

            if (closureGenericArgumentsNames.Length > 0)
            {
                var targetSignature = closureSignature.Select(t =>
                    {
                        if (t.IsGenericParameter)
                        {
                            Type closureGenericArgument = gpBuilders[t.GenericParameterPosition];
                            return closureGenericArgument;
                        }
                        return t;
                    }).ToArray();

                returnType = targetSignature.Last();
                argTypes = targetSignature.Take(targetSignature.Length - 1).ToArray();

                if (parentType.IsGenericType) // Parent is not generic if it's Action0
                {
                    Type genericTypeDef = parentType.GetGenericTypeDefinition();
                    if (returnType == typeof(void))
                    {
                        parentType = genericTypeDef.MakeGenericType(argTypes); // don't include 'void'
                    }
                    else
                    {
                        parentType = genericTypeDef.MakeGenericType(targetSignature);
                    }
                }
            }

            closureClass.AddInterfaceImplementation(parentType);
            MethodBuilder applyMethod = closureClass.DefineMethod("apply",
                MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.Public,
                CallingConventions.Standard);

            applyMethod.SetParameters(argTypes);
            applyMethod.SetReturnType(returnType);

            int index = 1;
            foreach (var param in node.Bindings)
            {
                applyMethod.DefineParameter(index, ParameterAttributes.None, param.Name);
                param.Index = index++;
            }

            ILGenerator body = applyMethod.GetILGenerator();
            ClrClosureContext closureContext = new ClrClosureContext
            {
                CapturedVars = capturedVars,
                CapturedParams = capturedParams,
                CapturedThis = capturedThis
            };
            ClrCodegen codegen = new ClrCodegen(body, closureContext);
            node.Expr.Accept(codegen);
            body.Emit(OpCodes.Ret);

            Type[] typeArguments = node.OriginalGenericArguments.Select(arg => arg.Type.NativeType).ToArray();
            Type specializedClosureClass = typeArguments.NonEmpty() ? closureClass.MakeGenericType(typeArguments) : closureClass;
            ConstructorInfo defaultClosureConstructor = closureClass.DefineDefaultConstructor(MethodAttributes.Public);

            if (closureGenericArgumentsNames.Length > 0)
            {
                defaultClosureConstructor = TypeBuilder.GetConstructor(specializedClosureClass, defaultClosureConstructor);
            }

            Debug.Assert(defaultClosureConstructor != null, "defaultClosureConstructor != null");
            _body.Emit(OpCodes.Newobj, defaultClosureConstructor);
            LocalBuilder closureClassInstance = _body.DeclareLocal(specializedClosureClass);
            _body.Emit(OpCodes.Stloc, closureClassInstance);

            // Initialize captured local vars
            capturedVars.ForEach(pair =>
                {
                    _body.Emit(OpCodes.Ldloc, closureClassInstance);
                    LocalBuilder capturedVariable = pair.Key;
                    _body.Emit(OpCodes.Ldloc, capturedVariable);
                    FieldInfo field = pair.Value.Item1;
                    if (specializedClosureClass.IsGenericType)
                        field = TypeBuilder.GetField(specializedClosureClass, field);
                    _body.Emit(OpCodes.Stfld, field);
                });

            // Initialize captured this
            if (capturedThis != null)
            {
                _body.Emit(OpCodes.Ldloc, closureClassInstance);
                _body.Emit(OpCodes.Ldarg_0);
                _body.Emit(OpCodes.Stfld, specializedClosureClass.IsGenericType ? TypeBuilder.GetField(specializedClosureClass, capturedThis) : capturedThis);
            }

            // Initialize captured params
            capturedParams.ForEach(pair =>
                {
                    _body.Emit(OpCodes.Ldloc, closureClassInstance);
                    int argIndex = pair.Key;
                    LoadArg(argIndex);
                    FieldInfo capturedParam = pair.Value;

                    capturedParam = specializedClosureClass.IsGenericType ? TypeBuilder.GetField(specializedClosureClass, capturedParam) : capturedParam;
                    _body.Emit(OpCodes.Stfld, capturedParam);
                });

            _body.Emit(OpCodes.Ldloc, closureClassInstance);
            closureClass.CreateType();
        }

        private static Dictionary<int, FieldBuilder> CreateFieldsForCapturedParams(TypeBuilder closureClass, IEnumerable<Tuple<RppParam, RType>> capturedParams)
        {
            return capturedParams.ToDictionary(pair => pair.Item1.Index,
                pair => closureClass.DefineField(pair.Item1.Name, pair.Item2.NativeType, FieldAttributes.Public));
        }

        private FieldBuilder CreatedCapturedThis(TypeBuilder closureClass)
        {
            return closureClass.DefineField("<>this", _typeBuilder, FieldAttributes.Public);
        }

        private static Dictionary<LocalBuilder, Tuple<FieldBuilder, Type>> CreateFieldsForCapturedVars(TypeBuilder closureClass,
            IEnumerable<Tuple<RppVar, RType>> capturedVars)
        {
            return capturedVars.ToDictionary(pair => pair.Item1.Builder,
                pair =>
                    {
                        Type varType = pair.Item2.NativeType;
                        Type refType = ClrVarCodegen.GetRefType(varType);
                        return Tuple.Create(closureClass.DefineField(pair.Item1.Name, refType, FieldAttributes.Public), varType);
                    });
        }

        public override void Visit(RppFieldSelector fieldSelector)
        {
            LoadField(fieldSelector.Field);
        }

        private void LoadField(RppFieldInfo field)
        {
            if (!_inSelector)
            {
                _body.Emit(OpCodes.Ldarg_0);
                if (ClosureContext?.CapturedThis != null)
                {
                    _body.Emit(OpCodes.Ldfld, ClosureContext.CapturedThis);
                }
            }

            if (IsInsideGetterOrSetter(field.Name))
            {
                _body.Emit(OpCodes.Ldfld, field.Native);
            }
            else
            {
                _body.Emit(OpCodes.Callvirt, field.NativeGetter);
            }
        }

        private bool IsInsideGetterOrSetter(string propertyName)
        {
            return _func != null &&
                   (RppMethodInfo.GetGetterAccessorName(propertyName) == _func.Name || RppMethodInfo.GetSetterAccessorName(propertyName) == _func.Name);
        }

        public override void Visit(RppAsInstanceOf node)
        {
            node.Value.Accept(this);
            _body.Emit(OpCodes.Isinst, node.Type.Value.NativeType);
        }

        public override void Visit(RppBreak node)
        {
            _body.Emit(OpCodes.Br, _blockExprExitLabels.Peek());
        }

        public override void Visit(RppPop node)
        {
            _body.Emit(OpCodes.Pop);
        }

        public override void Visit(RppThis node)
        {
            _body.Emit(OpCodes.Ldarg_0);
        }
    }
}