using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;
using Mono.Collections.Generic;

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

        private readonly IList<RppVariantTypeParam> _typeArgs;

        public RppNew([NotNull] string typeName, [NotNull] IList<IRppExpr> constructorsParams)
        {
            _typeName = typeName;
            _constructorsParams = constructorsParams;
            _typeArgs = ReadOnlyCollection<RppVariantTypeParam>.Empty;
        }

        public RppNew([NotNull] string typeName, [NotNull] IList<IRppExpr> constructorsParams, [NotNull] IList<RppVariantTypeParam> typeArgs)
        {
            _typeName = typeName;
            _constructorsParams = constructorsParams;
            _typeArgs = typeArgs;
        }

        public override void Accept(IRppNodeVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override IRppNode Analyze(RppScope scope)
        {
            NodeUtils.Analyze(scope, _constructorsParams);

            var refClass = scope.Lookup(_typeName) as RppClass;
            Debug.Assert(refClass != null);
            RefClass = refClass;

            Type = RppNativeType.Create(RefClass.RuntimeType);

            return this;
        }
    }
}