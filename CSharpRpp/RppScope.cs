using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;
using CSharpRpp.TypeSystem;

namespace CSharpRpp
{
    public class RppScope
    {
        [CanBeNull] protected readonly RppScope ParentScope;
        [NotNull] private readonly Dictionary<string, IRppNamedNode> _entities = new Dictionary<string, IRppNamedNode>();
        [NotNull] private readonly Dictionary<string, RppType> _genericTypes = new Dictionary<string, RppType>();

        [NotNull] private readonly Dictionary<string, RType> _types = new Dictionary<string, RType>();

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

            return ParentScope?.Lookup(name);
        }

        public RType LookupType(string name)
        {
            RType type;
            if (_types.TryGetValue(name, out type))
            {
                return type;
            }

            return ParentScope?.LookupType(name);
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

        public void Add(RType type)
        {
            if (_types.ContainsKey(type.Name))
            {
                throw new ArgumentException($"Already containes {type.Name}", nameof(type));
            }

            _types.Add(type.Name, type);
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
                    throw new ArgumentException($"Already containes {node.Name}", nameof(node));
                }

                _entities.Add(name, node);
            }
        }

        public void Add(string genericName, RppType specializedType)
        {
            if (_genericTypes.ContainsKey(genericName))
            {
                throw new ArgumentException($"Already containes {genericName}", nameof(genericName));
            }

            _genericTypes.Add(genericName, specializedType);
        }

        public RppType LookupGenericType(string genericName)
        {
            RppType type;
            if (_genericTypes.TryGetValue(genericName, out type))
            {
                return type;
            }

            return ParentScope?.LookupGenericType(genericName);
        }

        [NotNull]
        public virtual IReadOnlyCollection<RppMethodInfo> LookupFunction(string name, bool searchParentScope = true)
        {
            return Collections.NoRFuncsCollection;
        }
    }
}