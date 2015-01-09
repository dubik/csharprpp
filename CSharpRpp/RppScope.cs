using System;
using System.Collections.Generic;

namespace CSharpRpp
{
    public class RppScope
    {
        private readonly RppScope _parentScope;
        private readonly Dictionary<string, RppNamedNode> _entities = new Dictionary<string, RppNamedNode>();

        public RppScope(RppScope parentScope)
        {
            _parentScope = parentScope;
        }

        public RppNamedNode Lookup(string name)
        {
            RppNamedNode node;
            if (_entities.TryGetValue(name, out node))
            {
                return node;
            }

            return _parentScope != null ? _parentScope.Lookup(name) : null;
        }

        public void Add(RppNamedNode node)
        {
            if (_entities.ContainsKey(node.Name))
            {
                throw new ArgumentException(string.Format("Already containes {0}", node.Name), "node");
            }

            _entities.Add(node.Name, node);
        }
    }
}