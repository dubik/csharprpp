using System;
using System.Collections.Generic;

namespace CSharpRpp
{
    public class RppScope
    {
        private readonly RppScope _parentScope;
        private readonly Dictionary<string, IRppNamedNode> _entities = new Dictionary<string, IRppNamedNode>();

        public RppScope(RppScope parentScope)
        {
            _parentScope = parentScope;
        }

        public IRppNamedNode Lookup(string name)
        {
            IRppNamedNode node;
            if (_entities.TryGetValue(name, out node))
            {
                return node;
            }

            return _parentScope != null ? _parentScope.Lookup(name) : null;
        }

        public void Add(IRppNamedNode node)
        {
            if (_entities.ContainsKey(node.Name))
            {
                throw new ArgumentException(string.Format("Already containes {0}", node.Name), "node");
            }

            _entities.Add(node.Name, node);
        }
    }
}