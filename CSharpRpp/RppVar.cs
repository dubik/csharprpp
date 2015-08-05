using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using CSharpRpp.Expr;
using JetBrains.Annotations;
using Mono.Cecil;

namespace CSharpRpp
{
    public class RppVar : RppMember
    {
        public override sealed RppType Type { get; protected set; }

        public MutabilityFlag MutabilityFlag { get; private set; }

        [NotNull]
        public IRppExpr InitExpr { get; private set; }

        public LocalBuilder Builder { get; set; }

        protected bool AddToScope = true;

        public RppVar(MutabilityFlag mutability, [NotNull] string name, [NotNull] RppType type, [NotNull] IRppExpr initExpr) : base(name)
        {
            Type = type;
            InitExpr = initExpr;
            MutabilityFlag = mutability;
        }

        public override void Accept(IRppNodeVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override IRppNode Analyze(RppScope scope)
        {
            if (AddToScope)
            {
                scope.Add(this);
            }

            InitExpr = ReplaceUndefinedClosureTypesIfNeeded(InitExpr);

            InitExpr = (IRppExpr) InitExpr.Analyze(scope);

            // ReSharper disable once PossibleUnintendedReferenceComparison
            if (Type == RppUndefinedType.Instance)
            {
                if (InitExpr is RppEmptyExpr)
                {
                    throw new Exception("Type is not specified but also initializing expression is missing, I give up");
                }

                Type = InitExpr.Type;
            }
            else
            {
                var resolvedType = Type.Resolve(scope);
                Debug.Assert(resolvedType != null);
                Type = resolvedType;
            }

            if (!(InitExpr is RppEmptyExpr))
            {
                InitExpr = ImplicitCast.CastIfNeeded(InitExpr, Type.Runtime);
            }

            return this;
        }


        [NotNull]
        private IRppExpr ReplaceUndefinedClosureTypesIfNeeded([NotNull] IRppExpr expr)
        {
            if (expr is RppClosure)
            {
                RppClosure closure = (RppClosure) expr;
                var hasUndefinedClosureBinding = closure.Bindings.Any(b => b.Type.IsUndefined());
                if (Type.IsDefined() && hasUndefinedClosureBinding)
                {
                    if (Type is RppGenericType)
                    {
                        RppGenericType varType = (RppGenericType) Type;
                        var newBindings = varType.Params.Zip(closure.Bindings, (varTypeGenArg, binding) => binding.CloneWithNewType(varTypeGenArg)).ToList();
                        return new RppClosure(newBindings, closure.Expr);
                    }

                    throw new NotSupportedException("Only RppGenericType is supported at the moment");
                }
            }

            return expr;
        }

        #region Equality

        protected bool Equals(RppVar other)
        {
            Debug.Assert(other.Type != null, "other.Type != null");
            return Name.Equals(other.Name) && Type.Equals(other.Type);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (obj.GetType() != this.GetType())
            {
                return false;
            }
            return Equals((RppVar) obj);
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}