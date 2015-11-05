using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

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
                        _nativeMethod = TypeBuilder.GetMethod(DeclaringType.NativeType, (MethodInfo) GenericMethodDefinition.Native);
                    }
                }

                return _nativeMethod;
            }

            set { throw new NotImplementedException(); }
        }

        private readonly RType[] _genericArguments;
        private MethodBase _nativeMethod;

        public RppInflatedMethodInfo(RppMethodInfo genericMethodDefinition, RType[] genericArguments, RType declaringType)
            : base(genericMethodDefinition.Name, declaringType, genericMethodDefinition.Attributes, null, new RppParameterInfo[0])
        {
            GenericMethodDefinition = genericMethodDefinition;
            _genericArguments = genericArguments;
        }
    }
}