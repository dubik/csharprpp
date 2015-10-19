using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using JetBrains.Annotations;

namespace CSharpRpp.TypeSystem
{
    [Flags]
    public enum RTypeAttributes
    {
        None = 0,
        Abstract = 1,
        Class = 2,
        Object = 4,
        Sealed = 8,
        Interface = 16,
        Public = 32,
        Protected = 64,
        Private = 128
    }

    [Flags]
    public enum RMethodAttributes
    {
        None = 0,
        Abstract = 1,
        Final = 2,
        Private = 4,
        Public = 8,
        Static = 16,
        Override = 32
    }

    [Flags]
    public enum RFieldAttributes
    {
        None = 0,
        InitOnly = 1,
        Private = 4,
        Public = 8,
        Static = 16
    }

    public sealed class RppFieldInfo
    {
        [NotNull]
        public string Name { get; }

        public RFieldAttributes Attributes { get; }

        [NotNull]
        public RType DeclaringType { get; }

        public RppFieldInfo([NotNull] string name, RFieldAttributes attributes, [NotNull] RType declaringType)
        {
            Name = name;
            Attributes = attributes;
            DeclaringType = declaringType;
        }

        #region Equality

        private bool Equals(RppFieldInfo other)
        {
            return string.Equals(Name, other.Name) && Attributes == other.Attributes && (DeclaringType == null || DeclaringType.Equals(other.DeclaringType));
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

            return obj is RppFieldInfo && Equals((RppFieldInfo) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Name.GetHashCode() * 397) ^ (int) Attributes ^ (DeclaringType?.GetHashCode() ?? 0);
            }
        }

