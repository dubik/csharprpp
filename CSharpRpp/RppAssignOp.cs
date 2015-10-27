using JetBrains.Annotations;

namespace CSharpRpp
{
    public class RppAssignOp : BinOp
    {
        public RppAssignOp([NotNull] IRppExpr left, [NotNull] IRppExpr right) : base("=", left, right)
        {
            Type = RppPrimitiveType.UnitTy;
        }

        public override void Accept(IRppNodeVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override IRppNode Analyze(Symbols.SymbolTable scope)
        {
            base.Analyze(scope);

            if (!Equals(Left.Type, Right.Type))
            {
                if (!Right.Type.IsSubclassOf(Left.Type))
                {
                    throw new TypeMismatchException(Right.Token, Right.Type.Runtime.ToString(), Left.Type.Runtime.ToString());
                }
            }

            return this;
        }
    }
}