namespace CSharpRpp
{
    public abstract class RppNamedNode : IRppNode
    {
        public readonly string Name;

        protected RppNamedNode(string name)
        {
            Name = name;
        }

        public abstract void PreAnalyze(RppScope scope);
        public abstract IRppNode Analyze(RppScope scope);
    }
}