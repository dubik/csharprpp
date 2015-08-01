using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using JetBrains.Annotations;

namespace CSharpRpp.Codegen
{
    class TypeCreator : RppNodeVisitor
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
            TypeAttributes attrs = GetTypeAttributes(node.Modifiers);
            TypeBuilder classType = _module.DefineType(node.GetNativeName(), attrs);
            IList<RppVariantTypeParam> typeParams = node.TypeParams;
            GenericsSupport.DefineGenericParams(typeParams, classType);

            _typeBuilders.Add(node, classType);
            node.RuntimeType = classType;
        }

        public override void VisitEnter(RppFunc node)
        {
            TypeBuilder builder = node.Class.RuntimeType as TypeBuilder;
            Debug.Assert(builder != null, "builder != null");

            var attrs = GetMethodAttributes(node);

            MethodBuilder method = builder.DefineMethod(node.Name, attrs, CallingConventions.Standard);

            GenericsSupport.DefineGenericParams(node.TypeParams, method);

            node.Builder = method;
        }

        // TODO there is inconstistency between method and type attributes, should be fixed
        private static MethodAttributes GetMethodAttributes(IRppFunc node)
        {
            MethodAttributes attrs = MethodAttributes.Private;

            if (node.IsPublic)
            {
                attrs = MethodAttributes.Public;
            }

            if (node.IsStatic)
            {
                attrs |= MethodAttributes.Static;
            }
            else
            {
                attrs |= MethodAttributes.Virtual;
            }

            if (node.IsAbstract)
            {
                attrs |= MethodAttributes.Abstract;
            }
            return attrs;
        }

        private static TypeAttributes GetTypeAttributes(ICollection<ObjectModifier> modifiers)
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