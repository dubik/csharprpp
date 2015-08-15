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
        Sealed = 4,
        Interface = 8,
        Public = 16
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
        public string Name { get; private set; }
        public RFieldAttributes Attributes { get; private set; }
        public RType EnclosingType { get; private set; }

        public RppFieldInfo(string name, RFieldAttributes attributes, RType enclosingType)
        {
            Name = name;
            Attributes = attributes;
            EnclosingType = enclosingType;
        }

        #region Equality

        private bool Equals(RppFieldInfo other)
        {
            return string.Equals(Name, other.Name) && Attributes == other.Attributes;
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
                return ((Name != null ? Name.GetHashCode() : 0) * 397) ^ (int) Attributes;
            }
        }

        #endregion
    }

    public class RppConstructorInfo
    {
        public RMethodAttributes Attributes { get; private set; }
        public RType[] ParameterTypes { get; private set; }
        public RType EnclosingType { get; private set; }

        public RppConstructorInfo(RMethodAttributes attributes, IEnumerable<RType> parameterTypes, RType enclosingType)
        {
            Attributes = attributes;
            ParameterTypes = parameterTypes.ToArray();
            EnclosingType = enclosingType;
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

    public class RppMethodInfo
    {
        public string Name { get; private set; }
        public RType ParentType { get; private set; }
        public RMethodAttributes Attributes { get; private set; }
        public RType ReturnType { get; private set; }
        public RppParameterInfo[] Parameters { get; private set; }

        public RppMethodInfo(string name, RType parentType, RMethodAttributes attributes, RType returnType, RppParameterInfo[] parameters)
        {
            Name = name;
            ParentType = parentType;
            Attributes = attributes;
            ReturnType = returnType;
            Parameters = parameters;
        }
    }

    public class RppTypeParameterInfo
    {
        public string Name { get; private set; }
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

        public RppTypeParameterInfo[] TypeParameters { get; private set; }
        public RppConstructorInfo[] Constructors { get; private set; }
        public RppFieldInfo[] Fields { get; private set; }
        public RppMethodInfo[] Methods { get; private set; }

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
        public string Name { get; private set; }

        public RTypeAttributes Attributes { get; private set; }

        public bool IsAbstract
        {
            get { return (Attributes & RTypeAttributes.Abstract) != 0; }
        }

        public bool IsClass
        {
            get { return (Attributes & RTypeAttributes.Class) != 0; }
        }

        public bool IsSealed
        {
            get { return (Attributes & RTypeAttributes.Sealed) != 0; }
        }

        public bool IsArray
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsGenericType
        {
            get { return _typeParameters.Count != 0; }
        }

        public bool IsPrimitive
        {
            get { return !IsClass; }
        }

        public Type TypeInfo { get; set; }

        public IReadOnlyList<RppFieldInfo> Fields
        {
            get { return _fields; }
        }

        public IReadOnlyList<RppMethodInfo> Methods
        {
            get { return _methods; }
        }

        private readonly List<RppTypeParameterInfo> _typeParameters = new List<RppTypeParameterInfo>();
        private readonly List<RppConstructorInfo> _constructors = new List<RppConstructorInfo>();
        private readonly List<RppFieldInfo> _fields = new List<RppFieldInfo>();
        private readonly List<RppMethodInfo> _methods = new List<RppMethodInfo>();

        public RType([NotNull] string name, RTypeAttributes attributes)
        {
            Name = name;
            Attributes = attributes;
        }

        public RType([NotNull] string name, RTypeAttributes attributes, [NotNull] IRppTypeDefinition typeDefinition)
        {
            Name = name;
            Attributes = attributes;
        }

        public void DefineMethod(string name, RMethodAttributes attributes, RType returnType, IEnumerable<RppParameterInfo> parameterTypes)
        {
            DefineMethod(name, attributes, returnType, parameterTypes, Enumerable.Empty<RppGenericArgument>());
        }

        public void DefineMethod(string name, RMethodAttributes attributes, RType returnType, IEnumerable<RppParameterInfo> parameterTypes,
            IEnumerable<RppGenericArgument> genericArguments)
        {
            RppMethodInfo method = new RppMethodInfo(name, this, attributes, returnType, parameterTypes.ToArray());
            _methods.Add(method);
        }

        public void DefineField(string name, RFieldAttributes attributes, RType type)
        {
            RppFieldInfo field = new RppFieldInfo(name, attributes, this);
            _fields.Add(field);
        }

        public void DefineConstructor(RMethodAttributes attributes, IEnumerable<RType> parameterTypes)
        {
            RppConstructorInfo constructor = new RppConstructorInfo(attributes, parameterTypes, this);
            _constructors.Add(constructor);
        }
    }
}