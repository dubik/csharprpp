using System;
using System.Reflection.Emit;

namespace CSharpRpp
{
    public interface IRppNode
    {
        void PreAnalyze(RppScope scope);
        IRppNode Analyze(RppScope scope);
    }

    public interface IRppStatementExpr : IRppNode
    {
        void Codegen(ILGenerator generator);
    }

    public interface IRppExpr : IRppStatementExpr
    {
        RppType Type { get; }
        Type RuntimeType { get; }
    }

    public class RppNode : IRppNode
    {
        public virtual void PreAnalyze(RppScope scope)
        {
        }

        public virtual IRppNode Analyze(RppScope scope)
        {
            return this;
        }
    }
}