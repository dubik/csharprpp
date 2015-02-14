using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using JetBrains.Annotations;

namespace CSharpRpp
{
    class RppNew : RppNode, IRppExpr
    {
        public RppType Type { get; private set; }
        public Type RuntimeType { get; private set; }

        private readonly string _typeName;
        private readonly IList<IRppExpr> _constructorsParams;
        private RppClass _clazz;

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

            _clazz = scope.Lookup(_typeName) as RppClass;
            Debug.Assert(_clazz != null);
        }

        public override IRppNode Analyze(RppScope scope)
        {
            return this;
        }

        public void Codegen(ILGenerator generator)
        {
            ConstructorInfo constructorInfo = _clazz.RuntimeType.GetConstructor(System.Type.EmptyTypes);
            Debug.Assert(constructorInfo != null, "constructorInfo != null");
            generator.Emit(OpCodes.Newobj, constructorInfo);
        }
    }
}