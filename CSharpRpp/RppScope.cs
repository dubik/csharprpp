using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;

namespace CSharpRpp
{
    public class RppScope
    {
        [CanBeNull] protected readonly RppScope ParentScope;
        [NotNull] private readonly Dictionary<string, IRppNamedNode> _entities = new Dictionary<string, IRppNamedNode>();
        [NotNull] private readonly MultiValueDictionary<string, RppFunc> _functions = new MultiValueDictionary<string, RppFunc>();

        public RppScope(RppScope parentScope)
        {
            ParentScope = parentScope;
        }

        public IRppNamedNode Lookup(string name)
        {
            IRppNamedNode node;
            if (_entities.TryGetValue(name, out node))
            {
                return node;
            }

            return ParentScope != null ? ParentScope.Lookup(name) : null;
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

            if (node.IsFunction())
            {
                Add((RppFunc) node);
            }
            else
            {
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

        public void Add(RppFunc func)
        {
            CheckFunctionAlreadyExists(func);
            _functions.Add(func.Name, func);
        }

        private void CheckFunctionAlreadyExists(RppFunc func)
        {
            IReadOnlyCollection<RppFunc> funcs;

            if (_functions.TryGetValue(func.Name, out funcs) && funcs.FirstOrDefault(f => f.SignatureMatch(func)) != null)
            {
                throw new Exception("Function " + func.Name + " already exists in the scope");
            }
        }

        [NotNull]
        public virtual IReadOnlyCollection<IRppFunc> LookupFunction(string name, bool searchParentScope = true)
        {
            return DoLookupFunction(name, searchParentScope);
        }

        protected IReadOnlyCollection<IRppFunc> DoLookupFunction(string name, bool searchParentScope)
        {
            IReadOnlyCollection<RppFunc> node;
            if (_functions.TryGetValue(name, out node))
            {
                return node;
            }

            return ParentScope != null && searchParentScope ? ParentScope.LookupFunction(name) : Collections.NoFuncsCollection;
        }
    }
}