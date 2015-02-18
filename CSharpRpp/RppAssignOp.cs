using JetBrains.Annotations;

namespace CSharpRpp
{
    public class RppAssignOp : RppNode
    {
        [NotNull]
        public RppId Left { get; private set; }

        [NotNull]
        public IRppExpr Right { get; private set; }

        public RppAssignOp([NotNull] RppId left, [NotNull] IRppExpr right)
        {
            Left = left;
            Right = right;
        }

        public override void Accept(IRppNodeVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override void PreAnalyze(RppScope scope)
        {
            Left.PreAnalyze(scope);
            Right.PreAnalyze(scope);
        }

        public override IRppNode Analyze(RppScope scope)
        {
            Left.Analyze(scope);
            Right.Analyze(scope);

            return this;
        }
    }
}