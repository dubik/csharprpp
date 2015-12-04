using CSharpRpp.Reporting;
using CSharpRpp.Symbols;
using CSharpRpp.TypeSystem;
using JetBrains.Annotations;

namespace CSharpRpp
{
    public class RppAssignOp : BinOp
    {
        public RppAssignOp([NotNull] IRppExpr left, [NotNull] IRppExpr right) : base("=", left, right)
        {
            Type = ResolvableType.UnitTy;
        }

        public override void Accept(IRppNodeVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override IRppNode Analyze(SymbolTable scope, Diagnostic diagnostic)
        {
            base.Analyze(scope, diagnostic);

            if (!Equals(Left.Type, Right.Type))
            {
                if (!Right.Type.Value.IsSubclassOf(Left.Type.Value))
                {
                    throw new TypeMismatchException(Right.Token, Right.Type.Value.ToString(), Left.Type.Value.ToString());
                }
            }

            return this;
        }
    }
}