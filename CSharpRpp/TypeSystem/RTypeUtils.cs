﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace CSharpRpp.TypeSystem
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class VarianceAttribute : Attribute
    {
        public string Variance { get; }

        public VarianceAttribute(string variance)
        {
            Variance = variance;
        }
    }

    internal class RTypeUtils
    {
        public static void DefineParams(ConstructorBuilder constructorBuilder, IEnumerable<RppParameterInfo> constructorParams)
        {
            int index = 1;
            foreach (var param in constructorParams)
            {
                constructorBuilder.DefineParameter(index++, ParameterAttributes.None, RppClass.StringConstructorArgName(param.Name));
            }
        }

        public static void AssignConstructorParamIndex(IEnumerable<RppParameterInfo> parameters)
        {
            int index = 1;
            foreach (var param in parameters)
            {
                param.Index = index++;
            }
        }

        public static void DefineReturnType([NotNull] MethodBuilder method, RType returnType)
        {
            method.SetReturnType(returnType.NativeType);
        }

        public static Type[] ParametersTypes([CanBeNull] IEnumerable<RppParameterInfo> paramList)
        {
            if (paramList == null)
            {
                return Type.EmptyTypes;
            }

            return paramList.Select(p => p.Type.NativeType).ToArray();
        }

        public static void DefineParameters(MethodBuilder method, RppParameterInfo[] rppParams)
        {
            Type[] paramTypes = ParametersTypes(rppParams);
            method.SetParameters(paramTypes);

            int index = 1;
            foreach (var param in rppParams)
            {
                param.Index = index; // In static args should start from 1
                method.DefineParameter(index, ParameterAttributes.None, param.Name);
                index++;
            }
        }

        public static RTypeAttributes GetRTypeAttributes(TypeAttributes attrs, bool isValueType)
        {
            RTypeAttributes rAttrs = 0;
            TypeAttributes visibility = attrs & TypeAttributes.VisibilityMask;
            if (visibility == TypeAttributes.NotPublic)
            {
                rAttrs |= RTypeAttributes.Private;
            }
            else if (visibility == TypeAttributes.Public)
            {
                rAttrs |= RTypeAttributes.Public;
            }

            TypeAttributes classSemantics = attrs & TypeAttributes.ClassSemanticsMask;
            if (classSemantics == TypeAttributes.Class && !isValueType)
            {
                rAttrs |= RTypeAttributes.Class;
            }
            else if (classSemantics == TypeAttributes.Interface)
            {
                rAttrs |= RTypeAttributes.Interface;
            }

            if ((attrs & TypeAttributes.Sealed) != 0)
            {
                rAttrs |= RTypeAttributes.Sealed;
            }

            return rAttrs;
        }

        public static TypeAttributes GetTypeAttributes(RTypeAttributes modifiers)
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

            if (modifiers.HasFlag(RTypeAttributes.Public))
            {
                attrs |= TypeAttributes.Public;
            }

            if (modifiers.HasFlag(RTypeAttributes.Private) || modifiers.HasFlag(RTypeAttributes.Protected))
            {
                attrs |= TypeAttributes.NotPublic;
            }

            return attrs;
        }

        public static MethodAttributes GetMethodAttributes(RMethodAttributes rAttributes, bool constructor)
        {
            MethodAttributes attrs = MethodAttributes.Public;

            if (rAttributes.HasFlag(RMethodAttributes.Private))
            {
                attrs = MethodAttributes.Private;
            }

            if (rAttributes.HasFlag(RMethodAttributes.Protected))
            {
                attrs = MethodAttributes.Family;
            }

            // always virtual, even for statics but not for property accessors :)
            if (!constructor && !rAttributes.HasFlag(RMethodAttributes.Final))
            {
                attrs |= MethodAttributes.Virtual;
            }

            attrs |= MethodAttributes.HideBySig;

            if (!rAttributes.HasFlag(RMethodAttributes.Override))
            {
                if (!constructor && !rAttributes.HasFlag(RMethodAttributes.Final))
                {
                    attrs |= MethodAttributes.NewSlot;
                }
            }

            if (rAttributes.HasFlag(RMethodAttributes.Abstract))
            {
                attrs |= MethodAttributes.Abstract;
            }

            return attrs;
        }

        public static RMethodAttributes GetRMethodAttributes(MethodAttributes attributes)
        {
            RMethodAttributes attrs = 0;
            if (attributes.HasFlag(MethodAttributes.Private))
            {
                attrs |= RMethodAttributes.Private;
            }
            else
            {
                attrs |= RMethodAttributes.Public;
            }

            if (attributes.HasFlag(MethodAttributes.NewSlot))
            {
                attrs |= RMethodAttributes.Override;
            }

            return attrs;
        }

        public static void DefineNativeTypeForConstructor(TypeBuilder typeBuilder, RppMethodInfo rppConstructor)
        {
            MethodAttributes attr = GetMethodAttributes(rppConstructor.Attributes, constructor: true);
            Type[] parametersTypes = ParametersTypes(rppConstructor.Parameters);
            ConstructorBuilder constructor = typeBuilder.DefineConstructor(attr, CallingConventions.Standard, parametersTypes);
            DefineParams(constructor, rppConstructor.Parameters);
            AssignConstructorParamIndex(rppConstructor.Parameters);

            rppConstructor.Native = constructor;
        }

        public static void DefineNativeTypeFor(TypeBuilder typeBuilder, RppMethodInfo rppMethod)
        {
            MethodAttributes attr = GetMethodAttributes(rppMethod.Attributes, constructor: false);
            MethodBuilder method = typeBuilder.DefineMethod(rppMethod.Name, attr, CallingConventions.Standard);

            if (rppMethod.HasGenericParameters())
            {
                var genericParameters = rppMethod.GenericParameters;
                CreateNativeGenericParameters(genericParameters, genericParameterNames => method.DefineGenericParameters(genericParameterNames));
            }

            DefineReturnType(method, rppMethod.ReturnType);
            DefineParameters(method, rppMethod.Parameters);

            if (rppMethod.Attributes.HasFlag(RMethodAttributes.Synthesized))
            {
                method.SetCustomAttribute(CreateCompilerGeneratedAttribute());
            }

            rppMethod.Native = method;
        }

        public static void CreateNativeGenericParameters([NotNull] IEnumerable<RppGenericParameter> genericParameters,
            [NotNull] Func<string[], GenericTypeParameterBuilder[]> defineGenericParameterFunc)
        {
            var rppGenericParameters = genericParameters as RppGenericParameter[] ?? genericParameters.ToArray();
            string[] genericParameterNames = rppGenericParameters.Select(p => p.Name).ToArray();
            GenericTypeParameterBuilder[] nativeGenericParameters = defineGenericParameterFunc(genericParameterNames);
            UpdateGenericParameters(rppGenericParameters, nativeGenericParameters);
        }

        public static void UpdateGenericParameters(IEnumerable<RppGenericParameter> genericParameters,
            IEnumerable<GenericTypeParameterBuilder> nativeGenericParameters)
        {
            genericParameters.EachPair(nativeGenericParameters, UpdateGenericParameter);
        }

        private static void UpdateGenericParameter(RppGenericParameter genericParameter, GenericTypeParameterBuilder nativeGenericParameter)
        {
            genericParameter.SetGenericTypeParameterBuilder(nativeGenericParameter);
            UpdateGenericParameterContraints(genericParameter, nativeGenericParameter);
        }

        private static GenericParameterAttributes GetAttributes(RppGenericParameter parameter)
        {
            switch (parameter.Variance)
            {
                case RppGenericParameterVariance.Covariant:
                    return GenericParameterAttributes.Covariant;
                case RppGenericParameterVariance.Contravariant:
                    return GenericParameterAttributes.Contravariant;
                default:
                    return GenericParameterAttributes.None;
            }
        }

        private static void UpdateGenericParameterContraints(RppGenericParameter genericParameter, GenericTypeParameterBuilder nativeGenericParameter)
        {
            if (genericParameter.Constraint != null)
            {
                if (genericParameter.Constraint.IsClass)
                {
                    nativeGenericParameter.SetBaseTypeConstraint(genericParameter.Constraint.NativeType);
                }
                else
                {
                    nativeGenericParameter.SetInterfaceConstraints(genericParameter.Constraint.NativeType);
                }
            }
        }

        public static IEnumerable<RppGenericParameter> CreateGenericParameters(IEnumerable<string> genericParameterName, RType declaringType,
            RppMethodInfo declaringMethod = null)
        {
            int genericArgumentPosition = 0;
            foreach (var genericParamName in genericParameterName)
            {
                RppGenericParameter genericParameter = CreateGenericParameter(genericParamName, genericArgumentPosition++, declaringType, declaringMethod);
                yield return genericParameter;
            }
        }

        private static RppGenericParameter CreateGenericParameter(string name, int genericArgumentPosition, RType declaringType, RppMethodInfo declaringMethod)
        {
            RppGenericParameter genericParameter = new RppGenericParameter(name);
            RType type = new RType(name, RTypeAttributes.None, null, declaringType)
            {
                IsGenericParameter = true,
                GenericParameterPosition = genericArgumentPosition,
                GenericParameterDeclaringMethod = declaringMethod
            };
            genericParameter.Type = type;
            genericParameter.Position = genericArgumentPosition;
            return genericParameter;
        }

        public static CustomAttributeBuilder CreateCompilerGeneratedAttribute()
        {
            var compilerGeneratedAttributeCtor = typeof(CompilerGeneratedAttribute).GetConstructor(new Type[0]);
            Debug.Assert(compilerGeneratedAttributeCtor != null, "compilerGeneratedAttributeCtor != null");
            return new CustomAttributeBuilder(compilerGeneratedAttributeCtor, new object[0]);
        }

        public static CustomAttributeBuilder CreateVarianceAttribute(IEnumerable<RppGenericParameterVariance> variance)
        {
            var varianceAttributeCtor = typeof(VarianceAttribute).GetConstructor(new[] {typeof(string)});
            string encodedVariance = EncodeVariance(variance);
            Debug.Assert(varianceAttributeCtor != null, "varianceAttributeCtor != null");
            return new CustomAttributeBuilder(varianceAttributeCtor, new object[] {encodedVariance});
        }

        private static string EncodeVariance(IEnumerable<RppGenericParameterVariance> variance)
        {
            return string.Join("",
                variance.Select(v => v == RppGenericParameterVariance.Covariant ? "+" : v == RppGenericParameterVariance.Contravariant ? "-" : "_"));
        }

        public static IEnumerable<RppGenericParameterVariance> DecodeVariance(string variance)
        {
            return variance.ToCharArray()
                .Select(
                    c =>
                        c == '+'
                            ? RppGenericParameterVariance.Covariant
                            : c == '-' ? RppGenericParameterVariance.Contravariant : RppGenericParameterVariance.Invariant);
        }

        public static void AttachAttributes(RType type, TypeBuilder typeBuilder)
        {
            if (type.IsGenericType)
            {
                AttachVarianceAttribute(type.GenericParameters, typeBuilder);
            }
        }

        private static void AttachVarianceAttribute(IEnumerable<RppGenericParameter> genericParameters, TypeBuilder typeBuilder)
        {
            IEnumerable<RppGenericParameterVariance> variances = genericParameters.Select(gp => gp.Variance);
            CustomAttributeBuilder attributeBuilder = CreateVarianceAttribute(variances);
            typeBuilder.SetCustomAttribute(attributeBuilder);
        }
    }
}