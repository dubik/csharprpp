using Antlr.Runtime;
using CSharpRpp.Reporting;
using CSharpRpp.Symbols;
using CSharpRpp.TypeSystem;
using JetBrains.Annotations;

namespace CSharpRpp
{
    public interface IRppNode
    {
        IToken Token { get; }

        [NotNull]
        IRppNode Analyze([NotNull] SymbolTable scope, Diagnostic diagnostic);

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

        public virtual IRppNode Analyze(SymbolTable scope, Diagnostic diagnostic)
        {
            return this;
        }

        public virtual void Accept(IRppNodeVisitor visitor)
        {
        }
    }
}