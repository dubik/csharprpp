using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using JetBrains.Annotations;

namespace CSharpRpp.Codegen
{
    class GenericsSupport
    {
        // TODO typeParams are updated, we should find a better way, can't just pass list of objects and poke their properies
        public static void DefineGenericParams([NotNull] IList<RppVariantTypeParam> typeParams, MethodBuilder funcBuilder)
        {
            if (typeParams.Count > 0)
            {
                var genericParams = typeParams.Select(x => x.Name).ToArray();
                GenericTypeParameterBuilder[] genericTypeBuilders = funcBuilder.DefineGenericParameters(genericParams);
                typeParams.ForEachWithIndex((index, param) => param.Runtime = genericTypeBuilders[index].AsType());
            }
        }

        public static void DefineGenericParams([NotNull] IList<RppVariantTypeParam> typeParams, [NotNull] TypeBuilder classType)
        {
            if (typeParams.Count > 0)
            {
                var genericParams = typeParams.Select(x => x.Name).ToArray();
                GenericTypeParameterBuilder[] genericTypeBuilders = classType.DefineGenericParameters(genericParams);
                typeParams.ForEachWithIndex((index, param) => param.Runtime = genericTypeBuilders[index].AsType());
            }
        }
    }
}