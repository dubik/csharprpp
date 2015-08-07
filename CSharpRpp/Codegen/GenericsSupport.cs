using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using JetBrains.Annotations;

namespace CSharpRpp.Codegen
{
    class GenericsSupport
    {
        // TODO typeParams are updated, we should find a better way, can't just pass list of objects and poke their properies
        public static void DefineGenericParams([NotNull] IEnumerable<RppVariantTypeParam> typeParams, MethodBuilder funcBuilder)
        {
            IEnumerable<RppVariantTypeParam> variantTypeParams = typeParams as IList<RppVariantTypeParam> ?? typeParams.ToList();
            if (variantTypeParams.Any())
            {
                var genericParams = variantTypeParams.Select(x => x.Name).ToArray();
                GenericTypeParameterBuilder[] genericTypeBuilders = funcBuilder.DefineGenericParameters(genericParams);
                variantTypeParams.ForEachWithIndex((index, param) => param.Runtime = genericTypeBuilders[index].AsType());
            }
        }

        public static void DefineGenericParams([NotNull] IEnumerable<RppVariantTypeParam> typeParams, [NotNull] TypeBuilder classType)
        {
            IEnumerable<RppVariantTypeParam> variantTypeParams = typeParams as RppVariantTypeParam[] ?? typeParams.ToArray();
            if (variantTypeParams.Any())
            {
                var genericParams = variantTypeParams.Select(x => x.Name).ToArray();
                GenericTypeParameterBuilder[] genericTypeBuilders = classType.DefineGenericParameters(genericParams);
                variantTypeParams.ForEachWithIndex((index, param) => param.Runtime = genericTypeBuilders[index].AsType());
            }
        }
    }
}