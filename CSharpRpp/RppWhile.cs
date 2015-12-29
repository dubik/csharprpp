using System;
using CSharpRpp.Reporting;
using CSharpRpp.Symbols;
using CSharpRpp.TypeSystem;

namespace CSharpRpp
{
    public class RppWhile : RppNode, IRppExpr
    {
        public IRppExpr Condition { get; private set; }
        public IRppNode Body { get; private set; }

        public ResolvableType Type { get; private set; }

        public RppWhile(IRppExpr condition, IRppNode body)
        {
            Condition = condition;
            Body = body;
            Type = ResolvableType.UnitTy;
        }

        public override IRppNode Analyze(SymbolTable scope, Diagnostic diagnostic)
        {
            Condition = (IRppExpr) Condition.Analyze(scope, diagnostic);
            Body = Body.Analyze(scope, diagnostic);

            if (!Equals(Condition.Type, ResolvableType.BooleanTy))
            {
                throw new Exception("Condition should be boolean not " + Condition.Type);
            }

            return this;
        }

        public override void Accept(IRppNodeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}