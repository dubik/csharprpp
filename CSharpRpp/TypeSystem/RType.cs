using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using CSharpRpp.Utils;
using JetBrains.Annotations;
using static CSharpRpp.TypeSystem.RppTypeSystem;

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
        Override = 32,
        Synthesized = 64
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

        public string MangledName => GetMangledName(Name);

        public RFieldAttributes Attributes { get; }

        public bool IsInstanceField => Name == "_instance";

        [NotNull]
        public RType DeclaringType { get; }

        public virtual RType Type { get; private set; }

        public virtual FieldInfo Native { get; set; }

        public virtual PropertyInfo NativeProperty { get; set; }

        public virtual MethodInfo NativeGetter => NativeProperty.GetMethod;

        public virtual MethodInfo NativeSetter => NativeProperty.SetMethod;

        public static string GetMangledName(string propertyName) => $"<{propertyName}>_BackingField";

        public string SetterName => RppMethodInfo.GetSetterAccessorName(Name);

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
            return String.Equals(Name, other.Name) && Attributes == other.Attributes && Type.Equals(other.Type)
                   && DeclaringType.Equals(other.DeclaringType);
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
                return (Name.GetHashCode() * 397) ^ (int) Attributes ^ DeclaringType.GetHashCode();
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

    public enum RppGenericParameterVariance
    {
        Invariant,

        /// <summary>
        /// Covariant type, e.g. '+'
        /// </summary>
        Covariant,

        /// <summary>
        /// Contravariant type, e.g. '-'
        /// </summary>
        Contravariant // '-'
    }

    public class RppGenericParameter
    {
        public string Name { get; }
        public RType Type { get; set; }
        public int Position { get; set; }
        public GenericTypeParameterBuilder Native { get; private set; }
        public RppGenericParameterVariance Variance { get; set; }
        public RType Constraint { get; set; }

        public RppGenericParameter(string name)
        {
            Name = name;
            Variance = RppGenericParameterVariance.Invariant;
        }

        public override string ToString()
        {
            string covarianceStr = Variance == RppGenericParameterVariance.Covariant
                ? "+"
                : Variance == RppGenericParameterVariance.Contravariant ? "-" : "";
            return $"{covarianceStr}{Name}";
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

        public RppParameterInfo(RType type) : this("", type)
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

        public bool IsInterface => Attributes.HasFlag(RTypeAttributes.Interface);

        public bool IsArray { get; set; }

        public bool IsGenericType => GenericParameters.Any() || GenericArguments.Any();

        public bool IsPrimitive => !IsClass && !IsObject && !IsGenericParameter;

        public bool IsGenericParameter { get; internal set; }

        public virtual bool IsConstructedGenericType => false;

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
                        RType subType = this.ArrayElementType();
                        _type = subType.NativeType.MakeArrayType();
                        return _type;
                    }

                    throw new Exception("Native type is not initialized, call CreateNativeType method");
                }

                return _typeBuilder;
            }
        }

        public virtual IReadOnlyList<RppFieldInfo> Fields
        {
            get
            {
                if (_fields.Count == 0 && _type != null)
                {
                    InitNativeFields();
                }

                return _fields;
            }
        }

        public virtual IReadOnlyList<RppMethodInfo> Methods
        {
            get
            {
                if (_methods.Count == 0 && _type != null)
                {
                    InitNativeMethods();
                }
                return _methods;
            }
        }

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

        public virtual IReadOnlyCollection<RppGenericParameter> GenericParameters
        {
            get
            {
                if (_genericParameters.Length == 0 && _type != null)
                {
                    InitGenericParameters();
                }

                return _genericParameters;
            }
        }

        public int GenericParameterPosition { get; set; }
        public RppMethodInfo GenericParameterDeclaringMethod { get; set; }

        public bool IsMethodGenericParameter => GenericParameterDeclaringMethod != null;

        public IReadOnlyList<RType> Nested => _nested;

        private readonly List<RppMethodInfo> _constructors = new List<RppMethodInfo>();
        private readonly List<RppFieldInfo> _fields = new List<RppFieldInfo>();
        private readonly List<RppMethodInfo> _methods = new List<RppMethodInfo>();
        private RppGenericParameter[] _genericParameters = new RppGenericParameter[0];
        private readonly List<RType> _nested = new List<RType>();

        [CanBeNull] private List<RType> _implementedInterfaces;

        private readonly Dictionary<RType[], RType> InflatedTypesMap = new Dictionary<RType[], RType>(ArrayOfTypesComparator.Instance);

        public IEnumerable<RType> Interfaces
        {
            get
            {
                if (_implementedInterfaces == null)
                {
                    return Collections.NoRTypesList;
                }

                return _implementedInterfaces;
            }
        }

        public virtual bool IsGenericTypeDefinition => GenericParameters.Any();

        /// <summary>
        /// Returns true if type itself is generic parameter or contains generic parameter in generic arguments e.g.
        /// they can be replaced by type. 
        /// </summary>
        public bool ContainsGenericParameters
        {
            get
            {
                if (IsGenericParameter)
                    return true;

                if (IsGenericType)
                    if (GenericArguments.Any(ga => ga.ContainsGenericParameters))
                        return true;

                return GenericParameters.Count != GenericArguments.Count;
            }
        }

        public RType([NotNull] string name, [NotNull] Type type, [NotNull] Func<Type, RType> typeFactory)
        {
            Name = name;
            _type = type;

            // TODO perhaps make one method which will map attributes
            Attributes = RTypeUtils.GetRTypeAttributes(type.Attributes, type.IsValueType);

            // Name ends with '$' when we import Rpp generated Type
            if (type.Name.EndsWith("$") || name.EndsWith("$"))
            {
                Attributes = Attributes.UnSet(RTypeAttributes.Class);
                Attributes |= RTypeAttributes.Object;
            }

            IsGenericParameter = type.IsGenericParameter;
            if (type.IsGenericParameter)
            {
                GenericParameterPosition = type.GenericParameterPosition;
            }


            if (type.IsClass)
            {
                Type baseType = type.BaseType;
                if (baseType != null && baseType != typeof(object))
                {
                    BaseType = typeFactory(baseType);
                }
                else if (Name != "Any")
                {
                    BaseType = AnyTy;
                }
            }
        }

        public RType([NotNull] string name, RTypeAttributes attributes = RTypeAttributes.Class)
        {
            Name = name;
            Attributes = attributes;
            BaseType = AnyTy;
        }

        public RType(string name, RTypeAttributes attributes, RType parent, RType declaringType = null)
        {
            Name = name;
            Attributes = attributes;
            BaseType = parent;
            DeclaringType = declaringType;

            if (IsClass && BaseType == null)
            {
                BaseType = AnyTy;
            }
        }

        private void InitGenericParameters()
        {
            Debug.Assert(_type != null, "_type != null");
            _genericParameters =
                _type.GetGenericArguments()
                    .Select(
                        gp =>
                            new RppGenericParameter(gp.Name) {Position = gp.GenericParameterPosition, Type = CreateGenericParameterType(gp)})
                    .ToArray();
        }

        private static RType CreateGenericParameterType(Type gp)
        {
            if (!gp.IsGenericParameter)
            {
                // TODO Hm, but it should be also broken for generic parameters, no?
                throw new ArgumentException("Shouldn't create non generic parameters via new RType, because then reference equality among type will break");
            }

            return new RType(gp.Name, gp, CreateGenericParameterType);
        }

        private void InitNativeConstructors()
        {
            Debug.Assert(_type != null, "_type != null");
            var constructors = _type.GetConstructors(BindingFlags.Public | BindingFlags.Instance).Select(Convert);
            _constructors.AddRange(constructors);
        }

        private void InitNativeMethods()
        {
            Debug.Assert(_type != null, "_type != null");
            var methods = _type.GetMethods(BindingFlags.Public | BindingFlags.Instance).Select(Convert);
            _methods.AddRange(methods);
        }

        private void InitNativeFields()
        {
            Debug.Assert(_type != null, "_type != null");
            var fields = _type.GetFields().Select(f => Convert(f, this));
            _fields.AddRange(fields);
            var props = _type.GetProperties().Select(p => Convert(p, this));
            _fields.AddRange(props);
        }

        private static RppFieldInfo Convert(PropertyInfo property, RType declaringType)
        {
            RType fieldType = GetOrCreateType(property.PropertyType.Name, property.PropertyType);
            const RFieldAttributes attr = RFieldAttributes.Public;
            RppFieldInfo rppField = new RppFieldInfo(property.Name, fieldType, attr, declaringType)
            {
                NativeProperty = property
            };

            return rppField;
        }

        private static RppFieldInfo Convert(FieldInfo field, RType declaringType)
        {
            RType fieldType = GetOrCreateType(field.FieldType.Name, field.FieldType);
            bool priv = (field.Attributes & FieldAttributes.Private) != 0;
            RFieldAttributes attr = priv ? RFieldAttributes.Private : RFieldAttributes.Public;
            RppFieldInfo rppField = new RppFieldInfo(field.Name, fieldType, attr, declaringType)
            {
                Native = field
            };

            return rppField;
        }

        private static RppMethodInfo Convert(MethodInfo method)
        {
            Type declaringType = method.DeclaringType;
            Debug.Assert(declaringType != null, "declaringType != null");

            RType returnType = GetOrCreateType(method.ReturnType.Name, method.ReturnType);

            var rMethodAttributes = RTypeUtils.GetRMethodAttributes(method.Attributes);
            var parameters = method.GetParameters().Select(p => new RppParameterInfo(GetOrCreateType(p.ParameterType.Name, p.ParameterType))).ToArray();
            RppMethodInfo rppConstructor = new RppMethodInfo(method.Name, GetOrCreateType(declaringType.Name, declaringType), rMethodAttributes,
                returnType, parameters)
            {
                Native = method
            };

            return rppConstructor;
        }

        private static RppMethodInfo Convert(ConstructorInfo constructor)
        {
            Type declaringType = constructor.DeclaringType;
            Debug.Assert(declaringType != null, "declaringType != null");

            var rMethodAttributes = RTypeUtils.GetRMethodAttributes(constructor.Attributes);
            var parameters = constructor.GetParameters().Select(p => new RppParameterInfo(GetOrCreateType(p.ParameterType.Name, p.ParameterType))).ToArray();
            RppMethodInfo rppConstructor = new RppMethodInfo("ctor", GetOrCreateType(declaringType.Name, declaringType), rMethodAttributes,
                UnitTy, parameters)
            {
                Native = constructor
            };
            return rppConstructor;
        }

        public RType DefineNestedType(string name, RTypeAttributes attributes, RType parent)
        {
            RType nested = new RType(name, attributes, parent, this);
            _nested.Add(nested);
            return nested;
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
            return DefineMethod(name, attributes, returnType, parameterTypes, new RppGenericParameter[0]);
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
            RppMethodInfo constructor = new RppMethodInfo("ctor", this, attributes, UnitTy, parameterTypes);
            _constructors.Add(constructor);
            return constructor;
        }

        public RType MakeArrayType()
        {
            return ArrayTy.MakeGenericType(this);
        }

        public virtual RType MakeGenericType(params RType[] genericArguments)
        {
            if (IsGenericParameter)
            {
                return genericArguments[GenericParameterPosition];
            }

            RType inflatedType;
            if (!InflatedTypesMap.TryGetValue(genericArguments, out inflatedType))
            {
                inflatedType = new RInflatedType(this, genericArguments);
                InflatedTypesMap.Add(genericArguments, inflatedType);
            }

            return inflatedType;
        }

        public RppGenericParameter[] DefineGenericParameters(params string[] genericParameterName)
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

        // TODO perhaps we should remove this since we check equality only by reference
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
            return base.GetHashCode();
        }

        #endregion

        #region ToString

        public override string ToString()
        {
            string prefix = "";
            if (IsGenericParameter)
            {
                prefix = IsMethodGenericParameter ? "!!" : "!";
            }

            if (IsGenericType)
            {
                string genericParameters = string.Join(", ", _genericParameters.Select(p => p.ToString()));
                return $"{prefix}{Name}[{genericParameters}]"; // + $"@{ GetHashCode()}";
            }

            return $"{prefix}{Name}"; //+ $"@{GetHashCode()}";
        }

        #endregion

        public void InitializeNativeType(ModuleBuilder module)
        {
            if (_typeBuilder == null)
            {
                if (DeclaringType == null)
                {
                    TypeAttributes attrs = RTypeUtils.GetTypeAttributes(Attributes);
                    _typeBuilder = module.DefineType(Name, attrs);
                }
                else
                {
                    _typeBuilder = ((TypeBuilder) DeclaringType.NativeType).DefineNestedType(Name, TypeAttributes.NestedPublic);
                }

                if (IsGenericType)
                {
                    RTypeUtils.CreateNativeGenericParameters(_genericParameters,
                        genericParameterNames => _typeBuilder.DefineGenericParameters(genericParameterNames));
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
                Type baseClassType = BaseType.NativeType;
                if (baseClassType.IsClass)
                {
                    _typeBuilder.SetParent(baseClassType);
                }
                else if (baseClassType.IsInterface) // This happens for runtime interfaces like Function*
                {
                    _typeBuilder.AddInterfaceImplementation(baseClassType);
                }
            }

            foreach (RppMethodInfo rppMethod in Methods)
            {
                RTypeUtils.DefineNativeTypeFor(_typeBuilder, rppMethod);
            }

            foreach (RppMethodInfo rppConstructor in Constructors)
            {
                RTypeUtils.DefineNativeTypeForConstructor(_typeBuilder, rppConstructor);
            }

            foreach (RppFieldInfo rppField in Fields.Where(f => !f.IsInstanceField))
            {
                CreateProperty(rppField, Methods);
            }

            foreach (RppFieldInfo rppField in Fields.Where(f => f.IsInstanceField))
            {
                var attrs = GetAttributes(rppField.Attributes);
                rppField.Native = _typeBuilder.DefineField(rppField.Name, rppField.Type.NativeType, attrs);
            }
        }

        private void CreateProperty(RppFieldInfo field, IEnumerable<RppMethodInfo> methods)
        {
            Debug.Assert(_typeBuilder != null, "_typeBuilder != null");

            PropertyBuilder propertyBuilder = _typeBuilder.DefineProperty(field.Name, PropertyAttributes.None, field.Type.NativeType, null);
            propertyBuilder.SetCustomAttribute(RTypeUtils.CreateCompilerGeneratedAttribute());

            // TODO we need to update visibility somehow
            FieldBuilder fieldBuilder = _typeBuilder.DefineField(field.MangledName, field.Type.NativeType, FieldAttributes.Public);
            fieldBuilder.SetCustomAttribute(RTypeUtils.CreateCompilerGeneratedAttribute());

            SetAccessors(propertyBuilder, methods);

            field.Native = fieldBuilder;
            field.NativeProperty = propertyBuilder;
        }

        private static void SetAccessors(PropertyBuilder property, IEnumerable<RppMethodInfo> methods)
        {
            string propertyName = property.Name;
            foreach (RppMethodInfo method in methods)
            {
                if (method.Name == RppMethodInfo.GetGetterAccessorName(propertyName))
                {
                    property.SetGetMethod((MethodBuilder) method.Native);
                }
                else if (method.Name == RppMethodInfo.GetSetterAccessorName(propertyName))
                {
                    property.SetSetMethod((MethodBuilder) method.Native);
                }
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

        [Obsolete("Use IsTrueSubclassOf")]
        public bool IsSubclassOf(RType targetType)
        {
            if (string.Equals(Name, targetType.Name))
            {
                return true;
            }

            return BaseType?.IsSubclassOf(targetType) ?? false;
        }

        public bool IsTrueSubsclassOf(RType perhpasBaseClass)
        {
            RType clazz = this;
            if (clazz == perhpasBaseClass)
            {
                return false;
            }

            while (clazz != null)
            {
                if (clazz == perhpasBaseClass)
                {
                    return true;
                }

                clazz = clazz.BaseType;
            }

            return false;
        }

        public bool IsNumeric()
        {
            return Name == "Int" || Name == "Float" || Name == "Double" || Name == "Char" || Name == "Short" || Name == "Byte";
        }

        public bool IsSame(RType other)
        {
            return Name.Equals(other.Name);
        }

        public void AddInterfaceImplementation(RType interfaceType)
        {
            if (!interfaceType.IsInterface)
            {
                throw new ArgumentException("interface type is expected", nameof(interfaceType));
            }

            if (_implementedInterfaces == null)
            {
                _implementedInterfaces = new List<RType>();
            }

            _implementedInterfaces.Add(interfaceType);
        }

        public bool IsInstanceOf(RType other)
        {
            if (IsSame(other))
            {
                return true;
            }

            if (other.IsInterface && _implementedInterfaces != null)
            {
                return _implementedInterfaces.Any(i => i.IsInstanceOf(other));
            }

            if (BaseType != null)
            {
                return BaseType.IsInstanceOf(other);
            }

            return false;
        }

        public bool IsImplementsInterface(RType interfce)
        {
            if (!interfce.IsInterface)
                throw new ArgumentException("Interface is expected", nameof(interfce));

            return _implementedInterfaces != null && _implementedInterfaces.Contains(interfce);
        }

        /// <summary>
        /// <code>true</code> if specified type can be 'assigned' to the current one.
        /// <code>'x:this' = 'y:right'</code>
        /// </summary>
        /// <param name="right">some type</param>
        /// <returns><code>true</code>if type can be assigned</returns>
        public bool IsAssignable([CanBeNull] RType right)
        {
            if (right == null)
            {
                return false;
            }

            if (ReferenceEquals(right, NothingTy))
            {
                return true;
            }

            if (ReferenceEquals(this, right))
            {
                return true;
            }

            if (IsPrimitive || right.IsPrimitive)
            {
                return Name.Equals(right.Name);
            }

            if (IsCovariant(right))
            {
                return true;
            }

            if (IsInterface && right.IsImplementsInterface(this))
            {
                return true;
            }

            if (right.IsTrueSubsclassOf(this))
            {
                return true;
            }

            if (IsAssignable(right.BaseType))
            {
                return true;
            }

            return false;
        }


        protected virtual bool IsCovariant([NotNull] RType right)
        {
            return false;
        }
    }
}