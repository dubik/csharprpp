using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace CSharpRpp
{
    class RppNew : RppNode, IRppExpr
    {
        public RppType Type { get; private set; }
        public Type RuntimeType { get; private set; }

        private readonly string _typeName;
        private readonly IList<IRppExpr> _constructorsParams;

        public RppNew(string typeName, IList<IRppExpr> constructorsParams)
        {
            _typeName = typeName;
            _constructorsParams = constructorsParams;
        }

        public override void PreAnalyze(RppScope scope)
        {
        }

        public override IRppNode Analyze(RppScope scope)
        {
            IRppNamedNode clazz = scope.Lookup(_typeName);
            return this;
        }

        public void Codegen(ILGenerator generator)
        {
            throw new NotImplementedException();
        }
    }
}