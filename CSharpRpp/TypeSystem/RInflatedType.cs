using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace CSharpRpp.TypeSystem
{
    internal class RInflatedType : RType
    {
        private RppFieldInfo[] _fields;
        private RppMethodInfo[] _constructors;
        private RppMethodInfo[] _methods;

        public override IReadOnlyList<RppFieldInfo> Fields => _fields ?? (_fields = InflateFields(DefinitionType.Fields));

        public override IReadOnlyList<RppMethodInfo> Methods => _methods ?? (_methods = InflateMethods(DefinitionType.Methods));
        public override IReadOnlyList<RppMethodInfo> Constructors => _constructors ?? (_constructors = InflateMethods(DefinitionType.Constructors));

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
                    if (Name == "Array")
                    {
                        _type = types[0].MakeArrayType();
                    }
                    else
                    {
                        _type = DefinitionType.NativeType.MakeGenericType(types);
                    }
                }

                return _type;
            }
        }

        public RInflatedType([NotNull] RType type, RType[] genericArguments) : base(type.Name, type.Attributes, type.BaseType, type.DeclaringType)
        {
            DefinitionType = type;
            IsArray = type.IsArray;

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
            return fields.Select(InflateField).ToArray();
        }

        private RppFieldInfo InflateField(RppFieldInfo field)
        {
            return new RppInflatedField(field, _genericArguments, this);
        }

        private RppMethodInfo[] InflateMethods(IEnumerable<RppMethodInfo> constructors)
        {
            return constructors.Select(InflateMethod).ToArray();
        }

        private RppMethodInfo InflateMethod(RppMethodInfo constructor)
        {
            return new RppInflatedMethodInfo(constructor, _genericArguments, this);
        }

        public override string ToString()
        {
            string genericArgumentsStr = string.Join(", ", _genericArguments.Select(a => a.ToString()));
            return $"{Name}[{genericArgumentsStr}]";
        }

        public override RType MakeGenericType(RType[] genericArguments)
        {
            return DefinitionType.MakeGenericType(genericArguments);
        }
    }
}