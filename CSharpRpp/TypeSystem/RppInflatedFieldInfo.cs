﻿using System.Reflection;
using System.Reflection.Emit;

namespace CSharpRpp.TypeSystem
{
    internal class RppInflatedField : RppFieldInfo
    {
        private RType _type;
        public override RType Type => _type ?? (_type = InflateType(GenericFieldDefinition.Type));

        private FieldInfo _native;
        public override FieldInfo Native => _native ?? (_native = TypeBuilder.GetField(DeclaringType.NativeType, GenericFieldDefinition.Native));

        public override MethodInfo NativeGetter
        {
            get
            {
                try
                {
                    return TypeBuilder.GetMethod(DeclaringType.NativeType, GenericFieldDefinition.NativeGetter);
                }
                catch
                {
                    PropertyInfo property = DeclaringType.NativeType.GetProperty(Name);
                    return property.GetMethod;
                }
            }
        }

        public override MethodInfo NativeSetter => TypeBuilder.GetMethod(DeclaringType.NativeType, GenericFieldDefinition.NativeGetter);

        public RppFieldInfo GenericFieldDefinition { get; }

        private readonly RType[] _genericArguments;

        public RppInflatedField(RppFieldInfo genericFieldDefinition, RType[] genericArguments, RType declaringType)
            : base(genericFieldDefinition.Name, genericFieldDefinition.Type, genericFieldDefinition.Attributes, declaringType)
        {
            GenericFieldDefinition = genericFieldDefinition;
            _genericArguments = genericArguments;
        }

        private RType InflateType(RType type)
        {
            return _genericArguments[type.GenericParameterPosition];
        }
    }
}