        #endregion
    }

    public class RppMethodInfo
    {
        public string Name { get; }
        public RMethodAttributes Attributes { get; }

        [CanBeNull]
        public RType ReturnType { get; set; }

        [CanBeNull]
        public RppParameterInfo[] Parameters { get; set; }

        [NotNull]
        public RType DeclaringType { get; private set; }

        public MethodBase Native { get; set; }

        public RppMethodInfo([NotNull] string name, [NotNull] RType declaringType, RMethodAttributes attributes, [CanBeNull] RType returnType,
            [NotNull] RppParameterInfo[] parameters)
        {
            Name = name;
            DeclaringType = declaringType;
            Attributes = attributes;
            ReturnType = returnType;
            Parameters = parameters;
        }

        public override string ToString()
        {
            var res = new List<string> {ToString(Attributes), Name + ParamsToString(), ":", ReturnType?.ToString()};
            return string.Join(" ", res);
        }

        #region ToString

        private static readonly List<Tuple<RMethodAttributes, string>> _attrToStr = new List<Tuple<RMethodAttributes, string>>
        {
            Tuple.Create(RMethodAttributes.Final, "final"),
            Tuple.Create(RMethodAttributes.Public, "public"),
            Tuple.Create(RMethodAttributes.Private, "public"),
            Tuple.Create(RMethodAttributes.Abstract, "abstract"),
            Tuple.Create(RMethodAttributes.Override, "override"),
            Tuple.Create(RMethodAttributes.Static, "static")
        };

        private static string ToString(RMethodAttributes attrs)
        {
            List<string> res = new List<string>();

            _attrToStr.Aggregate(res, (list, tuple) =>
                                      {
                                          if (attrs.HasFlag(tuple.Item1))
                                              list.Add(tuple.Item2);
                                          return list;
                                      });

            return string.Join(" ", res);
        }

        private string ParamsToString()
        {
            if (Parameters != null)
            {
                return "(" + string.Join(", ", Parameters.Select(p => p.Name + ": " + p.Type.ToString())) + ")";
            }

            return "";
        }

        #endregion
    }

    public sealed class RppConstructorInfo : RppMethodInfo
    {
        public RppConstructorInfo(RMethodAttributes attributes, RppParameterInfo[] parameterTypes, RType declaringType)
            : base("ctor", declaringType, attributes, RppTypeSystem.UnitTy, parameterTypes)
        {
        }
    }

    public class RppGenericArgument
    {
        public string Name { get; private set; }
    }

    public class RppParameterInfo
    {
        public string Name { get; private set; }
        public RType Type { get; private set; }

        public RppParameterInfo(RType type) : this("", type)
        {
        }

        public RppParameterInfo(string name, RType type)
        {
            Name = name;
            Type = type;
        }
    }

    public class RppTypeParameterInfo
    {
        public string Name { get; private set; }
        public RType Type { get; private set; }

        public RppTypeParameterInfo(string name, RType type)
        {
            Name = name;
            Type = type;
        }
    }

    public interface IRppTypeDefinition
    {
        RppTypeParameterInfo[] TypeParameters { get; }
        RppConstructorInfo[] Constructors { get; }
        RppFieldInfo[] Fields { get; }
        RppMethodInfo[] Methods { get; }
    }

    public sealed class EmptyTypeDefinition : IRppTypeDefinition
    {
        public static IRppTypeDefinition Instance = new EmptyTypeDefinition();

        public RppTypeParameterInfo[] TypeParameters { get; }
        public RppConstructorInfo[] Constructors { get; }
        public RppFieldInfo[] Fields { get; }
        public RppMethodInfo[] Methods { get; }

        private EmptyTypeDefinition()
        {
            TypeParameters = new RppTypeParameterInfo[0];
            Constructors = new RppConstructorInfo[0];
            Fields = new RppFieldInfo[0];
            Methods = new RppMethodInfo[0];
        }
    }

    public class RTypeName
    {
        public static RTypeName Undefined = new RTypeName("Undefined");
        public static RTypeName UnitN = new RTypeName("Unit");
        public static RTypeName IntN = new RTypeName("Int");

        public string Name { get; }

        private readonly IList<RTypeName> _params = new List<RTypeName>();

        public RTypeName(string name)
        {
            Name = name;
        }

        public void AddGenericArgument(RTypeName genericArgument)
        {
            _params.Add(genericArgument);
        }

        public RType Resolve([NotNull] RppScope scope)
        {
            if (_params.Any())
            {
                throw new NotImplementedException("Generics not implemented yet");
            }

            return scope.LookupType(Name);
        }

        public override string ToString()
        {
            if (_params.Any())
            {
                var paramsString = string.Join(", ", _params.Select(p => p.ToString()));
                return $"{Name}[{paramsString}]";
            }

            return Name;
        }
    }

    [DebuggerDisplay("Name = {Name}")]
    public class RType
    {
        [NotNull]
        public string Name { get; }

        [CanBeNull]
        public RType DeclaringType { get; }

        [CanBeNull]
        public RType BaseType { get; private set; }

        public RTypeAttributes Attributes { get; }

        public bool IsAbstract => Attributes.HasFlag(RTypeAttributes.Abstract);

        public bool IsClass => Attributes.HasFlag(RTypeAttributes.Class);

        public bool IsObject => Attributes.HasFlag(RTypeAttributes.Object);

        public bool IsSealed => Attributes.HasFlag(RTypeAttributes.Sealed);

        public bool IsArray => false;

        public bool IsGenericType => _typeParameters.Count != 0;

        public bool IsPrimitive => !IsClass;

        [CanBeNull] private TypeBuilder _typeBuilder;

        [CanBeNull] private readonly Type _type;

        [NotNull]
        public Type NativeType
        {
            get
            {
                if (_type != null)
                {
                    return _type;
                }

                if (_typeBuilder == null)
                {
                    throw new Exception("Native type is not initialized, call CreateNativeType method");
                }

                return _typeBuilder;
            }
        }

        public IReadOnlyList<RppFieldInfo> Fields => _fields;

        public IReadOnlyList<RppMethodInfo> Methods => _methods;

        public IReadOnlyList<RppTypeParameterInfo> TypeParameters => _typeParameters;

        public IReadOnlyList<RppConstructorInfo> Constructors => _constructors;

        private readonly List<RppTypeParameterInfo> _typeParameters = new List<RppTypeParameterInfo>();
        private readonly List<RppConstructorInfo> _constructors = new List<RppConstructorInfo>();
        private readonly List<RppFieldInfo> _fields = new List<RppFieldInfo>();
        private readonly List<RppMethodInfo> _methods = new List<RppMethodInfo>();

        public RType([NotNull] string name, [NotNull] Type type)
        {
            Name = name;
            _type = type;
            // TODO initialize fields and method maps
        }

        public RType([NotNull] string name, RTypeAttributes attributes = RTypeAttributes.None)
        {
            Name = name;
            Attributes = attributes;
        }

        public RType(string name, RTypeAttributes attributes, RType parent, RType declaringType)
        {
            Name = name;
            Attributes = attributes;
            BaseType = parent;
            DeclaringType = declaringType;
        }

        public RppMethodInfo DefineMethod([NotNull] string name, RMethodAttributes attributes)
        {
            return DefineMethod(name, attributes, null, new RppParameterInfo[0]);
        }

        public RppMethodInfo DefineMethod([NotNull] string name,
            RMethodAttributes attributes,
            [CanBeNull] RType returnType,
            [NotNull] RppParameterInfo[] parameterTypes)
        {
            return DefineMethod(name, attributes, returnType, parameterTypes, null);
        }

        public RppMethodInfo DefineMethod([NotNull] string name,
            RMethodAttributes attributes,
            [CanBeNull] RType returnType,
            [NotNull] RppParameterInfo[] parameterTypes,
            [NotNull] RppGenericArgument[] genericArguments)
        {
            RppMethodInfo method = new RppMethodInfo(name, this, attributes, returnType, parameterTypes);
            _methods.Add(method);
            return method;
        }

        public RppFieldInfo DefineField([NotNull] string name, RFieldAttributes attributes, [NotNull] RType type)
        {
            RppFieldInfo field = new RppFieldInfo(name, attributes, this);
            _fields.Add(field);
            return field;
        }

        public RppMethodInfo DefineConstructor(RMethodAttributes attributes)
        {
            return DefineConstructor(attributes, new RppParameterInfo[0]);
        }

        public RppConstructorInfo DefineConstructor(RMethodAttributes attributes, RppParameterInfo[] parameterTypes)
        {
            RppConstructorInfo constructor = new RppConstructorInfo(attributes, parameterTypes, this);
            _constructors.Add(constructor);
            return constructor;
        }

        #region Equality

        protected bool Equals(RType other)
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
            return Equals((RType) obj);
        }

        public override int GetHashCode()
        {
            return (int) Name?.GetHashCode();
        }

        #endregion

        #region ToString

        public override string ToString()
        {
            return $"{Name}";
        }

        #endregion

        public void SetParent(RType type2)
        {
            BaseType = type2;
        }

        public void InitializeNativeType(ModuleBuilder module)
        {
            if (_typeBuilder == null)
            {
                TypeAttributes attrs = GetTypeAttributes(Attributes);
                _typeBuilder = module.DefineType(Name, attrs);
            }
        }

        public void CreateNativeType()
        {
            if (_typeBuilder == null)
            {
                throw new Exception("This instance is wrapping runtime type so can't create native type out of it");
            }

            foreach (RppMethodInfo rppMethod in Methods)
            {
                MethodAttributes attr = GetMethodAttributes(rppMethod.Attributes);
                RType returnType = rppMethod.ReturnType;
                Debug.Assert(returnType != null, "returnType != null");

                rppMethod.Native = _typeBuilder.DefineMethod(rppMethod.Name, attr, CallingConventions.Standard,
                    returnType.NativeType, Type.EmptyTypes);
            }

            foreach (RppConstructorInfo rppConstructor in Constructors)
            {
                MethodAttributes attr = GetMethodAttributes(rppConstructor.Attributes);
                rppConstructor.Native = _typeBuilder.DefineConstructor(attr, CallingConventions.Standard, Type.EmptyTypes);
            }
        }

        private static TypeAttributes GetTypeAttributes(RTypeAttributes modifiers)
        {
            TypeAttributes attrs = TypeAttributes.Class;
            if (modifiers.HasFlag(RTypeAttributes.Abstract))
            {
                attrs |= TypeAttributes.Abstract;
            }

            if (modifiers.HasFlag(RTypeAttributes.Sealed))
            {
                attrs |= TypeAttributes.Sealed;
            }

            if (modifiers.HasFlag(RTypeAttributes.Private) || modifiers.HasFlag(RTypeAttributes.Protected))
            {
                attrs |= TypeAttributes.NotPublic;
            }

            return attrs;
        }

        private static MethodAttributes GetMethodAttributes(RMethodAttributes rAttributes)
        {
            MethodAttributes attrs = MethodAttributes.Private;
            if (rAttributes.HasFlag(RMethodAttributes.Public))
            {
                attrs = MethodAttributes.Public;
            }

            // always virtual, even for statics
            attrs |= MethodAttributes.Virtual;
            attrs |= MethodAttributes.HideBySig;

            if (!rAttributes.HasFlag(RMethodAttributes.Override))
            {
                attrs |= MethodAttributes.NewSlot;
            }

            if (rAttributes.HasFlag(RMethodAttributes.Abstract))
            {
                attrs |= MethodAttributes.Abstract;
            }

            return attrs;
        }
    }
}