using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;

namespace CSharpRpp
{
    public class RppNew : RppNode, IRppExpr
    {
        public RppType Type { get; private set; }
        public Type RuntimeType { get; private set; }

        [NotNull]
        public RppClass RefClass { get; private set; }

        public IEnumerable<RppType> ArgumentTypes { get; private set; }

        private readonly string _typeName;

        public IEnumerable<IRppExpr> Args
        {
            get { return _constructorsParams.AsEnumerable(); }
        }

        private readonly IList<IRppExpr> _constructorsParams;

        public RppNew([NotNull] string typeName, [NotNull] IList<IRppExpr> constructorsParams)
        {
            _typeName = typeName;
            _constructorsParams = constructorsParams;
        }

        public override void Accept(IRppNodeVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override void PreAnalyze(RppScope scope)
        {
            NodeUtils.PreAnalyze(scope, _constructorsParams);

            var refClass = scope.Lookup(_typeName) as RppClass;
            Debug.Assert(refClass != null);
            RefClass = refClass;
        }

        public override IRppNode Analyze(RppScope scope)
        {
            Type = RppNativeType.Create(RefClass.RuntimeType);

            return this;
        }
    }
}