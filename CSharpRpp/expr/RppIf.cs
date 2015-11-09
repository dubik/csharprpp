using CSharpRpp.TypeSystem;
using JetBrains.Annotations;

namespace CSharpRpp.Expr
{
    public class RppIf : RppNode, IRppExpr
    {
        public ResolvableType Type { get; private set; }

        [NotNull]
        public IRppExpr Condition { get; private set; }

        [NotNull]
        public IRppExpr ThenExpr { get; private set; }

        [NotNull]
        public IRppExpr ElseExpr { get; private set; }

        public RppIf([NotNull] IRppExpr condition, [NotNull] IRppExpr thenExpr, [NotNull] IRppExpr elseExpr)
        {
            Condition = condition;
            ThenExpr = thenExpr;
            ElseExpr = elseExpr;
        }

        public override IRppNode Analyze(Symbols.SymbolTable scope)
        {
            Condition = (IRppExpr) Condition.Analyze(scope);
            ThenExpr = (IRppExpr) ThenExpr.Analyze(scope);
            ElseExpr = (IRppExpr) ElseExpr.Analyze(scope);

            Type = ThenExpr.Type;

            return this;
        }

        public override void Accept(IRppNodeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}