using System;
using System.Collections.Generic;

namespace CSharpRpp
{
    public class RppField : RppNamedNode
    {
        private readonly IList<string> _modifiers;
        private RppType _type;

        public RppField(string name, IList<string> modifiers, RppType type) : base(name)
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