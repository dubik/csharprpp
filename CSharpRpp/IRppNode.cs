using System.Reflection.Emit;

namespace CSharpRpp
{
    public interface IRppNode
    {
        void PreAnalyze(RppScope scope);
        IRppNode Analyze(RppScope scope);
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