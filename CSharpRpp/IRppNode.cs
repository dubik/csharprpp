namespace CSharpRpp
{
    public interface IRppNode
    {
        void PreAnalyze(RppScope scope);
        IRppNode Analyze(RppScope scope);
    }
}