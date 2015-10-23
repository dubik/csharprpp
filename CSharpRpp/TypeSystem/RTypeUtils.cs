using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using JetBrains.Annotations;

namespace CSharpRpp.TypeSystem
{
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

            // always virtual, even for statics
            if (!constructor)
            {
                attrs |= MethodAttributes.Virtual;
            }

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

        public static void DefineNativeTypeFor(TypeBuilder typeBuilder, RppConstructorInfo rppConstructor)
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
            DefineReturnType(method, rppMethod.ReturnType);
            DefineParameters(method, rppMethod.Parameters);

            rppMethod.Native = method;
        }
    }
}
