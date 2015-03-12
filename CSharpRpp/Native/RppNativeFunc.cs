using System;
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
            private set { throw new NotImplementedException(); }
        }

        public MethodInfo RuntimeType { get; set; }
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

        public RppClass Class { get; set; }

        public RppNativeFunc(MethodInfo methodInfo) : base(methodInfo.Name)
        {
            RuntimeType = methodInfo;

            ReturnType = RppNativeType.Create(methodInfo.ReturnType);
            Params = methodInfo.GetParameters().Select(CreateRppParam).ToArray();
            RuntimeReturnType = methodInfo.ReturnType;
        }

        private static IRppParam CreateRppParam(ParameterInfo paramInfo)
        {
            var variadic = paramInfo.GetCustomAttributes(typeof (ParamArrayAttribute), false).Length != 0;
            return new RppNativeParam(paramInfo.Name, paramInfo.ParameterType, variadic);
        }
    }
}