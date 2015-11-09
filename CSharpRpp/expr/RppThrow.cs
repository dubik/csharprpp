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