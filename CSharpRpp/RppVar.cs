using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Emit;
using CSharpRpp.Exceptions;
using CSharpRpp.Expr;
using CSharpRpp.Reporting;
using CSharpRpp.Symbols;
using CSharpRpp.TypeSystem;
using JetBrains.Annotations;

namespace CSharpRpp
{
    public class RppVar : RppMember
    {
        // TODO RppVar doesn't have a type as a statement, so it should communicate type of variable using another property
        public sealed override ResolvableType Type { get; protected set; }

        public MutabilityFlag MutabilityFlag { get; }

        [NotNull]
        public IRppExpr InitExpr { get; private set; }

        public LocalBuilder Builder { get; set; }

        protected bool IsLocalSemantic = true;

        public bool IsCaptured { get; private set; }

        public bool CanBeCaptured { get; private set; } // vars defined inside closure can't be captured

        public RppVar(MutabilityFlag mutability, [NotNull] string name, [NotNull] ResolvableType type, [NotNull] IRppExpr initExpr) : base(name)
        {
            Type = type;
            InitExpr = initExpr;
            MutabilityFlag = mutability;
        }

        public override void Accept(IRppNodeVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override IRppNode Analyze(SymbolTable scope, Diagnostic diagnostic)
        {
            if (InitExpr is RppEmptyExpr && IsLocalSemantic)
            {
                diagnostic.Error(102, "local variable must be initialized");
                return this;
            }

            // Don't capture variables declared inside closure
            CanBeCaptured = scope.GetEnclosingType()?.Name != RppClosure.TempClosureTypeName;

            // We have 2 cases when type is omited, so we need to get it from initializing expression
            // and when type is specified so we need to resolve it and if there is a closure, propagate that
            // to init expression
            if (Type.IsDefined())
            {
                Type.Resolve(scope);

                InitExpr = TypeInference.ReplaceUndefinedClosureTypesIfNeeded(InitExpr, Type, new List<RType>());
                InitExpr = (IRppExpr) InitExpr.Analyze(scope, diagnostic);
            }
            else
            {
                InitExpr = (IRppExpr) InitExpr.Analyze(scope, diagnostic);
                Type = InitExpr.Type;
            }

            if (IsLocalSemantic)
            {
                scope.AddLocalVar(Name, Type.Value, this);
            }

            if (!(InitExpr is RppEmptyExpr))
            {
                if (ImplicitCast.CanCast(InitExpr.Type.Value, Type.Value))
                {
                    InitExpr = ImplicitCast.CastIfNeeded(InitExpr, Type.Value);
                }
                else
                {
                    throw SemanticExceptionFactory.TypeMismatch(Token, Type.Value.Name, InitExpr.Type.Value.Name);
                }
            }

            return this;
        }

        public void MakeCaptured()
        {
            IsCaptured = true;
        }

        public override string ToString()
        {
            return $"{MutabilityFlagToString(MutabilityFlag)} {Name}: {Type}";
        }

        protected static string MutabilityFlagToString(MutabilityFlag mutability)
        {
            if (mutability == MutabilityFlag.MfVal)
            {
                return "val";
            }

            if (mutability == MutabilityFlag.MfVar)
            {
                return "var";
            }

            return "";
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
            if (obj.GetType() != GetType())
            {
                return false;
            }
            return Equals((RppVar) obj);
        }

        public override int GetHashCode()
        {
            int hash = 13;
            hash = hash + Name.GetHashCode();
            hash = hash * 7 + Type.GetHashCode();
            return hash;
        }

        #endregion
    }
}