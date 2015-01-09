using System;
using System.Collections.Generic;

namespace CSharpRpp
{
    public class RppFunc : RppNamedNode
    {
        private IList<RppParam> _params;
        private RppType _returnType;
        private RppExpr _expr;

        public RppFunc(string name, IList<RppParam> @params, RppType returnType, RppExpr expr) : base(name)
        {
            _params = @params;
            _returnType = returnType;
            _expr = expr;
        }

        public override void PreAnalyze(RppScope scope)
        {
            throw new NotImplementedException();
        }

        public override IRppNode Analyze(RppScope scope)
        {
            throw new NotImplementedException();
        }
    }

    public class RppParam : RppNamedNode
    {
        private RppType _type;

        public RppParam(string name, RppType type)
            : base(name)
        {
            _type = type;
        }

        public override void PreAnalyze(RppScope scope)
        {
            throw new NotImplementedException();
        }

        public override IRppNode Analyze(RppScope scope)
        {
            throw new NotImplementedException();
        }
    }
}