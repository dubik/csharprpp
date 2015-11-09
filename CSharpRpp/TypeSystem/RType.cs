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

    public class RppFieldInfo
    {
        [NotNull]
        public string Name { get; }

        public RFieldAttributes Attributes { get; }

        [NotNull]
        public RType DeclaringType { get; }

        public virtual RType Type { get; private set; }

        public virtual FieldInfo Native { get; set; }

        public RppFieldInfo([NotNull] string name, [NotNull] RType fieldType, RFieldAttributes attributes, [NotNull] RType declaringType)
        {
            Name = name;
            Type = fieldType;
            Attributes = attributes;
            DeclaringType = declaringType;
        }

        #region Equality

        private bool Equals(RppFieldInfo other)
        {
            return string.Equals(Name, other.Name) && Attributes == other.Attributes && Type.Equals(other.Type)
                   && ((bool) DeclaringType?.Equals(other.DeclaringType));
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

        public RppFieldInfo CloneWithNewType(RType substitutedType)
        {
            RppFieldInfo newField = (RppFieldInfo) MemberwiseClone();
            newField.Type = substitutedType;
            return newField;
        }
    }

    public class RppGenericParameter
    {
        public string Name { get; }
        public RType Type { get; set; }
        public int Position { get; set; }
        public GenericTypeParameterBuilder Native { get; private set; }

        public RppGenericParameter(string name)
        {
            Name = name;
        }

        public override string ToString()
        {
            return $"{Name}";
        }

        public void SetGenericTypeParameterBuilder(GenericTypeParameterBuilder builder)
        {
            Native = builder;
            Type.ReplaceType(builder);
        }
    }

    public class RppParameterInfo
    {
        public string Name { get; }
        public RType Type { get; private set; }
        public int Index { get; set; }

        public bool IsVariadic { get; }

        public RppParameterInfo(RType type) : this("", type, false)
        {
        }

        public RppParameterInfo(string name, RType type, bool variadic = false)
        {
            Name = name;
            Type = type;
            IsVariadic = variadic;
        }

        public override string ToString()
        {
            return $"{Name} : {Type}";
        }

        public RppParameterInfo CloneWithNewType(RType type)
        {
            var instance = (RppParameterInfo) MemberwiseClone();
            instance.Type = type;
            return instance;
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
        RppMethodInfo[] Constructors { get; }
        RppFieldInfo[] Fields { get; }
        RppMethodInfo[] Methods { get; }
    }

    public sealed class EmptyTypeDefinition : IRppTypeDefinition
    {
        public static IRppTypeDefinition Instance = new EmptyTypeDefinition();

        public RppTypeParameterInfo[] TypeParameters { get; }
        public RppMethodInfo[] Constructors { get; }
        public RppFieldInfo[] Fields { get; }
        public RppMethodInfo[] Methods { get; }

        private EmptyTypeDefinition()
        {
            TypeParameters = new RppTypeParameterInfo[0];
            Constructors = new RppMethodInfo[0];
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
        public RType BaseType { get; set; }

        public RTypeAttributes Attributes { get; }

        public bool IsAbstract => Attributes.HasFlag(RTypeAttributes.Abstract);

        public bool IsClass => Attributes.HasFlag(RTypeAttributes.Class) || Attributes.HasFlag(RTypeAttributes.Interface);

        public bool IsObject => Attributes.HasFlag(RTypeAttributes.Object);

        public bool IsSealed => Attributes.HasFlag(RTypeAttributes.Sealed);

        public bool IsArray { get; private set; }

        public bool IsGenericType => _genericParameters.Any();

        public bool IsPrimitive => !IsClass;

        public bool IsGenericParameter { get; internal set; }

        public RType DefinitionType { get; protected set; }

        [CanBeNull] private TypeBuilder _typeBuilder;

        [CanBeNull] private Type _type;

        [NotNull]
        public virtual Type NativeType
        {
            get
            {
                if (_type != null)
                {
                    return _type;
                }

                if (_typeBuilder == null)
                {
                    // Array is a special type, when native type is requested
                    // we need to get underlying type and make array out of it
                    // however we do not store that type, there is no place in the class
                    // (we could create subclass RArrayType but already been there, don't want
                    // So we get subtype by looking at return type of apply() method
                    if (Name == "Array")
                    {
                        RType subType = this.SubType();
                        _type = subType.NativeType.MakeArrayType();
                        return _type;
                    }

                    throw new Exception("Native type is not initialized, call CreateNativeType method");
                }

                return _typeBuilder;
            }
        }

        public virtual IReadOnlyList<RppFieldInfo> Fields => _fields;

        public virtual IReadOnlyList<RppMethodInfo> Methods => _methods;

        public virtual IReadOnlyList<RppMethodInfo> Constructors
        {
            get
            {
                if (_constructors.Count == 0 && _type != null)
                {
                    InitNativeConstructors();
                }

                return _constructors;
            }
        }

        public virtual IReadOnlyCollection<RType> GenericArguments => Collections.NoRTypes;

        public IReadOnlyCollection<RppGenericParameter> GenericParameters => _genericParameters;

        public int GenericParameterPosition { get; set; }

        private readonly List<RppMethodInfo> _constructors = new List<RppMethodInfo>();
        private readonly List<RppFieldInfo> _fields = new List<RppFieldInfo>();
        private readonly List<RppMethodInfo> _methods = new List<RppMethodInfo>();
        private RppGenericParameter[] _genericParameters = new RppGenericParameter[0];
        private readonly List<RType> _genericArguments = new List<RType>();

        public RType([NotNull] string name, [NotNull] Type type)
        {
            Name = name;
            _type = type;
        }

        private void InitNativeConstructors()
        {
            Debug.Assert(_type != null, "_type != null");
            var constructors = _type.GetConstructors(BindingFlags.Public | BindingFlags.Instance).Select(Convert);
            _constructors.AddRange(constructors);
        }

        public static RppMethodInfo Convert(ConstructorInfo constructor)
        {
            Type declaringType = constructor.DeclaringType;
            Debug.Assert(declaringType != null, "declaringType != null");

            var rMethodAttributes = RTypeUtils.GetRMethodAttributes(constructor.Attributes);
            var parameters = constructor.GetParameters().Select(p => new RppParameterInfo(new RType(p.ParameterType.Name, p.ParameterType))).ToArray();
            RppMethodInfo rppConstructor = new RppMethodInfo("ctor", new RType(declaringType.Name, declaringType), rMethodAttributes,
                RppTypeSystem.UnitTy, parameters)
            {
                Native = constructor
            };
            return rppConstructor;
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
            [NotNull] RppGenericParameter[] genericParameters)
        {
            RppMethodInfo method = new RppMethodInfo(name, this, attributes, returnType, parameterTypes);
            if (name == "ctor")
            {
                _constructors.Add(method);
            }
            else
            {
                _methods.Add(method);
            }
            return method;
        }

        public RppFieldInfo DefineField([NotNull] string name, [NotNull] RType type, RFieldAttributes attributes)
        {
            RppFieldInfo field = new RppFieldInfo(name, type, attributes, this);
            _fields.Add(field);
            return field;
        }

        public RppMethodInfo DefineConstructor(RMethodAttributes attributes)
        {
            return DefineConstructor(attributes, new RppParameterInfo[0]);
        }

        public RppMethodInfo DefineConstructor(RMethodAttributes attributes, RppParameterInfo[] parameterTypes)
        {
            RppMethodInfo constructor = new RppMethodInfo("ctor", this, attributes, RppTypeSystem.UnitTy, parameterTypes);
            _constructors.Add(constructor);
            return constructor;
        }

        public RType MakeArrayType()
        {
            RType newType = new RType("Array", Attributes, null, DeclaringType) {IsArray = true};
            newType.DefineMethod("length", RMethodAttributes.Public, RppTypeSystem.IntTy, new RppParameterInfo[0]);
            newType.DefineMethod("apply", RMethodAttributes.Public, this, new[] {new RppParameterInfo("index", RppTypeSystem.IntTy)}, new RppGenericParameter[0]);
            newType.DefineMethod("update", RMethodAttributes.Public, this,
                new[] {new RppParameterInfo("index", RppTypeSystem.IntTy), new RppParameterInfo("value", this)}, new RppGenericParameter[0]);
            return newType;
        }

        public RType MakeGenericType(RType[] genericArguments)
        {
            RInflatedType inflatedType = new RInflatedType(this, genericArguments);
            return inflatedType;
        }

        public RppGenericParameter[] DefineGenericParameters(string[] genericParameterName)
        {
            if (_genericParameters.Any())
            {
                throw new Exception("there were generic paremeters defined already");
            }

            _genericParameters = RTypeUtils.CreateGenericParameters(genericParameterName, this).ToArray();
            return _genericParameters;
        }

        internal void ReplaceType(Type type)
        {
            _type = type;
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
            if (IsGenericType)
            {
                string genericParameters = string.Join(", ", _genericParameters.Select(p => p.ToString()));
                return $"{Name}[{genericParameters}]";
            }

            return $"{Name}";
        }

        #endregion

        public void InitializeNativeType(ModuleBuilder module)
        {
            if (_typeBuilder == null)
            {
                TypeAttributes attrs = RTypeUtils.GetTypeAttributes(Attributes);
                _typeBuilder = module.DefineType(Name, attrs);

                if (IsGenericType)
                {
                    string[] genericParameterNames = _genericParameters.Select(p => p.Name).ToArray();
                    GenericTypeParameterBuilder[] nativeGenericParameter = _typeBuilder.DefineGenericParameters(genericParameterNames);
                    _genericParameters.ForEachWithIndex(
                        (index, genericParameter) => genericParameter.SetGenericTypeParameterBuilder(nativeGenericParameter[index]));
                }
            }
        }

        public void CreateNativeType()
        {
            if (_typeBuilder == null)
            {
                throw new Exception("This instance is wrapping runtime type so can't create native type out of it");
            }

            if (BaseType != null)
            {
                _typeBuilder.SetParent(BaseType.NativeType);
            }

            foreach (RppMethodInfo rppMethod in Methods)
            {
                RTypeUtils.DefineNativeTypeFor(_typeBuilder, rppMethod);
            }

            foreach (RppMethodInfo rppConstructor in Constructors)
            {
                RTypeUtils.DefineNativeTypeForConstructor(_typeBuilder, rppConstructor);
            }

            foreach (RppFieldInfo rppField in Fields)
            {
                FieldAttributes attr = GetAttributes(rppField.Attributes);
                rppField.Native = _typeBuilder.DefineField(rppField.Name, rppField.Type.NativeType, attr);
            }
        }

        private static FieldAttributes GetAttributes(RFieldAttributes attributes)
        {
            FieldAttributes attrs = FieldAttributes.Public;
            if (attributes.HasFlag(RFieldAttributes.Public))
            {
                attrs |= FieldAttributes.Public;
            }

            if (attributes.HasFlag(RFieldAttributes.Private))
            {
                attrs |= FieldAttributes.Private;
            }

            if (attributes.HasFlag(RFieldAttributes.InitOnly))
            {
                attrs |= FieldAttributes.InitOnly;
            }

            if (attributes.HasFlag(RFieldAttributes.Static))
            {
                attrs |= FieldAttributes.Static;
            }

            return attrs;
        }

        public bool IsSubclassOf(RType targetType)
        {
            if (string.Equals(Name, targetType.Name))
            {
                return true;
            }

            return BaseType?.IsSubclassOf(targetType) ?? false;
        }

        public bool IsNumeric()
        {
            return Name == "Int" || Name == "Float" || Name == "Double" || Name == "Char" || Name == "Short" || Name == "Byte";
        }
    }
}