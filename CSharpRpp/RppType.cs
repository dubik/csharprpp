using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;

namespace CSharpRpp
{
    public enum ERppPrimitiveType
    {
        EBool,
        EChar,
        EShort,
        EByte,
        EInt,
        ELong,
        EFloat,
        EDouble,
        EUnit
    }

    public abstract class RppType
    {
        [CanBeNull]
        public abstract Type Resolve([NotNull] RppScope scope);
    }

    public class RppNativeType : RppType
    {
        private readonly Type _type;

        [NotNull]
        public static RppNativeType Create([NotNull] Type type)
        {
            return new RppNativeType(type);
        }

        public RppNativeType([NotNull] Type type)
        {
            _type = type;
        }

        public override Type Resolve(RppScope scope)
        {
            return _type;
        }
    }

    public class RppPrimitiveType : RppType
    {
        public readonly ERppPrimitiveType PrimitiveType;
        public static RppPrimitiveType RppUnit = new RppPrimitiveType(ERppPrimitiveType.EUnit);

        private static readonly Dictionary<string, RppPrimitiveType> PrimitiveTypesMap = new Dictionary<string, RppPrimitiveType>
        {
            {"Bool", new RppPrimitiveType(ERppPrimitiveType.EBool)},
            {"Char", new RppPrimitiveType(ERppPrimitiveType.EChar)},
            {"Short", new RppPrimitiveType(ERppPrimitiveType.EShort)},
            {"Int", new RppPrimitiveType(ERppPrimitiveType.EInt)},
            {"Long", new RppPrimitiveType(ERppPrimitiveType.ELong)},
            {"Float", new RppPrimitiveType(ERppPrimitiveType.EFloat)},
            {"Double", new RppPrimitiveType(ERppPrimitiveType.EDouble)},
            {"Unit", RppUnit}
        };

        private static readonly Dictionary<ERppPrimitiveType, Type> SystemTypesMap = new Dictionary<ERppPrimitiveType, Type>
        {
            {ERppPrimitiveType.EBool, typeof (bool)},
            {ERppPrimitiveType.EChar, typeof (char)},
            {ERppPrimitiveType.EShort, typeof (short)},
            {ERppPrimitiveType.EInt, typeof (int)},
            {ERppPrimitiveType.ELong, typeof (long)},
            {ERppPrimitiveType.EFloat, typeof (float)},
            {ERppPrimitiveType.EDouble, typeof (double)},
            {ERppPrimitiveType.EUnit, typeof (void)}
        };

        public RppPrimitiveType(ERppPrimitiveType primitiveType)
        {
            PrimitiveType = primitiveType;
        }

        public static bool IsPrimitive([NotNull] string name, out RppPrimitiveType type)
        {
            return PrimitiveTypesMap.TryGetValue(name, out type);
        }

        public override Type Resolve(RppScope scope)
        {
            return SystemTypesMap[PrimitiveType];
        }
    }

    public class RppObjectType : RppType
    {
        private readonly RppClass _class;

        public RppObjectType([NotNull] RppClass claz)
        {
            _class = claz;
        }

        public override Type Resolve(RppScope scope)
        {
            return _class.RuntimeType;
        }
    }

    public class RppGenericType : RppType
    {
        private readonly IList<RppType> _params = new List<RppType>();

        private readonly RppTypeName _typeName;

        public RppGenericType([NotNull] string typeName)
        {
            _typeName = new RppTypeName(typeName);
        }

        public void AddParam([NotNull] RppType param)
        {
            _params.Add(param);
        }

        public override Type Resolve(RppScope scope)
        {
            // var paramsType = _params.Select(par => par.Resolve(scope)).ToList();
            // RppNamedNode genericType = scope.Lookup(_typeName.Name);
            if (_typeName.Name == "Array")
            {
                Type subType = _params[0].Resolve(scope);
                return subType.MakeArrayType();
            }

            return null;
        }

        public override string ToString()
        {
            var paramsString = string.Join(", ", _params.Select(p => p.ToString()));
            return string.Format("{0}[{1}]", _typeName, paramsString);
        }

        #region Equality

        protected bool Equals(RppGenericType other)
        {
            return _params.SequenceEqual(other._params) && Equals(_typeName, other._typeName);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((RppGenericType) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((_params != null ? _params.GetHashCode() : 0) * 397) ^ _typeName.GetHashCode();
            }
        }

        #endregion
    }


    [DebuggerDisplay("{Name}")]
    public class RppTypeName : RppType
    {
        public readonly string Name;

        public RppTypeName([NotNull] string name)
        {
            Name = name;
        }

        [CanBeNull]
        public override Type Resolve([NotNull] RppScope scope)
        {
            RppPrimitiveType primitiveType;
            if (RppPrimitiveType.IsPrimitive(Name, out primitiveType))
            {
                return primitiveType.Resolve(scope);
            }

            if (Name == "String")
            {
                return typeof (String);
            }

            IRppNamedNode node = scope.Lookup(Name);
            RppClass clazz = node as RppClass;
            Debug.Assert(clazz != null);
            return clazz.RuntimeType;
        }

        public override string ToString()
        {
            return Name;
        }

        #region Equality

        protected bool Equals(RppTypeName other)
        {
            return string.Equals(Name, other.Name);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (obj.GetType() != GetType())
            {
                return false;
            }
            return Equals((RppTypeName) obj);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        #endregion
    }
}