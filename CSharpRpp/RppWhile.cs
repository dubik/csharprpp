using System;
using CSharpRpp.TypeSystem;

namespace CSharpRpp
{
    public class RppWhile : RppNode, IRppExpr
    {
        public IRppExpr Condition { get; private set; }
        public IRppNode Body { get; private set; }

        public RppType Type { get; }
        public RType Type2 { get; private set; }

        public RppWhile(IRppExpr condition, IRppNode body)
        {
            Type = RppPrimitiveType.UnitTy;
            Condition = condition;
            Body = body;
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