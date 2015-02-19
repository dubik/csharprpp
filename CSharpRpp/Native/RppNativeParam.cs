using System;
using System.Reflection.Emit;

namespace CSharpRpp.Native
{
    public class RppNativeParam : RppNamedNode, IRppParam
    {
        public RppType Type { get; private set; }
        public Type RuntimeType { get; private set; }
        public int Index { get; set; }

        public RppNativeParam(string name, Type paramType) : base(name)
        {
            Type = new RppNativeType(paramType);
            RuntimeType = paramType;
        }

        public void Codegen(ILGenerator generator)
        {
            throw new NotImplementedException();
        }
    }
}