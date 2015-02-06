using System.Collections.Generic;
using System.Diagnostics;


namespace CSharpRpp
{
    public enum MutabilityFlag
    {
        MF_Val,
        MF_Var
    }

    [DebuggerDisplay("{_type.ToString()} {Name}")]
    public class RppField : RppNamedNode
    {
        private readonly IList<string> _modifiers;
        private readonly RppType _type;
        private readonly IRppExpr _initExpr;

        public RppField(MutabilityFlag mutabilityFlag, string name, IList<string> modifiers, RppType type) : base(name)
        {
            _modifiers = modifiers;
            _type = type;
        }

        public RppField(MutabilityFlag mutabilityFlag, string name, IList<string> modifiers, RppType type, IRppExpr initExpr)
            : base(name)
        {
            _modifiers = modifiers;
            _type = type;
            _initExpr = initExpr;
        }

        public override void PreAnalyze(RppScope scope)
        {
        }

        public override IRppNode Analyze(RppScope scope)
        {
            return this;
        }

        public void Codegen(CodegenContext ctx)
        {
        }

        protected bool Equals(RppField other)
        {
            return Equals(Name, other.Name) && Equals(_type, other._type);
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
            return Equals((RppField) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((_modifiers != null ? _modifiers.GetHashCode() : 0) * 397) ^ (_type != null ? _type.GetHashCode() : 0);
            }
        }
    }
}