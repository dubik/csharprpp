using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Emit;
using CSharpRpp.Symbols;
using CSharpRpp.TypeSystem;


namespace CSharpRpp
{
    public enum MutabilityFlag
    {
        MfVal,
        MfVar,
        MfUnspecified
    }

    [DebuggerDisplay("Field = {Name}: {Type}")]
    public class RppField : RppVar
    {
        private readonly IList<string> _modifiers;

        public string MangledName => RppFieldInfo.GetMangledName(Name);

        public RppField(MutabilityFlag mutabilityFlag, string name, IList<string> modifiers, ResolvableType type)
            : base(mutabilityFlag, name, type, RppEmptyExpr.Instance)
        {
            _modifiers = modifiers;
            IsLocalSemantic = false;
        }

        public RppField(MutabilityFlag mutabilityFlag, string name, IList<string> modifiers, ResolvableType type, IRppExpr initExpr)
            : base(mutabilityFlag, name, type, initExpr)
        {
            _modifiers = modifiers;
            IsLocalSemantic = false;
        }

        public new FieldBuilder Builder { get; set; }

        public RppFieldInfo FieldInfo { get; set; }

        public override void Accept(IRppNodeVisitor visitor)
        {
            visitor.Visit(this);
        }

        #region Equality

        protected bool Equals(RppField other)
        {
            return Equals(Name, other.Name) && Equals(Type, other.Type) && Equals(_modifiers, other._modifiers);
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

            return Equals((RppField) obj);
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }

        #endregion

        public void ResolveType(SymbolTable scope)
        {
            Type?.Resolve(scope);
        }
    }
}