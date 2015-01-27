namespace CSharpRpp
{
    public interface IRppNamedNode
    {
        string Name { get; }
    }

    public abstract class RppNamedNode : RppNode, IRppNamedNode
    {
        public string Name { get; private set; }

        protected RppNamedNode(string name)
        {
            Name = name;
        }
    }
}