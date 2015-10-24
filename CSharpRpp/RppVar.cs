﻿using System;
using System.Diagnostics;
using System.Reflection.Emit;
using CSharpRpp.Expr;
using CSharpRpp.TypeSystem;
using JetBrains.Annotations;

namespace CSharpRpp
{
    public class RppVar : RppMember
    {
        public override sealed RppType Type { get; protected set; }
        public override sealed ResolvableType Type2 { get; protected set; }

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

        public RppVar(MutabilityFlag mutability, [NotNull] string name, [NotNull] ResolvableType type, [NotNull] IRppExpr initExpr) : base(name)
        {
            Type2 = type;
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

            InitExpr = TypeInference.ReplaceUndefinedClosureTypesIfNeeded(InitExpr, Type);

            InitExpr = (IRppExpr) InitExpr.Analyze(scope);

            // ReSharper disable once PossibleUnintendedReferenceComparison
            if (Type2 == ResolvableType.UndefinedTy)
            {
                if (InitExpr is RppEmptyExpr)
                {
                    throw new Exception("Type is not specified but also initializing expression is missing, I give up");
                }

                Type2 = InitExpr.Type2;
            }
            else
            {
                Type2.Resolve(scope);
            }

            if (!(InitExpr is RppEmptyExpr))
            {
                InitExpr = ImplicitCast.CastIfNeeded(InitExpr, Type2.Value);
            }

            return this;
        }

        #region Equality

        protected bool Equals(RppVar other)
        {
            Debug.Assert(other.Type2 != null, "other.Type != null");
            return Name.Equals(other.Name) && Type2.Equals(other.Type2);
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