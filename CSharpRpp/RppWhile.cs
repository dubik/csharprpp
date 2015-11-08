using System;
using CSharpRpp.TypeSystem;

namespace CSharpRpp
{
    public class RppWhile : RppNode, IRppExpr
    {
        public IRppExpr Condition { get; private set; }
        public IRppNode Body { get; private set; }

        public RppType Type { get; }
        public ResolvableType Type2 { get; private set; }

        public RppWhile(IRppExpr condition, IRppNode body)
        {
            Type = RppPrimitiveType.UnitTy;
            Condition = condition;
            Body = body;
        }

        public override IRppNode Analyze(Symbols.SymbolTable scope)
        {
            Condition = (IRppExpr) Condition.Analyze(scope);
            Body = Body.Analyze(scope);

            if (!Equals(Condition.Type2, ResolvableType.BooleanTy))
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