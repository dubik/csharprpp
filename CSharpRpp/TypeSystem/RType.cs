using System;
using System.Collections.Generic;
using System.Linq;
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
        Public = 32
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
        public string Name { get; private set; }
        public RMethodAttributes Attributes { get; private set; }

        [NotNull]
        public RType ReturnType { get; private set; }

        public RppParameterInfo[] Parameters { get; private set; }

        [NotNull]
        public RType DeclaringType { get; private set; }

        public RppMethodInfo([NotNull] string name, [NotNull] RType declaringType, RMethodAttributes attributes, [NotNull] RType returnType,
            [NotNull] RppParameterInfo[] parameters)
        {
            Name = name;
            DeclaringType = declaringType;
            Attributes = attributes;
            ReturnType = returnType;
            Parameters = parameters;
        }
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

        public RppTypeParameterInfo(string name)
        {
            Name = name;
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

    public class RType
    {
        [NotNull]
        public string Name { get; }

        [CanBeNull]
        public RType DeclaringType { get; }

        [CanBeNull]
        public RType BaseType { get; }

        public RTypeAttributes Attributes { get; }

        public bool IsAbstract => Attributes.HasFlag(RTypeAttributes.Abstract);

        public bool IsClass => Attributes.HasFlag(RTypeAttributes.Class);

        public bool IsSealed => Attributes.HasFlag(RTypeAttributes.Sealed);

        public bool IsArray => false;

        public bool IsGenericType => _typeParameters.Count != 0;

        public bool IsPrimitive => !IsClass;

        public Type TypeInfo { get; set; }

        public IReadOnlyList<RppFieldInfo> Fields => _fields;

        public IReadOnlyList<RppMethodInfo> Methods => _methods;

        public IReadOnlyList<RppTypeParameterInfo> TypeParameters => _typeParameters;

        public IReadOnlyList<RppConstructorInfo> Constructors => _constructors;

        private readonly List<RppTypeParameterInfo> _typeParameters = new List<RppTypeParameterInfo>();
        private readonly List<RppConstructorInfo> _constructors = new List<RppConstructorInfo>();
        private readonly List<RppFieldInfo> _fields = new List<RppFieldInfo>();
        private readonly List<RppMethodInfo> _methods = new List<RppMethodInfo>();

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
    }
}