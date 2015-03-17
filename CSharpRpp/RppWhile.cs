using System;

namespace CSharpRpp
{
    public class RppWhile : RppNode, IRppExpr
    {
        public IRppExpr Condition { get; private set; }
        public IRppNode Body { get; private set; }

        public RppType Type { get; private set; }

        public RppWhile(IRppExpr condition, IRppNode body)
        {
            Type = RppNativeType.Create(typeof(void));
            Condition = condition;
            Body = body;
        }

        public override void PreAnalyze(RppScope scope)
        {
            Condition.PreAnalyze(scope);
            Body.PreAnalyze(scope);
        }

        public override IRppNode Analyze(RppScope scope)
        {
            Condition = (IRppExpr) Condition.Analyze(scope);
            Body = Body.Analyze(scope);

            if (Condition.Type.Runtime != typeof (bool))
            {
                throw new Exception("Condition should be boolean not " + Condition.Type.Runtime);
            }

            return this;
        }

        public override void Accept(IRppNodeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}