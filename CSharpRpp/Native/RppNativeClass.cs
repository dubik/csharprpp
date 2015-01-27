using System;

namespace CSharpRpp.Native
{
    class RppNativeClass : RppNamedNode
    {
        public RppNativeClass(Type classType) : base(classType.Name)
        {
        }
    }
}
