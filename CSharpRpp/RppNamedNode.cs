using JetBrains.Annotations;

namespace CSharpRpp
{
    public interface IRppNamedNode
    {
        [NotNull]
        string Name { get; }
    }

    public abstract class RppNamedNode : RppNode, IRppNamedNode
    {
        public string Name { get; }

        protected RppNamedNode([NotNull] string name)
        {
            Name = name;
        }
    }
}