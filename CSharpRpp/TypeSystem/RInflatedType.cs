﻿using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace CSharpRpp.TypeSystem
{
    internal class RInflatedType : RType
    {
        private RppFieldInfo[] _fields;
        private RppMethodInfo[] _constructors;

        public override IReadOnlyList<RppFieldInfo> Fields => _fields ?? (_fields = InflateFields(DefinitionType.Fields));

        public override IReadOnlyList<RppMethodInfo> Methods { get; }
        public override IReadOnlyList<RppMethodInfo> Constructors => _constructors ?? (_constructors = InflateConstructors(DefinitionType.Constructors));

        public override IReadOnlyCollection<RType> GenericArguments => _genericArguments;

        private readonly RType[] _genericArguments;

        private Type _type;

        public override Type NativeType
        {
            get
            {
                if (_type == null)
                {
                    Type[] types = _genericArguments.Select(a => a.NativeType).ToArray();
                    _type = DefinitionType.NativeType.MakeGenericType(types);
                }

                return _type;
            }
        }

        public RInflatedType([NotNull] RType type, RType[] genericArguments) : base(type.Name, type.Attributes, type.BaseType, type.DeclaringType)
        {
            DefinitionType = type;

            if (!type.IsGenericType)
            {
                throw new Exception("Can't inlfate non generic type");
            }

            if (type.GenericParameters.Count != genericArguments.Length)
            {
                throw new Exception("There are different amount of generic arguments and parameters, they should be the same");
            }

            _genericArguments = genericArguments;
        }

        private RppFieldInfo[] InflateFields(IEnumerable<RppFieldInfo> fields)
        {
            foreach (RppFieldInfo field in fields)
            {
                throw new NotImplementedException();
            }

            throw new NotImplementedException();
        }

        private RppMethodInfo[] InflateConstructors(IEnumerable<RppMethodInfo> constructors)
        {
            return constructors.Select(InflateConstructor).ToArray();
        }

        private RppMethodInfo InflateConstructor(RppMethodInfo constructor)
        {
            return new RppInflatedMethodInfo(constructor, _genericArguments, this);
        }

        public override string ToString()
        {
            string genericArgumentsStr = string.Join(", ", _genericArguments.Select(a => a.ToString()));
            return $"{Name}[{genericArgumentsStr}]";
        }
    }
}