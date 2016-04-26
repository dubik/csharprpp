using System;
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
                    Type declaringNativeType = DeclaringType.NativeType;
                    if (GenericMethodDefinition.Native is ConstructorInfo)
                    {
                        try
                        {
                            _nativeMethod = TypeBuilder.GetConstructor(declaringNativeType, (ConstructorInfo) GenericMethodDefinition.Native);
                        }
                        catch
                        {
                            Type[] parametersTypes = Parameters.Select(p => p.Type.NativeType).ToArray();
                            try
                            {
                                _nativeMethod = declaringNativeType.GetConstructor(parametersTypes);
                            }
                            catch
                            {
                                _nativeMethod = null;
                                throw;
                            }
                        }
                    }
                    else
                    {
                        try
                        {
                            _nativeMethod = TypeBuilder.GetMethod(declaringNativeType, (MethodInfo) GenericMethodDefinition.Native);
                        }
                        catch
                        {
                            try
                            {
                                MethodInfo method = declaringNativeType.GetMethod(Name, BindingFlags.Public | BindingFlags.Instance);
                                _nativeMethod = method;
                            }
                            catch
                            {
                                _nativeMethod = null;
                            }
                        }
                    }
                }

                return _nativeMethod;
            }

            set { throw new NotImplementedException(); }
        }

        private RppParameterInfo[] _parameters;
        public override RppParameterInfo[] Parameters => _parameters ?? (_parameters = InflateParameters());

        private readonly RType _returnType;
        public override RType ReturnType => _returnType;

        private readonly RType[] _genericArguments;
        private MethodBase _nativeMethod;

        public override RppGenericParameter[] GenericParameters { get; }

        public RppInflatedMethodInfo([NotNull] RppMethodInfo genericMethodDefinition, [NotNull] RType[] genericArguments, [NotNull] RType declaringType)
            : base(genericMethodDefinition.Name, declaringType, genericMethodDefinition.Attributes, null, new RppParameterInfo[0])
        {
            GenericMethodDefinition = genericMethodDefinition;
            _genericArguments = genericArguments;

            GenericParameters = genericMethodDefinition.GenericParameters;

            _returnType = SubstitutedType(GenericMethodDefinition.ReturnType);
        }

        private RppParameterInfo[] InflateParameters()
        {
            return GenericMethodDefinition?.Parameters.Select(InflateParameter).ToArray();
        }

        private RppParameterInfo InflateParameter(RppParameterInfo parameter)
        {
            if (parameter.Type.IsGenericParameter || parameter.Type.IsGenericType)
            {
                var substitutedType = SubstitutedType(parameter.Type);
                return parameter.CloneWithNewType(substitutedType);
            }

            return parameter;
        }

        private RType SubstitutedType(RType type)
        {
            if (type.IsGenericParameter)
            {
                return GetGenericArgument(type);
            }

            if (type.IsGenericType)
            {
                RType[] mappedGenericArguments;
                // Generictype definition is an RType which contains generic params
                // Inflated type doesn't contain generic params but contains generic arguments
                // So for RType we should use GenericParameters and for RInflatedType - GenericArguments
                if (type.IsGenericTypeDefinition)
                {
                    mappedGenericArguments = type.GenericParameters.Select(ga => _genericArguments[ga.Position]).ToArray();
                }
                else
                {
                    mappedGenericArguments = type.GenericArguments.Select(ga => _genericArguments[ga.GenericParameterPosition]).ToArray();
                }

                var substitutedType = type.MakeGenericType(mappedGenericArguments);
                return substitutedType;
            }

            return type;
        }

        [NotNull]
        private RType GetGenericArgument([NotNull] RType type)
        {
            if (type.IsMethodGenericParameter)
            {
                return _genericArguments[type.GenericParameterPosition];
            }

            return _genericArguments[type.GenericParameterPosition];
        }
    }
}