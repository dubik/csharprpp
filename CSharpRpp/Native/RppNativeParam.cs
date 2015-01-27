using System;

namespace CSharpRpp.Native
{
    public class RppNativeParam : RppNamedNode, IRppParam
    {
        public RppType Type { get; private set; }
        public Type RuntimeType { get; private set; }

        public RppNativeParam(string name, Type paramType) : base(name)
        {
            Type = new RppNativeType(paramType);
            RuntimeType = paramType;
        }
    }
}