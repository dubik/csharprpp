using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace CSharpRpp
{
    public class RppFunc : RppNamedNode
    {
        private IList<RppParam> _params;
        private readonly RppType _returnType;
        private RppExpr _expr;
        private RppScope _scope;

        #region Codegen

        private MethodBuilder _methodBuilder;
        private Type _runtimeReturnType;

        #endregion

        public RppFunc(string name, IList<RppParam> funcParams, RppType returnType, RppExpr expr) : base(name)
        {
            _params = funcParams;
            _returnType = returnType;
            _expr = expr;
        }

        public override void PreAnalyze(RppScope scope)
        {
            _scope = new RppScope(scope);

            NodeUtils.PreAnalyze(_scope, _params);
            _expr.PreAnalyze(_scope);
        }

        public override IRppNode Analyze(RppScope scope)
        {
            _params = NodeUtils.Analyze(_scope, _params);
            _expr = NodeUtils.Analyze(_scope, _expr);

            _runtimeReturnType = _returnType.Resolve(_scope);

            return this;
        }

        #region Codegen

        public void CodegenMethodStubs(TypeBuilder typeBuilder)
        {
            _methodBuilder = typeBuilder.DefineMethod(Name, MethodAttributes.Public);
        }

        public override void Codegen(CodegenContext ctx)
        {
            CodegenParams(_params, _methodBuilder);

            _methodBuilder.SetReturnType(_runtimeReturnType);

            _methodBuilder.GetILGenerator().Emit(OpCodes.Ret);
        }

        private static void CodegenParams(IList<RppParam> paramList, MethodBuilder methodBuilder)
        {
            Type[] parameterTypes = paramList.Select(param => param.RuntimeType).ToArray();
            methodBuilder.SetParameters(parameterTypes);
            paramList.ForEachWithIndex((index, param) => methodBuilder.DefineParameter(index + 1, ParameterAttributes.In, param.Name));
        }

        #endregion
    }

    public class RppParam : RppNamedNode
    {
        public Type RuntimeType { get; set; }
        private readonly RppType _type;

        public RppParam(string name, RppType type) : base(name)
        {
            _type = type;
        }

        public override IRppNode Analyze(RppScope scope)
        {
            RuntimeType = _type.Resolve(scope);
            return this;
        }
    }
}