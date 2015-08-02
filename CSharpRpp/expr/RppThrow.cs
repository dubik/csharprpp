namespace CSharpRpp.Expr
{
    public class RppThrow : RppNode, IRppExpr
    {
        public RppType Type { get; private set; }

        public IRppExpr Expr;

        public RppThrow(IRppExpr expr)
        {
            Expr = expr;
            Type = RppPrimitiveType.UnitTy;
        }

        public override IRppNode Analyze(RppScope scope)
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