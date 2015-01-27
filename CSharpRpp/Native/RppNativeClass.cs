using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CSharpRpp.Native
{
    class RppNativeClass : RppNamedNode, IRppClass
    {
        public IEnumerable<IRppFunc> Functions { get; private set; }

        public RppNativeClass(Type classType) : base(classType.Name)
        {
            MethodInfo[] methods = classType.GetMethods(BindingFlags.Public | BindingFlags.Static);
            Functions = methods.Select(CreateFunc).ToList();
        }

        private static IRppFunc CreateFunc(MethodInfo methodInfo)
        {
            return new RppNativeFunc(methodInfo);
        }
    }
}