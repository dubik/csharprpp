using System;
using JetBrains.Annotations;

namespace CSharpRpp.TypeSystem
{
    public class ResolvableType
    {
        [NotNull]
        public RTypeName Name { get; }

        [NotNull]
        public RType Value
        {
            get
            {
                if (_type == null)
                    throw new Exception("Type wasn't resolved");

                return _type;
            }
        }

        private RType _type;

        public ResolvableType([NotNull] RTypeName name)
        {
            Name = name;
        }

        public ResolvableType([NotNull] RType type)
        {
            _type = type;
            Name = new RTypeName(type.Name); // TODO this doesn't work for generics
        }

        public void Resolve([NotNull] RppScope scope)
        {
            if (_type == null)
            {
                _type = Name.Resolve(scope);
            }
        }
    }
}