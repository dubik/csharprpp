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
        public readonly HashSet<ObjectModifier> Modifiers;

        public string MangledName => RppFieldInfo.GetMangledName(Name);

        public new FieldBuilder Builder { get; set; }

        public RppFieldInfo FieldInfo { get; set; }
        public bool IsClassParam { get; set; }

        public RppField(MutabilityFlag mutabilityFlag, string name, HashSet<ObjectModifier> modifiers, ResolvableType type)
            : base(mutabilityFlag, name, type, RppEmptyExpr.Instance)
        {
            Modifiers = modifiers;
            IsLocalSemantic = false;
        }

        public RppField(MutabilityFlag mutabilityFlag, string name, HashSet<ObjectModifier> modifiers, ResolvableType type, IRppExpr initExpr)
            : base(mutabilityFlag, name, type, initExpr)
        {
            Modifiers = modifiers;
            IsLocalSemantic = false;
        }

        public override void Accept(IRppNodeVisitor visitor)
        {
            visitor.Visit(this);
        }

        #region Equality

        protected bool Equals(RppField other)
        {
            return Equals(Name, other.Name) && Equals(Type, other.Type) && Equals(Modifiers, other.Modifiers);
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