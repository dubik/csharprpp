using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using JetBrains.Annotations;

namespace CSharpRpp.Codegen
{
    internal class TypeCreator : RppNodeVisitor
    {
        private readonly ModuleBuilder _module;
        private readonly Dictionary<RppClass, TypeBuilder> _typeBuilders;

        public TypeCreator([NotNull] ModuleBuilder module, [NotNull] Dictionary<RppClass, TypeBuilder> typeBuilders)
        {
            _module = module;
            _typeBuilders = typeBuilders;
        }

        public override void VisitEnter(RppClass node)
        {
            TypeAttributes attrs = ToTypeAttributes(node.Modifiers);
            TypeBuilder classType = _module.DefineType(node.GetNativeName(), attrs);
            if (node.TypeParams != null && node.TypeParams.Count > 0)
            {
                var genericParams = node.TypeParams.Select(x => x.Name).ToArray();
                GenericTypeParameterBuilder[] genericTypeBuilders = classType.DefineGenericParameters(genericParams);
                node.TypeParams.ForEachWithIndex((index, param) => param.Runtime = genericTypeBuilders[index].AsType());
            }

            _typeBuilders.Add(node, classType);
            node.RuntimeType = classType;
        }

        private static TypeAttributes ToTypeAttributes(HashSet<ObjectModifier> modifiers)
        {
            TypeAttributes attrs = TypeAttributes.Class;
            if (modifiers.Contains(ObjectModifier.OmAbstract))
            {
                attrs |= TypeAttributes.Abstract;
            }

            if (modifiers.Contains(ObjectModifier.OmSealed))
            {
                attrs |= TypeAttributes.Sealed;
            }

            if (modifiers.Contains(ObjectModifier.OmPrivate) || modifiers.Contains(ObjectModifier.OmProtected))
            {
                attrs |= TypeAttributes.NotPublic;
            }

            return attrs;
        }
    }
}