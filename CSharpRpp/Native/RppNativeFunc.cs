using System.Linq;
using System.Reflection;

namespace CSharpRpp.Native
{
    public class RppNativeFunc : RppNamedNode, IRppFunc
    {
        public MethodInfo MethodInfo { get; private set; }

        public RppType ReturnType { get; private set; }
        public IRppParam[] Params { get; private set; }

        public bool IsStatic
        {
            get { return MethodInfo.IsStatic; }
        }

        public bool IsPublic
        {
            get { return MethodInfo.IsPublic; }
        }

        public bool IsAbstract
        {
            get { return MethodInfo.IsAbstract; }
        }

        public RppNativeFunc(MethodInfo methodInfo) : base(methodInfo.Name)
        {
            MethodInfo = methodInfo;
            ReturnType = RppNativeType.Create(methodInfo.ReturnType);
            Params = methodInfo.GetParameters().Select(CreateRppParam).ToArray();
        }

        private static IRppParam CreateRppParam(ParameterInfo paramInfo)
        {
            return new RppNativeParam(paramInfo.Name, paramInfo.ParameterType);
        }
    }
}