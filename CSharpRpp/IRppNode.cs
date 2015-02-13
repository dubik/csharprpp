using System;
using System.Reflection.Emit;
using JetBrains.Annotations;

namespace CSharpRpp
{
    public interface IRppNode
    {
        void PreAnalyze([NotNull] RppScope scope);

        [NotNull]
        IRppNode Analyze([NotNull] RppScope scope);

        void Accept([NotNull] IRppNodeVisitor visitor);
    }

    public interface IRppStatementExpr : IRppNode
    {
        void Codegen([NotNull] ILGenerator generator);
    }

    public interface IRppExpr : IRppStatementExpr
    {
        [NotNull]
        RppType Type { get; }

        [NotNull]
        Type RuntimeType { get; }
    }

    public class RppNode : IRppNode
    {
        public virtual void PreAnalyze(RppScope scope)
        {
        }

        [NotNull]
        public virtual IRppNode Analyze(RppScope scope)
        {
            return this;
        }

        public virtual void Accept(IRppNodeVisitor visitor)
        {
        }
    }
}