using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using CSharpRpp.TypeSystem;
using JetBrains.Annotations;

namespace CSharpRpp.Codegen
{
    internal class Type2Creator : RppNodeVisitor
    {
        private RType _currentType;

        public override void VisitEnter(RppClass node)
        {
            _currentType = new RType(node.Name, GetAttributes(node.Modifiers), null, _currentType);
            node.Type2 = _currentType;
        }

        public override void VisitExit(RppFunc node)
        {
            if (node.IsConstructor)
            {
                node.MethodInfo = _currentType.DefineConstructor(GetMethodAttributes(node.Modifiers));
            }
            else
            {
                node.MethodInfo = _currentType.DefineMethod(node.Name, GetMethodAttributes(node.Modifiers));
            }
        }

        private static RTypeAttributes GetAttributes(ICollection<ObjectModifier> modifiers)
        {
            RTypeAttributes attrs = RTypeAttributes.None;
            if (modifiers.Contains(ObjectModifier.OmSealed))
            {
                attrs |= RTypeAttributes.Sealed;
            }
            if (modifiers.Contains(ObjectModifier.OmAbstract))
            {
                attrs |= RTypeAttributes.Abstract;
            }
            if (!modifiers.Contains(ObjectModifier.OmPrivate))
            {
                attrs |= RTypeAttributes.Public;
            }

            return attrs;
        }

        private static RMethodAttributes GetMethodAttributes(ICollection<ObjectModifier> modifiers)
        {
            RMethodAttributes attrs = RMethodAttributes.None;
            if (modifiers.Contains(ObjectModifier.OmOverride))
            {
                attrs |= RMethodAttributes.Override;
            }
            if (!modifiers.Contains(ObjectModifier.OmPrivate))
            {
                attrs |= RMethodAttributes.Public;
            }
            if (modifiers.Contains(ObjectModifier.OmAbstract))
            {
                attrs |= RMethodAttributes.Abstract;
            }
            return attrs;
        }
    }

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
            TypeAttributes attrs = GetTypeAttributes(node.Modifiers);
            TypeBuilder classType = _module.DefineType(node.GetNativeName(), attrs);
            IEnumerable<RppVariantTypeParam> typeParams = node.TypeParams;
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

            // always virtual, even for statics
            attrs |= MethodAttributes.Virtual;
            attrs |= MethodAttributes.HideBySig;
            if (!node.IsOverride)
            {
                attrs |= MethodAttributes.NewSlot;
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