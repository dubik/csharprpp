﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CSharpRpp.Native
{
    public class RppNativeClass : RppNamedNode, IRppClass
    {
        public IEnumerable<IRppFunc> Functions { get; private set; }
        public IEnumerable<RppField> Fields { get; private set; }

        public IEnumerable<IRppFunc> Constructors { get; private set; }
        public IEnumerable<RppVariantTypeParam> TypeParams { get; private set; }
        public Type RuntimeType { get; private set; }
        public RppClassScope Scope { get; private set; }
        public IRppClass BaseClass { get; private set; }
        public RppBaseConstructorCall BaseConstructorCall { get; private set; }

        public RppNativeClass(Type classType) : base(classType.Name)
        {
            MethodInfo[] methods = classType.GetMethods();
            Constructors = classType.GetConstructors().Select(CreateConstructor).ToList();
            Functions = methods.Select(CreateFunc).ToList();
            FieldInfo[] fields = classType.GetFields();
            Fields = fields.Select(CreateField).ToList();
            RuntimeType = classType;
            Scope = new RppClassScope(null);
            TypeParams = classType.IsGenericType ? classType.GetGenericArguments().Select(CreateVariantTypeParam).ToList() : Collections.NoVariantTypeParams;

            if (classType.BaseType != null)
            {
                BaseClass = new RppNativeClass(classType.BaseType);
            }

            // TODO Not yet implemented, I guess we should
            BaseConstructorCall = null;
        }

        private static RppField CreateField(FieldInfo field)
        {
            return new RppField(MutabilityFlag.MF_Val, field.Name, Collections.NoStrings, RppNativeType.Create(field.FieldType));
        }

        private static RppVariantTypeParam CreateVariantTypeParam(Type type)
        {
            TypeVariant typeVariance = TypeVariant.Contravariant;
            if (type.IsGenericParameter)
            {
                var attr = type.GenericParameterAttributes;
                typeVariance = attr == GenericParameterAttributes.Covariant ? TypeVariant.Covariant : TypeVariant.Contravariant;
            }

            return new RppVariantTypeParam(type.Name, typeVariance) {Runtime = type, Type = RppNativeType.Create(type)};
        }

        private static IRppFunc CreateFunc(MethodInfo methodInfo)
        {
            return new RppNativeFunc(methodInfo);
        }

        private static IRppFunc CreateConstructor(ConstructorInfo constructorInfo)
        {
            return new RppNativeFunc(constructorInfo);
        }
    }
}