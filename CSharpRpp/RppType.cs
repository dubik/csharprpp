using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CSharpRpp.Native;
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

    internal static class TypeExtensions
    {
        public static bool IsNumeric(this Type type)
        {
            return type == typeof (int) || type == typeof (long) || type == typeof (float) || type == typeof (double) || type == typeof (short) ||
                   type == typeof (byte) || type == typeof (char);
        }

        public static bool IsUndefined(this RppType type)
        {
            return type is RppUndefinedType;
        }

        public static bool IsDefined(this RppType type)
        {
            return !type.IsUndefined();
        }
    }

    public class Types
    {
        public static Type Int = typeof (int);
        public static Type Long = typeof (long);
        public static Type Char = typeof (char);
        public static Type Short = typeof (short);
        public static Type Bool = typeof (bool);
        public static Type Byte = typeof (byte);
        public static Type Float = typeof (float);
        public static Type Double = typeof (double);
    }

    public abstract class RppType
    {
        public virtual Type Runtime { get; protected set; }

        [CanBeNull]
        public abstract ResolvedType Resolve([NotNull] RppScope scope);

        public virtual bool IsSubclassOf(RppType type)
        {
            return false;
        }

        public virtual bool IsGenericParameter()
        {
            return false;
        }

        #region Eqaulity

        protected bool Equals(RppType other)
        {
            return Runtime == other.Runtime;
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
            return Equals((RppType) obj);
        }

        public override int GetHashCode()
        {
            return (Runtime != null ? Runtime.GetHashCode() : 0);
        }

        #endregion
    }

    public class ResolvedType : RppType
    {
        public override ResolvedType Resolve(RppScope scope)
        {
            return this;
        }
    }

    public sealed class RppNullType : ResolvedType
    {
        public static RppNullType Instance = new RppNullType();

        private RppNullType()
        {
            Runtime = typeof (object);
        }
    }

    public sealed class RppUndefinedType : ResolvedType
    {
        public static RppUndefinedType Instance = new RppUndefinedType();
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

        public override bool IsGenericParameter()
        {
            return Runtime.IsGenericParameter;
        }

        public override string ToString()
        {
            return Runtime.ToString();
        }
    }

    public sealed class RppPrimitiveType : RppType
    {
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

        public static RppPrimitiveType UnitTy = new RppPrimitiveType(ERppPrimitiveType.EUnit);
        public static RppPrimitiveType CharTy = new RppPrimitiveType(ERppPrimitiveType.EChar);
        public static RppPrimitiveType BooleanTy = new RppPrimitiveType(ERppPrimitiveType.EBool);
        public static RppPrimitiveType ShortTy = new RppPrimitiveType(ERppPrimitiveType.EShort);
        public static RppPrimitiveType IntTy = new RppPrimitiveType(ERppPrimitiveType.EInt);
        public static RppPrimitiveType LongTy = new RppPrimitiveType(ERppPrimitiveType.ELong);
        public static RppPrimitiveType FloatTy = new RppPrimitiveType(ERppPrimitiveType.EFloat);
        public static RppPrimitiveType DoubleTy = new RppPrimitiveType(ERppPrimitiveType.EDouble);

        private static readonly Dictionary<string, RppPrimitiveType> PrimitiveTypesMap = new Dictionary<string, RppPrimitiveType>
        {
            {"Boolean", BooleanTy},
            {"Char", CharTy},
            {"Short", ShortTy},
            {"Int", IntTy},
            {"Long", LongTy},
            {"Float", FloatTy},
            {"Double", DoubleTy},
            {"Unit", UnitTy}
        };

        public readonly ERppPrimitiveType PrimitiveType;

        public RppPrimitiveType(ERppPrimitiveType primitiveType)
        {
            PrimitiveType = primitiveType;
            Runtime = SystemTypesMap[PrimitiveType].Runtime;
        }

        public static bool IsPrimitive([NotNull] string name, out RppPrimitiveType type)
        {
            return PrimitiveTypesMap.TryGetValue(name, out type);
        }

        public override ResolvedType Resolve(RppScope scope)
        {
            return SystemTypesMap[PrimitiveType];
        }

        private bool Equals(RppPrimitiveType other)
        {
            return base.Equals(other) && PrimitiveType == other.PrimitiveType;
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
            return Equals((RppPrimitiveType) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode() * 397) ^ (int) PrimitiveType;
            }
        }
    }

    public class RppObjectType : ResolvedType
    {
        public IRppClass Class { get; protected set; }

        public RppObjectType([NotNull] IRppClass clazz)
        {
            Class = clazz;
        }

        public override Type Runtime => Class.RuntimeType;

        public override bool IsSubclassOf(RppType type)
        {
            return type.Runtime.IsSubclassOf(type.Runtime);
        }
    }

    public sealed class RppGenericObjectType : RppObjectType
    {
        public override Type Runtime { get; protected set; }
        public IEnumerable<Type> GenericArguments { get; }

        public RppGenericObjectType(IRppClass clazz, IEnumerable<Type> genericArguments, Type runtimeType) : base(clazz)
        {
            Runtime = runtimeType;
            GenericArguments = genericArguments;
        }

        public override string ToString()
        {
            return Class.Name + "[" + string.Join(", ", GenericArguments) + "]";
        }
    }

    public sealed class RppArrayType : RppObjectType
    {
        public RppType SubType { get; set; }

        public override Type Runtime { get; protected set; }

        public RppArrayType([NotNull] RppType subType) : base(CreateWrappedClass(subType))
        {
            SubType = subType;
        }

        private static RppClass CreateWrappedClass([NotNull] RppType subType)
        {
            var funcs = new[]
            {
                new RppFunc("length", RppPrimitiveType.IntTy) {IsStub = true},
                new RppFunc("apply", new[] {new RppParam("i", RppPrimitiveType.IntTy)}, subType) {IsStub = true},
                new RppFunc("update", new[] {new RppParam("i", RppPrimitiveType.IntTy), new RppParam("x", subType)}, RppPrimitiveType.UnitTy) {IsStub = true}
            };

            return new RppClass(ClassKind.Class, Collections.NoModifiers, "Array", Collections.NoFields, funcs, Collections.NoVariantTypeParams,
                RppBaseConstructorCall.Object);
        }

        public override ResolvedType Resolve(RppScope scope)
        {
            SubType = SubType.Resolve(scope);
            Debug.Assert(SubType != null, "resolvedSubType != null");
            Class = CreateWrappedClass(SubType);
            var runtime = SubType.Runtime;
            var makeArrayType = runtime.MakeArrayType();
            Runtime = makeArrayType;
            return this;
        }
    }

    public class RppGenericType : RppType
    {
        public IEnumerable<RppType> Params
        {
            get { return _params.AsEnumerable(); }
        }

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

            Type[] paramType = _params.Select(param => param.Resolve(scope)).Select(param => param.Runtime).ToArray();
            IRppNamedNode node = LookupGenericType(_typeName.Name, paramType.Length, scope);
            if (node is RppClass)
            {
                RppClass obj = node as RppClass;
                Type specializedType = obj.RuntimeType.MakeGenericType(paramType);
                return new RppGenericObjectType(obj, paramType, specializedType);
            }

            if (node is RppNativeClass)
            {
                RppNativeClass nativeClass = node as RppNativeClass;
                Type specializedType = nativeClass.RuntimeType.MakeGenericType(paramType);
                return new RppGenericObjectType(nativeClass, paramType, specializedType);
            }


            return null;
        }

        /// <summary>
        /// C# generates names of generic classes based on signature, e.g. Foo[T] is Foo`1
        /// </summary>
        /// <param name="name">base name of class, e.g. "Foo"</param>
        /// <param name="genericArgCount">how many generic arguments</param>
        /// <param name="scope">scope where to look</param>
        /// <returns></returns>
        private static IRppNamedNode LookupGenericType(string name, int genericArgCount, RppScope scope)
        {
            IRppNamedNode node = scope.Lookup(name);
            if (node != null)
            {
                return node;
            }

            return scope.Lookup(name + '`' + genericArgCount); // 1 accounts for return type
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
                return RppNativeType.Create(typeof (string));
            }

            if (Name == "Exception")
            {
                return RppNativeType.Create(typeof (Exception));
            }

            if (Name == "Any")
            {
                return RppNativeType.Create(typeof (object));
            }

            if (Name == "Nothing")
            {
                return RppNativeType.Create(typeof (object));
            }

            IRppNamedNode node = scope.Lookup(Name);
            if (node is RppVariantTypeParam)
            {
                return RppNativeType.Create((node as RppVariantTypeParam).Runtime);
            }

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

    public enum TypeVariant
    {
        Covariant,
        Contravariant
    }

    public class RppVariantTypeParam : RppNamedNode
    {
        public TypeVariant Variant { get; private set; }
        public RppType LowerBound { get; set; }
        public RppType UpperBound { get; set; }
        public RppType Type { get; set; }

        public Type Runtime { get; set; }

        // TODO this is kinda bad I think. We shouldn't create variant type out of native type since they solve different tasks
        // variant type describes constraints but native type doesn't have those
        public RppVariantTypeParam(Type nativeType) : base(nativeType.Name)
        {
            Runtime = nativeType;
            Type = RppNativeType.Create(nativeType);
        }

        public RppVariantTypeParam(string name, TypeVariant variant) : base(name)
        {
            Variant = variant;
        }

        public void Resolve(RppScope scope)
        {
            if (Runtime == null)
            {
                RppTypeName typeName = new RppTypeName(Name);
                ResolvedType resolvedType = typeName.Resolve(scope);
                if (resolvedType == null)
                {
                    throw new Exception("Can't resolve type: " + Name);
                }

                Runtime = resolvedType.Runtime;
            }
        }
    }
}