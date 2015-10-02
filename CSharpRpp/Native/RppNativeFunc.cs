using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace CSharpRpp.Native
{
    public class RppNativeFunc : RppNamedNode, IRppFunc
    {
        public RppType ReturnType { get; private set; }
        public Type RuntimeReturnType { get; private set; }
        public IRppParam[] Params { get; private set; }

        public IRppExpr Expr
        {
            get { throw new NotImplementedException(); }
        }

        public MethodInfo RuntimeType { get; set; }
        public ConstructorInfo ConstructorInfo { get; set; }
        public IList<RppVariantTypeParam> TypeParams { get; set; }

        public MethodBuilder Builder { get; set; }
        public ConstructorBuilder ConstructorBuilder { get; set; }

        public bool IsStatic
        {
            get { return RuntimeType.IsStatic; }
            set { throw new NotImplementedException(); }
        }

        public bool IsPublic
        {
            get { return RuntimeType.IsPublic; }
            set { throw new NotImplementedException(); }
        }

        public bool IsAbstract
        {
            get { return RuntimeType.IsAbstract; }
            set { throw new NotImplementedException(); }
        }

        public bool IsVariadic { get; set; }

        public bool IsOverride
        {
            get { return false; }
            set { throw new NotImplementedException(); }
        }

        public bool IsConstructor
        {
            get { return ConstructorInfo != null || ConstructorBuilder != null; }
        }

        public bool IsSynthesized
        {
            get { return false; }
            set { throw new NotImplementedException(); }
        }

        public bool IsStub
        {
            get { return false; }
            set { throw new NotImplementedException(); }
        }

        public RppClass Class { get; set; }

        public RppNativeFunc(ConstructorInfo constructorInfo) : base("this")
        {
            ConstructorInfo = constructorInfo;
            ReturnType = RppPrimitiveType.UnitTy;
            Params = constructorInfo.GetParameters().Select(CreateRppParam).ToArray();
            RuntimeReturnType = ReturnType.Runtime;
            IsVariadic = constructorInfo.GetParameters().Any(IsParamVariadic);

            TypeParams = Collections.NoVariantTypeParams;
        }

        public RppNativeFunc(MethodInfo methodInfo) : base(methodInfo.Name)
        {
            RuntimeType = methodInfo;

            ReturnType = RppNativeType.Create(methodInfo.ReturnType);
            Params = methodInfo.GetParameters().Select(CreateRppParam).ToArray();
            RuntimeReturnType = methodInfo.ReturnType;

            IsVariadic = methodInfo.GetParameters().Any(IsParamVariadic);

            TypeParams = methodInfo.GetGenericArguments().Select(a => new RppVariantTypeParam(a)).ToList();
        }

        private static IRppParam CreateRppParam(ParameterInfo paramInfo)
        {
            var variadic = IsParamVariadic(paramInfo);
            return new RppNativeParam(paramInfo.Name, paramInfo.ParameterType, variadic);
        }

        private static bool IsParamVariadic(ICustomAttributeProvider paramInfo)
        {
            return paramInfo.GetCustomAttributes(typeof (ParamArrayAttribute), false).Length != 0;
        }
    }
}