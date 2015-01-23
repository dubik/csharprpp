namespace CSharpRpp
{
    public abstract class RppNamedNode : RppNode
    {
        public string Name { get; private set; }

        protected RppNamedNode(string name)
        {
            Name = name;
        }
    }
}