using System;
using CSharpRpp.TypeSystem;

namespace CSharpRpp.Native
{
    public class RppNativeParam : RppNamedNode, IRppParam
    {
        public RppType Type { get; }
        public ResolvableType Type2 { get; private set; }

        public int Index { get; set; }
        public bool IsVariadic { get; set; }
        public RType NewType { get; private set; }

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