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
    internal class ClrCodegen : RppNodeVisitor
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
            _typeBuilder = node.Type2.NativeType as TypeBuilder;
            Debug.Assert(_typeBuilder != null, "_typeBuilder != null");
            _closureId = 1;

            if (node.IsObject())
            {
                FieldInfo instanceField = node.InstanceField.FieldInfo.Native;
                CreateStaticConstructor(_typeBuilder, instanceField, node);
            }
        }

        private static void CreateStaticConstructor(TypeBuilder typeBuilder, FieldInfo instanceField, RppClass obj)
        {
            ConstructorBuilder staticConstructor = typeBuilder.DefineTypeInitializer();
            ILGenerator body = staticConstructor.GetILGenerator();
            ConstructorInfo constructor = obj.Type2.Constructors[0].Native as ConstructorInfo;
            body.Emit(OpCodes.Newobj, constructor);
            body.Emit(OpCodes.Stsfld, instanceField);
            body.Emit(OpCodes.Ret);
        }

        private readonly Regex _typeExcSplitter = new Regex(@"'(.*?)'", RegexOptions.Singleline);

        public override void VisitExit(RppClass node)
        {
            TypeBuilder clazz = node.Type2.NativeType as TypeBuilder;
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
            RType funcReturnType = func.ReturnType2.Value;
            RType expressionType = func.Expr.Type2.Value;

            if (funcReturnType.Equals(RppTypeSystem.UnitTy) && !expressionType.Equals(RppTypeSystem.UnitTy))
            {
                generator.Emit(OpCodes.Pop);
            }

            generator.Emit(OpCodes.Ret);
        }

        public override void Visit(RppVar node)
        {
            node.Builder = _body.DeclareLocal(node.Type2.Value.NativeType);

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
            var arrayType = node.Type2.Value.NativeType;
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
                    if (!_inSelector && !rppMethodInfo.IsStatic)
                    {
                        _body.Emit(OpCodes.Ldarg_0); // load 'this'
                    }

                    if (rppMethodInfo.IsStatic)
                    {
                        Debug.Assert(rppMethodInfo.Native.DeclaringType != null, "rppMethodInfo.Native.DeclaringType != null");
                        // TODO Fix this, this is weird also if we are inside object, we shouldn't probably load instance through the field because we have it in the parameter
                        var instanceField = rppMethodInfo.DeclaringType.Fields.First(f => f.Name == "_instance");
                        Debug.Assert(instanceField != null, "instanceField != null");
                        _body.Emit(OpCodes.Ldsfld, instanceField.Native);
                    }

                    node.Args.ForEach(arg => arg.Accept(this));

                    //if (node.Function.IsStub)
                    //{
                    // Not real functions, like Array.length
                    //    CodegenForStub(node.Function);
                    //}
                    //else
                    //{
                    MethodInfo method = rppMethodInfo.Native as MethodInfo;

                    if (method == null) // This is a stub, so generate code for it
                    {
                        CodegenForStub(rppMethodInfo);
                        return;
                    }

                    if (node.TargetType is RppGenericObjectType)
                    {
                        RppGenericObjectType genericObjectType = (RppGenericObjectType) node.TargetType;
                        try
                        {
                            // Getting a specialized method from generic object. genericObjectType.Runtime has specialized type
                            // and 'method' contains generic version
                            method = TypeBuilder.GetMethod(genericObjectType.Runtime, method);
                        }
                        catch
                        {
                            // Above works only for TypeBuilders, for C# imported types it throws an exception, so getting
                            // method which has the same name and amount of parameters
                            method = node.TargetType.Runtime
                                .GetMethods().FirstOrDefault(x => x.Name == method.Name && x.GetParameters().Length == method.GetParameters().Length);
                        }
                    }

                    Debug.Assert(method != null, "method != null");

                    if (method.IsGenericMethod)
                    {
                        var methodTypeArgs = node.TypeArgs.Select(t => t.Runtime).ToArray();
                        method = method.MakeGenericMethod(methodTypeArgs);
                    }

                    _body.Emit(OpCodes.Callvirt, method);
                    //}
                }
            }
        }

        private void CodegenForStub(RppMethodInfo function)
        {
            if (function.DeclaringType.Name == "Array" && function.Name == "length")
            {
                _body.Emit(OpCodes.Ldlen);
                return;
            }

            throw new NotImplementedException("Other funcs are not implemented");
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

            if (node.BaseConstructor == null)
            {
                _body.Emit(OpCodes.Call, typeof (object).GetConstructor(new Type[0]));
                return;
            }

            ConstructorInfo constructor = node.BaseConstructor.Native as ConstructorInfo;

            /*
            if (node.BaseClassType.Runtime.IsGenericType)
            {
                constructor = TypeBuilder.GetConstructor(node.BaseClassType.Runtime, constructor);
            }
            */

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
            if (node.IsField)
            {
                if (!_inSelector)
                {
                    _body.Emit(OpCodes.Ldarg_0);
                }


                FieldInfo cilField = node.Field.Native;
                // TODO this is wierd, we should have all info in the fieldSelector
                /*
                if (_selectorType != null && _selectorType.Runtime.IsGenericType)
                {
                    cilField = TypeBuilder.GetField(_selectorType.Runtime, field.Builder);
                }
                */

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

        public override void Visit(RppField node)
        {
            // TODO we probably don't need to generate code for field because it should be already generated
            //_body.Emit(OpCodes.Ldarg_0);
            //_body.Emit(OpCodes.Ldfld, node.Builder);
        }

        public override void Visit(RppBox node)
        {
            node.Expression.Accept(this);
            _body.Emit(OpCodes.Box, node.Expression.Type2.Value.NativeType);
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

            FieldInfo field = fieldSelector.Field.Native;
            //Type targetType = fieldSelector.ClassType.Runtime;

            /*
            if (field.FieldType.ContainsGenericParameters)
            {
                field = TypeBuilder.GetField(targetType, field);
            }
            */

            Debug.Assert(field != null, "field != null");
            _body.Emit(OpCodes.Ldfld, field);
        }
    }
}