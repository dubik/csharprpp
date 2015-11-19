﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using JetBrains.Annotations;

namespace CSharpRpp.TypeSystem
{
    internal class RppInflatedMethodInfo : RppMethodInfo
    {
        public override IReadOnlyCollection<RType> GenericArguments => _genericArguments;

        public override MethodBase Native
        {
            get
            {
                if (_nativeMethod == null)
                {
                    // Reflection.Emit wants to differentiate between methods and constructors
                    if (GenericMethodDefinition.Native is ConstructorInfo)
                    {
                        _nativeMethod = TypeBuilder.GetConstructor(DeclaringType.NativeType, (ConstructorInfo) GenericMethodDefinition.Native);
                    }
                    else
                    {
                        if (DeclaringType.NativeType is TypeBuilder)
                        {
                            _nativeMethod = TypeBuilder.GetMethod(DeclaringType.NativeType, (MethodInfo) GenericMethodDefinition.Native);
                        }
                        else
                        {
                            MethodInfo method = DeclaringType.NativeType.GetMethod(Name, BindingFlags.Public | BindingFlags.Instance);
                            _nativeMethod = method;
                        }
                    }
                }

                return _nativeMethod;
            }

            set { throw new NotImplementedException(); }
        }

        private RppParameterInfo[] _parameters;
        public override RppParameterInfo[] Parameters => _parameters ?? (_parameters = InflateParameters());

        private RType _returnType;
        public override RType ReturnType => _returnType ?? (_returnType = SubstitutedType(GenericMethodDefinition.ReturnType));

        private readonly RType[] _genericArguments;
        private MethodBase _nativeMethod;

        public RppInflatedMethodInfo([NotNull] RppMethodInfo genericMethodDefinition, RType[] genericArguments, RType declaringType)
            : base(genericMethodDefinition.Name, declaringType, genericMethodDefinition.Attributes, null, new RppParameterInfo[0])
        {
            GenericMethodDefinition = genericMethodDefinition;
            _genericArguments = genericArguments;
        }

        private RppParameterInfo[] InflateParameters()
        {
            return GenericMethodDefinition?.Parameters?.Select(InflateParameter).ToArray();
        }

        private RppParameterInfo InflateParameter(RppParameterInfo parameter)
        {
            if (parameter.Type.IsGenericParameter)
            {
                var substitutedType = SubstitutedType(parameter.Type);
                return parameter.CloneWithNewType(substitutedType);
            }

            return parameter;
        }

        private RType SubstitutedType(RType type)
        {
            RType substitutedType = _genericArguments[type.GenericParameterPosition];
            return substitutedType;
        }
    }
}