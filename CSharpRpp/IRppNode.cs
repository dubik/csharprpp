using Antlr.Runtime;
using CSharpRpp.Symbols;
using CSharpRpp.TypeSystem;
using JetBrains.Annotations;

namespace CSharpRpp
{
    public interface IRppNode
    {
        IToken Token { get; }

        [NotNull]
        IRppNode Analyze([NotNull] SymbolTable scope);

        void Accept([NotNull] IRppNodeVisitor visitor);
    }

    public interface IRppStatementExpr : IRppNode
    {
    }

    public interface IRppExpr : IRppStatementExpr
    {
        ResolvableType Type { get; }
    }

    public class RppNode : IRppNode
    {
        public IToken Token { get; set; }

        public virtual IRppNode Analyze(SymbolTable scope)
        {
            return this;
        }

        public virtual void Accept(IRppNodeVisitor visitor)
        {
        }
    }
}