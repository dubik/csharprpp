using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace CSharpRpp
{
    public interface IRppFunc : IRppNode, IRppNamedNode
    {
        MethodInfo RuntimeFuncInfo { get; }

        RppType ReturnType { get; }
        Type RuntimeReturnType { get; }
        IRppParam[] Params { get; }

        bool IsStatic { get; set; }
        bool IsPublic { get; set; }
        bool IsAbstract { get; set; }

        void CodegenMethodStubs(TypeBuilder typeBuilder);
        void Codegen(CodegenContext ctx);
    }

    [DebuggerDisplay("Func: {Name}, Return: {ReturnType.ToString()}, Params: {Params.Length}")]
    public class RppFunc : RppNamedNode, IRppFunc
    {
        private IRppExpr _expr;
        private RppScope _scope;

        public RppType ReturnType { get; private set; }
        public Type RuntimeReturnType { get; private set; }
        public IRppParam[] Params { get; private set; }

        public bool IsStatic { get; set; }
        public bool IsPublic { get; set; }
        public bool IsAbstract { get; set; }

        #region Codegen

        private MethodBuilder _methodBuilder;

        #endregion

        public RppFunc(string name, IEnumerable<IRppParam> funcParams, RppType returnType, IRppExpr expr) : base(name)
        {
            Params = funcParams.ToArray();
            ReturnType = returnType;
            _expr = expr ?? new RppEmptyExpr();
        }

        public override void PreAnalyze(RppScope scope)
        {
            _scope = new RppScope(scope);

            Params.ForEach(scope.Add);
            _expr.PreAnalyze(_scope);
        }

        public override IRppNode Analyze(RppScope scope)
        {
            Params = NodeUtils.Analyze(_scope, Params).ToArray();
            _expr = NodeUtils.AnalyzeNode(_scope, _expr);

            RuntimeReturnType = ReturnType.Resolve(_scope);

            return this;
        }

        #region Codegen

        public MethodInfo RuntimeFuncInfo
        {
            get { return _methodBuilder.GetBaseDefinition(); }
        }

        public void CodegenMethodStubs(TypeBuilder typeBuilder)
        {
            _methodBuilder = typeBuilder.DefineMethod(Name, MethodAttributes.Public | MethodAttributes.Static);
        }

        public void Codegen(CodegenContext ctx)
        {
            _methodBuilder.SetReturnType(RuntimeReturnType);
            CodegenParams(Params, _methodBuilder);

            ILGenerator generator = _methodBuilder.GetILGenerator();
            _expr.Codegen(generator);

            if (RuntimeReturnType == typeof (void) && _expr.RuntimeType != typeof (void))
            {
                generator.Emit(OpCodes.Pop);
            }

            generator.Emit(OpCodes.Ret);
        }

        private static void CodegenParams(IEnumerable<IRppParam> paramList, MethodBuilder methodBuilder)
        {
            Type[] parameterTypes = paramList.Select(param => param.RuntimeType).ToArray();
            methodBuilder.SetParameters(parameterTypes);
            // paramList.ForEachWithIndex((index, param) => methodBuilder.DefineParameter(index, ParameterAttributes.In, param.Name));
        }

        #endregion

        public MethodInfo NativeMethodInfo()
        {
            return _methodBuilder.GetBaseDefinition();
        }
    }

    public interface IRppParam : IRppNamedNode, IRppExpr
    {
    }

    [DebuggerDisplay("{Type.ToString()} {Name} [{RuntimeType}]")]
    public class RppParam : RppNamedNode, IRppParam
    {
        public RppType Type { get; private set; }
        public Type RuntimeType { get; private set; }

        private readonly int _index;

        public RppParam(string name, int index, RppType type) : base(name)
        {
            Type = type;
            _index = index;
        }

        public override IRppNode Analyze(RppScope scope)
        {
            RuntimeType = Type.Resolve(scope);
            return this;
        }

        public void Codegen(ILGenerator generator)
        {
            generator.Emit(OpCodes.Ldarg, _index);
        }
    }
}