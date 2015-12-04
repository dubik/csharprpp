using CSharpRpp.Reporting;
using CSharpRpp.Symbols;
using CSharpRpp.TypeSystem;

namespace CSharpRpp.Expr
{
    public class RppThrow : RppNode, IRppExpr
    {
        public ResolvableType Type { get; }

        public IRppExpr Expr;

        public RppThrow(IRppExpr expr)
        {
            Expr = expr;
            Type = ResolvableType.UnitTy;
        }

        public override IRppNode Analyze(SymbolTable scope, Diagnostic diagnostic)
        {
            Expr = Expr.Analyze(scope, diagnostic) as IRppExpr;
            return this;
        }

        public override void Accept(IRppNodeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}