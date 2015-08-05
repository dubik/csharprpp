using System;

namespace CSharpRpp.Native
{
    public class RppNativeParam : RppNamedNode, IRppParam
    {
        public RppType Type { get; private set; }
        public int Index { get; set; }
        public bool IsVariadic { get; set; }

        public RppNativeParam(string name, Type paramType, bool variadic = false) : base(name)
        {
            Type = RppNativeType.Create(paramType);
            IsVariadic = variadic;
        }

        public IRppParam CloneWithNewType(RppType newType)
        {
            throw new NotImplementedException();
        }
    }
}