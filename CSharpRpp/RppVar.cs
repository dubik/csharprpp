﻿using System;
using System.Diagnostics;
using System.Reflection.Emit;
using JetBrains.Annotations;

namespace CSharpRpp
{
    public class RppVar : RppMember
    {
        public override RppType Type { get; protected set; }

        public override Type RuntimeType { get; protected set; }

        [NotNull]
        public IRppExpr InitExpr { get; private set; }

        private LocalBuilder _builder;

        public RppVar(MutabilityFlag mutability, [NotNull] string name, [NotNull] RppType type, [NotNull] IRppExpr initExpr) : base(name)
        {
            Type = type;

            InitExpr = initExpr;
        }

        public override void Accept(IRppNodeVisitor visitor)
        {
            InitExpr.Accept(visitor);
            visitor.Visit(this);
        }

        public override void PreAnalyze(RppScope scope)
        {
            InitExpr.PreAnalyze(scope);
        }

        public override IRppNode Analyze(RppScope scope)
        {
            InitExpr.Analyze(scope);

            var resolvedType = Type.Resolve(scope);
            Debug.Assert(resolvedType != null);
            RuntimeType = resolvedType;

            return this;
        }

        public override void Codegen(ILGenerator generator)
        {
            _builder = generator.DeclareLocal(RuntimeType);

            if (!(InitExpr is RppEmptyExpr))
            {
                InitExpr.Codegen(generator);
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