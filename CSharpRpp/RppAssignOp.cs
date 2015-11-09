using CSharpRpp.TypeSystem;
using JetBrains.Annotations;

namespace CSharpRpp
{
    public class RppAssignOp : BinOp
    {
        public RppAssignOp([NotNull] IRppExpr left, [NotNull] IRppExpr right) : base("=", left, right)
        {
            Type2 = ResolvableType.UnitTy;
        }

        public override void Accept(IRppNodeVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override IRppNode Analyze(Symbols.SymbolTable scope)
        {
            base.Analyze(scope);

            if (!Equals(Left.Type2, Right.Type2))
            {
                if (!Right.Type2.Value.IsSubclassOf(Left.Type2.Value))
                {
                    throw new TypeMismatchException(Right.Token, Right.Type2.Value.ToString(), Left.Type2.Value.ToString());
                }
            }

            return this;
        }
    }
}