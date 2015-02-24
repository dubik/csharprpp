using System;

namespace CSharpRpp.Native
{
    public class RppNativeParam : RppNamedNode, IRppParam
    {
        public RppType Type { get; private set; }
        public int Index { get; set; }

        public RppNativeParam(string name, Type paramType) : base(name)
        {
            Type = RppNativeType.Create(paramType);
        }
    }
}