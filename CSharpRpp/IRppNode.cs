using Antlr.Runtime;
using JetBrains.Annotations;

namespace CSharpRpp
{
    public interface IRppNode
    {
        IToken Token { get; }

        [NotNull]
        IRppNode Analyze([NotNull] RppScope scope);

        void Accept([NotNull] IRppNodeVisitor visitor);
    }

    public interface IRppStatementExpr : IRppNode
    {
    }

    public interface IRppExpr : IRppStatementExpr
    {
        [NotNull]
        RppType Type { get; }
    }

    public class RppNode : IRppNode
    {
        public IToken Token { get; set; }

        public virtual IRppNode Analyze(RppScope scope)
        {
            return this;
        }

        public virtual void Accept(IRppNodeVisitor visitor)
        {
        }
    }
}