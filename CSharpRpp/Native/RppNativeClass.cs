using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CSharpRpp.Native
{
    public class RppNativeClass : RppNamedNode, IRppClass
    {
        public IEnumerable<IRppFunc> Functions { get; private set; }
        public IEnumerable<IRppFunc> Constructors { get; private set; }
        public IEnumerable<RppVariantTypeParam> TypeParams { get; private set; }
        public Type RuntimeType { get; private set; }
        public RppClassScope Scope { get; private set; }

        public RppNativeClass(Type classType) : base(classType.Name)
        {
            MethodInfo[] methods = classType.GetMethods();
            Constructors = classType.GetConstructors().Select(CreateConstructor).ToList();
            Functions = methods.Select(CreateFunc).ToList();
            RuntimeType = classType;
            Scope = new RppClassScope(null);
            TypeParams = classType.GetGenericArguments().Select(CreateVariantTypeParam).ToList();
        }

        private static RppVariantTypeParam CreateVariantTypeParam(Type type)
        {
            var attr = type.GenericParameterAttributes;
            TypeVariant typeVariance = attr == GenericParameterAttributes.Covariant ? TypeVariant.Covariant : TypeVariant.Contravariant;
            return new RppVariantTypeParam(type.Name, typeVariance) {Runtime = type, Type = RppNativeType.Create(type)};
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