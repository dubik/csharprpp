using System;
using System.Collections.Generic;
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
        }

        #endregion
    }

    public class RppParam : RppNamedNode
    {
        private readonly RppType _type;
        private Type _runtimeType;

        public RppParam(string name, RppType type) : base(name)
        {
            _type = type;
        }

        public override IRppNode Analyze(RppScope scope)
        {
            _runtimeType = _type.Resolve(scope);
            return this;
        }
    }
}