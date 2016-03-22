using CSharpRpp.TypeSystem;

namespace CSharpRpp.Expr
{
    public class RppPop : RppNode, IRppExpr
    {
        public static RppPop Instance = new RppPop();

        public ResolvableType Type { get; }

        protected RppPop()
        {
            Type = ResolvableType.NothingTy;
        }

        public override void Accept(IRppNodeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}