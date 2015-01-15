using System.Collections.Generic;

namespace CSharpRpp
{
    public class RppFunc : RppNamedNode
    {
        private IList<RppParam> _params;
        private RppType _returnType;
        private RppExpr _expr;
        private RppScope _scope;

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

            _returnType = _returnType.Resolve(_scope);

            return this;
        }
    }

    public class RppParam : RppNamedNode
    {
        private RppType _type;

        public RppParam(string name, RppType type) : base(name)
        {
            _type = type;
        }

        public override IRppNode Analyze(RppScope scope)
        {
            _type = _type.Resolve(scope);
            return this;
        }
    }
}