﻿using CSharpRpp.TypeSystem;

namespace CSharpRpp.Expr
{
    public class RppThrow : RppNode, IRppExpr
    {
        public RppType Type { get; private set; }
        public ResolvableType Type2 { get; private set; }

        public IRppExpr Expr;

        public RppThrow(IRppExpr expr)
        {
            Expr = expr;
            Type = RppPrimitiveType.UnitTy;
            Type2 = ResolvableType.UnitTy;
        }

        public override IRppNode Analyze(Symbols.SymbolTable scope)
        {
            Expr = Expr.Analyze(scope) as IRppExpr;
            return this;
        }

        public override void Accept(IRppNodeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}