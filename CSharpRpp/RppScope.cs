using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace CSharpRpp
{
    public class RppScope
    {
        private readonly RppScope _parentScope;
        private readonly Dictionary<string, IRppNamedNode> _entities = new Dictionary<string, IRppNamedNode>();
        private readonly Dictionary<string, RppClass> _objects = new Dictionary<string, RppClass>();

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

        public RppClass LookupObject(string name)
        {
            RppClass obj = (RppClass) Lookup(GetObjectName(name));
            Debug.Assert(obj.Kind == ClassKind.Object);
            return obj;
        }

        public static string GetObjectName(string name)
        {
            return name + "$";
        }

        public void Add(IRppNamedNode node)
        {
            string name = node.Name;

            if (node.IsObject())
            {
                name = GetObjectName(name);
            }
            
            if (_entities.ContainsKey(name))
            {
                throw new ArgumentException(string.Format("Already containes {0}", node.Name), "node");
            }

            _entities.Add(name, node);
        }
    }
}