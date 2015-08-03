using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CSharpRpp.Native
{
    public class RppNativeClass : RppNamedNode, IRppClass
    {
        public IEnumerable<IRppFunc> Functions { get; private set; }
        public IRppFunc Constructor { get; private set; }
        public IEnumerable<IRppFunc> Constructors { get; private set; }
        public Type RuntimeType { get; private set; }
        public RppClassScope Scope { get; private set; }

        public RppNativeClass(Type classType) : base(classType.Name)
        {
            MethodInfo[] methods = classType.GetMethods(BindingFlags.Public | BindingFlags.Static);
            Constructor = new RppNativeFunc(classType.GetConstructor(Type.EmptyTypes));
            Constructors = classType.GetConstructors().Select(CreateConstructor).ToList();
            Functions = methods.Select(CreateFunc).ToList();
            RuntimeType = classType;
            Scope = new RppClassScope(null);
        }

        private static IRppFunc CreateFunc(MethodInfo methodInfo)
        {
            return new RppNativeFunc(methodInfo);
        }

        private static IRppFunc CreateConstructor(ConstructorInfo constructorInfo)
        {
            return new RppNativeFunc(constructorInfo);
        }
    }
}