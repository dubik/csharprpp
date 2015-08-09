using System;
using System.Collections.Generic;
using System.Linq;

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
        Static = 16
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
        public RType ParentType { get; private set; }

        public RppFieldInfo(string name, RFieldAttributes attributes, RType parentType)
        {
            Name = name;
            Attributes = attributes;
            ParentType = parentType;
        }

        #region Equality

        private bool Equals(RppFieldInfo other)
        {
            return string.Equals(Name, other.Name) && Attributes == other.Attributes;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
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
        public RType Class { get; private set; }

        public RppConstructorInfo(RMethodAttributes attributes, IEnumerable<RType> parameterTypes, RType parentClass)
        {
            Attributes = attributes;
            ParameterTypes = parameterTypes.ToArray();
            Class = parentClass;
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

    public class RType
    {
        private static readonly Dictionary<Type, RType> primitiveTypesMap = new Dictionary<Type, RType>();

        public static RType Create(Type systemType)
        {
            if (systemType.IsPrimitive)
            {
                RType type;
                if (!primitiveTypesMap.TryGetValue(systemType, out type))
                {
                    type = new RType(systemType.Name, 0);
                    primitiveTypesMap.Add(systemType, type);
                }

                return type;
            }

            throw new NotImplementedException("Not implemented yet");
        }

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
            get { throw new NotImplementedException(); }
        }

        public bool IsPrimitive
        {
            get { return !IsClass; }
        }

        private readonly List<RppFieldInfo> _fields = new List<RppFieldInfo>();
        private readonly List<RppConstructorInfo> _constructors = new List<RppConstructorInfo>();
        private readonly List<RppMethodInfo> _methods = new List<RppMethodInfo>();

        public RType(string name, RTypeAttributes attributes)
        {
            Name = name;
            Attributes = attributes;
        }

        public RppMethodInfo[] GetMethods()
        {
            return _methods.ToArray();
        }

        public RppConstructorInfo[] GetConstructors()
        {
            return _constructors.ToArray();
        }

        public RppFieldInfo[] GetFields()
        {
            return _fields.ToArray();
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