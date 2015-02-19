using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Emit;


namespace CSharpRpp
{
    public enum MutabilityFlag
    {
        MF_Val,
        MF_Var
    }

    [DebuggerDisplay("Field: {Type.ToString()} {Name}")]
    public class RppField : RppVar
    {
        private readonly IList<string> _modifiers;

        public RppField(MutabilityFlag mutabilityFlag, string name, IList<string> modifiers, RppType type)
            : base(mutabilityFlag, name, type, RppEmptyExpr.Instance)
        {
            _modifiers = modifiers;
        }

        public RppField(MutabilityFlag mutabilityFlag, string name, IList<string> modifiers, RppType type, IRppExpr initExpr)
            : base(mutabilityFlag, name, type, initExpr)
        {
            _modifiers = modifiers;
        }

        public new FieldBuilder Builder { get; set; }

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
    }
}