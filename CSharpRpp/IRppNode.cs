using System.Reflection.Emit;

namespace CSharpRpp
{
    public interface IRppNode
    {
        void PreAnalyze(RppScope scope);
        IRppNode Analyze(RppScope scope);
        void Codegen(CodegenContext ctx);
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

        public virtual void Codegen(CodegenContext ctx)
        {
        }
    }

}