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
        public virtual Type Runtime { get; protected set; }

        [CanBeNull]
        public abstract ResolvedType Resolve([NotNull] RppScope scope);
    }

    public class ResolvedType : RppType
    {
        public override ResolvedType Resolve(RppScope scope)
        {
            return this;
        }
    }

    public sealed class RppNativeType : ResolvedType
    {
        [NotNull]
        public static RppNativeType Create([NotNull] Type type)
        {
            return new RppNativeType {Runtime = type};
        }

        private RppNativeType()
        {
        }
    }

    public class RppPrimitiveType : RppType
    {
        public readonly ERppPrimitiveType PrimitiveType;
        public static RppPrimitiveType UnitTy = new RppPrimitiveType(ERppPrimitiveType.EUnit);

        private static readonly Dictionary<string, RppPrimitiveType> PrimitiveTypesMap = new Dictionary<string, RppPrimitiveType>
        {
            {"Bool", new RppPrimitiveType(ERppPrimitiveType.EBool)},
            {"Char", new RppPrimitiveType(ERppPrimitiveType.EChar)},
            {"Short", new RppPrimitiveType(ERppPrimitiveType.EShort)},
            {"Int", new RppPrimitiveType(ERppPrimitiveType.EInt)},
            {"Long", new RppPrimitiveType(ERppPrimitiveType.ELong)},
            {"Float", new RppPrimitiveType(ERppPrimitiveType.EFloat)},
            {"Double", new RppPrimitiveType(ERppPrimitiveType.EDouble)},
            {"Unit", UnitTy}
        };

        private static readonly Dictionary<ERppPrimitiveType, ResolvedType> SystemTypesMap = new Dictionary<ERppPrimitiveType, ResolvedType>
        {
            {ERppPrimitiveType.EBool, RppNativeType.Create(typeof (bool))},
            {ERppPrimitiveType.EChar, RppNativeType.Create(typeof (char))},
            {ERppPrimitiveType.EShort, RppNativeType.Create(typeof (short))},
            {ERppPrimitiveType.EInt, RppNativeType.Create(typeof (int))},
            {ERppPrimitiveType.ELong, RppNativeType.Create(typeof (long))},
            {ERppPrimitiveType.EFloat, RppNativeType.Create(typeof (float))},
            {ERppPrimitiveType.EDouble, RppNativeType.Create(typeof (double))},
            {ERppPrimitiveType.EUnit, RppNativeType.Create(typeof (void))}
        };

        public RppPrimitiveType(ERppPrimitiveType primitiveType)
        {
            PrimitiveType = primitiveType;
        }

        public static bool IsPrimitive([NotNull] string name, out RppPrimitiveType type)
        {
            return PrimitiveTypesMap.TryGetValue(name, out type);
        }

        public override ResolvedType Resolve(RppScope scope)
        {
            return SystemTypesMap[PrimitiveType];
        }
    }

    public sealed class RppObjectType : ResolvedType
    {
        public RppClass Class { get; private set; }

        public RppObjectType([NotNull] RppClass clazz)
        {
            Class = clazz;
        }

        public override Type Runtime
        {
            get { return Class.RuntimeType; }
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

        public override ResolvedType Resolve(RppScope scope)
        {
            // var paramsType = _params.Select(par => par.Resolve(scope)).ToList();
            // RppNamedNode genericType = scope.Lookup(_typeName.Name);
            if (_typeName.Name == "Array")
            {
                ResolvedType subType = _params[0].Resolve(scope);
                Debug.Assert(subType != null, "subType != null");
                return RppNativeType.Create(subType.Runtime.MakeArrayType());
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

        public override ResolvedType Resolve(RppScope scope)
        {
            RppPrimitiveType primitiveType;
            if (RppPrimitiveType.IsPrimitive(Name, out primitiveType))
            {
                return primitiveType.Resolve(scope);
            }

            if (Name == "String")
            {
                return RppNativeType.Create(typeof (String));
            }

            if (Name == "Any")
            {
                return RppNativeType.Create(typeof (Object));
            }

            IRppNamedNode node = scope.Lookup(Name);
            RppClass clazz = node as RppClass;
            Debug.Assert(clazz != null);
            return new RppObjectType(clazz);
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