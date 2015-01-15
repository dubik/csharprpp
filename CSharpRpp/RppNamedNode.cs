namespace CSharpRpp
{
    public abstract class RppNamedNode : RppNode
    {
        public readonly string Name;

        protected RppNamedNode(string name)
        {
            Name = name;
        }
    }
}