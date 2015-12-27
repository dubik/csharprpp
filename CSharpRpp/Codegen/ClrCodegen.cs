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
    class ClrCodegen : RppNodeVisitor
    {
        private ILGenerator _body;

        // inst.myField - selector with RppId, so when it generates field access it wants to
        // load 'this', which is wrong, because 'inst' already loaded
        private bool _inSelector;

        private TypeBuilder _typeBuilder;

        private static readonly Dictionary<string, OpCode> _arithmToIl = new Dictionary<string, OpCode>
        {
            {"+", OpCodes.Add},
            {"-", OpCodes.Sub},
            {"*", OpCodes.Mul},
            {"/", OpCodes.Div}
        };

        private static readonly Regex _typeExcSplitter = new Regex(@"'(.*?)'", RegexOptions.Singleline);

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
        public ClrCodegen(ILGenerator body) : this(null, body)
        {
        }

        public ClrCodegen(TypeBuilder builder, ILGenerator body)
        {
            _typeBuilder = builder;
            _body = body;
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
                MatchCollection groups = _typeExcSplitter.Matches(exception.Message);
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

            if (funcReturnType.Equals(RppTypeSystem.UnitTy) && !expressionType.Equals(RppTypeSystem.UnitTy))
            {
                generator.Emit(OpCodes.Pop);
            }

            generator.Emit(OpCodes.Ret);
        }

        public override void Visit(RppVar node)
        {
            node.Builder = _body.DeclareLocal(node.Type.Value.NativeType);

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

        private readonly Dictionary<string, OpCode> _logToIl = new Dictionary<string, OpCode>
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
                _logicalTemp = _body.DeclareLocal(typeof (bool));
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
            LocalBuilder tempVar = _body.DeclareLocal(typeof (bool));
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
                ConstructorInfo constructor = typeof (object).GetConstructor(Type.EmptyTypes);
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
                            _body.Emit(OpCodes.Ldarg_0); // load 'this'
                        }
                    }

                    // Create own code generator for arguments because they can have RppSelectors which may interfer with already existing RppSelector
                    // myField.CallFunc(anotherInstanceFunc()) would set _inSelector to true and no 'this' will be loaded
                    ClrCodegen codegen = new ClrCodegen(_typeBuilder, _body);
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
                        method = method.MakeGenericMethod(methodTypeArgs);
                    }

                    _body.Emit(OpCodes.Callvirt, method);
                }
            }
        }

        private void CodegenForStub(RppMethodInfo function)
        {
            if (function.DeclaringType.Name == "Array")
            {
                switch (function.Name)
                {
                    case "length":
                        _body.Emit(OpCodes.Ldlen);
                        break;

                    case "apply":
                    {
                        RType elementType = function.DeclaringType.GenericArguments.First();
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
                        RType elementType = function.DeclaringType.GenericArguments.First();
                        OpCode ldOpCode = ClrCodegenUtils.ArrayStoreOpCodeByType(elementType.NativeType);
                        _body.Emit(ldOpCode);
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
                _body.Emit(OpCodes.Call, typeof (object).GetConstructor(new Type[0]));
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
                if (!_inSelector)
                {
                    _body.Emit(OpCodes.Ldarg_0);
                }

                FieldInfo cilField = node.Field.Native;
                Debug.Assert(cilField != null, "cilField != null");
                _body.Emit(OpCodes.Ldfld, cilField);
            }
            else if (node.IsVar)
            {
                _body.Emit(OpCodes.Ldloc, ((RppVar) node.Ref).Builder);
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
                    if (node.Index < 256)
                    {
                        _body.Emit(OpCodes.Ldarg_S, (byte) node.Index);
                    }
                    else
                    {
                        _body.Emit(OpCodes.Ldarg, (short) node.Index);
                    }
                    break;
            }
        }

        public override void Visit(RppNew node)
        {
            node.Args.ForEach(arg => arg.Accept(this));
            RppMethodInfo constructor = node.Constructor;

            ConstructorInfo constructorInfo = constructor.Native as ConstructorInfo;

            _body.Emit(OpCodes.Newobj, constructorInfo);
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
                ClrCodegenUtils.StoreLocal(var.Builder, _body);
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
            Type[] argTypes = node.Bindings.Select(p => p.Type.Value.NativeType).ToArray();
            Type parentType = node.Type.Value.NativeType;
            TypeBuilder closureClass = _typeBuilder.DefineNestedType("c__Closure" + (_closureId++),
                TypeAttributes.AutoClass | TypeAttributes.AnsiClass | TypeAttributes.Sealed | TypeAttributes.NestedPrivate);

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
            Type[] closureGenericArguments = closureSignature.Where(t => t.IsGenericParameter).ToArray();
            string[] closureGenericArgumentsNames = closureGenericArguments.Select(arg => "T" + arg.Name).ToArray();
            if (closureGenericArgumentsNames.Length > 0)
            {
                GenericTypeParameterBuilder[] gpBuilders = closureClass.DefineGenericParameters(closureGenericArgumentsNames);
                var targetSignature = closureSignature.Select(t =>
                    {
                        if (t.IsGenericParameter)
                        {
                            Type closureGenericArgument = gpBuilders[t.GenericParameterPosition];
                            return closureGenericArgument;
                        }
                        return t;
                    }).ToArray();

                Type genericTypeDef = parentType.GetGenericTypeDefinition();
                parentType = genericTypeDef.MakeGenericType(targetSignature);
                returnType = targetSignature.Last();
                argTypes = targetSignature.Take(targetSignature.Length - 1).ToArray();
            }

            //closureClass.SetParent(parentType);
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
            ClrCodegen codegen = new ClrCodegen(body);
            node.Expr.Accept(codegen);
            body.Emit(OpCodes.Ret);

            ConstructorInfo defaultClosureConstructor = closureClass.DefineDefaultConstructor(MethodAttributes.Public);
            if (closureGenericArgumentsNames.Length > 0)
            {
                Type spec = closureClass.MakeGenericType(closureSignature);
                defaultClosureConstructor = TypeBuilder.GetConstructor(spec, defaultClosureConstructor);
            }

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

            FieldInfo field = fieldSelector.Field.Native;

            Debug.Assert(field != null, "field != null");
            _body.Emit(OpCodes.Ldfld, field);
        }
    }
}