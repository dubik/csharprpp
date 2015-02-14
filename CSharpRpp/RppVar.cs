using System;
using System.Diagnostics;
using System.Reflection.Emit;
using JetBrains.Annotations;

namespace CSharpRpp
{
    public class RppVar : RppNamedNode, IRppStatementExpr
    {
        [NotNull]
        public RppType Type { get; private set; }

        [NotNull]
        public Type RuntimeType { get; private set; }

        private readonly IRppExpr _initExpr;

        private LocalBuilder _builder;

        public RppVar(MutabilityFlag mutability, [NotNull] string name, [NotNull] RppType type, [NotNull] IRppExpr initExpr) : base(name)
        {
            Type = type;

            _initExpr = initExpr;
        }

        public override void Accept(IRppNodeVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override void PreAnalyze(RppScope scope)
        {
            _initExpr.PreAnalyze(scope);
        }

        public override IRppNode Analyze(RppScope scope)
        {
            _initExpr.Analyze(scope);

            var resolvedType = Type.Resolve(scope);
            Debug.Assert(resolvedType != null);
            RuntimeType = resolvedType;

            return this;
        }

        public void Codegen(ILGenerator generator)
        {
            _builder = generator.DeclareLocal(RuntimeType);

            if (!(_initExpr is RppEmptyExpr))
            {
                _initExpr.Codegen(generator);
                generator.Emit(OpCodes.Stloc, _builder.LocalIndex);
            }
        }

        #region Equality

        protected bool Equals(RppVar other)
        {
            Debug.Assert(other.Type != null, "other.Type != null");
            return Name.Equals(other.Name) && Type.Equals(other.Type);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((RppVar) obj);
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}