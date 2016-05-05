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

        public override IReadOnlyCollection<RppGenericParameter> GenericParameters => DefinitionType.GenericParameters;

        private readonly RType[] _genericArguments;

        private Type _type;

        public override bool IsGenericTypeDefinition => false;

        public override bool IsConstructedGenericType => true;

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

        public RInflatedType([NotNull] RType type, RType[] genericArguments) : base(type.Name, type.Attributes, null, type.DeclaringType)
        {
            BaseType = InflateBaseType(type.BaseType, genericArguments);

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

        [CanBeNull]
        private static RType InflateBaseType([CanBeNull] RType baseType, [NotNull] IEnumerable<RType> genericArguments)
        {
            if (baseType == null)
            {
                return null;
            }

            if (!baseType.ContainsGenericParameters)
            {
                return baseType;
            }

            if (baseType.GenericParameters.Count == 0)
            {
                return baseType;
            }

            return baseType.MakeGenericType(genericArguments.Take(baseType.GenericParameters.Count).ToArray());
        }

        private RppFieldInfo[] InflateFields(IEnumerable<RppFieldInfo> fields)
        {
            return fields.Select(InflateField).ToArray();
        }

        private RppFieldInfo InflateField(RppFieldInfo field)
        {
            return new RppInflatedField(field, _genericArguments, this);
        }

        private RppMethodInfo[] InflateMethods(IEnumerable<RppMethodInfo> method)
        {
            return method.Select(InflateMethod).ToArray();
        }

        private RppMethodInfo InflateMethod(RppMethodInfo method)
        {
            return new RppInflatedMethodInfo(method, _genericArguments, this);
        }

        public override string ToString()
        {
            string genericArgumentsStr = string.Join(", ", _genericArguments.Select(a => a.ToString()));
            return $"{Name}[{genericArgumentsStr}]";
        }

        public override RType MakeGenericType(params RType[] genericArguments)
        {
            return DefinitionType.MakeGenericType(genericArguments);
        }

        protected override bool IsCovariant(RType right)
        {
            RInflatedType other = right as RInflatedType;

            if (other == null)
            {
                return false;
            }

            if (IsConstructedGenericType && right.IsConstructedGenericType && ReferenceEquals(DefinitionType, other.DefinitionType))
            {
                RppGenericParameter[] genericParameters = DefinitionType.GenericParameters.ToArray();

                RType[] otherGenericArguments = other.GenericArguments.ToArray();
                for (int i = 0; i < genericParameters.Length; i++)
                {
                    RppGenericParameter p = genericParameters[i];
                    RType type = _genericArguments[i];
                    RType otherType = otherGenericArguments[i];

                    if (type.IsPrimitive != otherType.IsPrimitive)
                    {
                        return false;
                    }

                    switch (p.Variance)
                    {
                        case RppGenericParameterVariance.Invariant:
                            if (type != otherType)
                            {
                                return false;
                            }
                            break;

                        case RppGenericParameterVariance.Covariant:
                            if (!type.IsAssignable(otherType))
                            {
                                return false;
                            }
                            break;

                        case RppGenericParameterVariance.Contravariant:
                            if (!otherType.IsAssignable(type))
                            {
                                return false;
                            }
                            break;

                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                return true;
            }

            return false;
        }
    }
}