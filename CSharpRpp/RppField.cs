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

        public RppField(MutabilityFlag mutabilityFlag, string name, IList<string> modifiers, RppType type) : base(name)
        {
            _modifiers = modifiers;
            _type = type;
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
    }
}