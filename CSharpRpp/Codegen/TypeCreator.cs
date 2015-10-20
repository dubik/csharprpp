// ----------------------------------------------------------------------
// Copyright © 2014 Microsoft Mobile. All rights reserved.
// Contact: Sergiy Dubovik <sergiy.dubovik@microsoft.com>
//  
// This software, including documentation, is protected by copyright controlled by
// Microsoft Mobile. All rights are reserved. Copying, including reproducing, storing,
// adapting or translating, any or all of this material requires the prior written consent of
// Microsoft Mobile. This material also contains confidential information which may not
// be disclosed to others without the prior written consent of Microsoft Mobile.
// ----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using CSharpRpp.TypeSystem;
using JetBrains.Annotations;

namespace CSharpRpp.Codegen
{
    public class TypeAndStub2Creator : RppNodeVisitor
    {
        private RType _currentType;

        public override void VisitEnter(RppClass node)
        {
            _currentType = new RType(node.Name, GetTypeAttributes(node), null, _currentType);
            node.Type2 = _currentType;
        }

        private static RTypeAttributes GetTypeAttributes(RppClass node)
        {
            RTypeAttributes attr = node.Kind == ClassKind.Class ? RTypeAttributes.Class : RTypeAttributes.Object;
            return attr | GetAttributes(node.Modifiers);
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

        public override void Visit(RppField node)
        {
            node.FieldInfo = _currentType.DefineField(node.Name, node.Type2.Value, GetAttributes(node));
        }

        private static RFieldAttributes GetAttributes(RppField node)
        {
            RFieldAttributes attrs = RFieldAttributes.Public;

            return attrs;
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
            if (modifiers == null)
            {
                return attrs;
            }

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

    public class StubCreator : RppNodeVisitor
    {
        private RppClass _class;

        public override void VisitEnter(RppClass node)
        {
            _class = node;
        }

        public override void VisitExit(RppFunc node)
        {
            RppMethodInfo method = node.MethodInfo;
            UpdateParameters(node, method);
            UpdateReturnType(node, method);
        }

        private void UpdateParameters(RppFunc node, RppMethodInfo method)
        {
            IRppParam[] funcParams = node.Params;
            if (funcParams.Length != 0)
            {
                RppParameterInfo[] funcParamsTypes = funcParams.Select(p => new RppParameterInfo(p.Name, ResolveType(p))).ToArray();
                method.Parameters = funcParamsTypes;
            }
        }

        private RType ResolveType(IRppExpr param)
        {
            Debug.Assert(_class.Scope != null, "_class.Scope != null");
            param.Analyze(_class.Scope);
            return param.Type2.Value;
        }

        private void UpdateReturnType(RppFunc node, RppMethodInfo method)
        {
            if (!node.IsConstructor)
            {
                node.ResolveTypes(_class.Scope);
                method.ReturnType = node.ReturnType2.Value;
            }
        }
    }

    public class TypeInitializer : RppNodeVisitor
    {
        [NotNull] private readonly ModuleBuilder _module;

        public TypeInitializer([NotNull] ModuleBuilder module)
        {
            _module = module;
        }

        public override void VisitEnter(RppClass node)
        {
            node.Type2.InitializeNativeType(_module);
        }

        private static TypeAttributes GetTypeAttributes(RTypeAttributes modifiers)
        {
            TypeAttributes attrs = TypeAttributes.Class;
            if (modifiers.HasFlag(RTypeAttributes.Abstract))
            {
                attrs |= TypeAttributes.Abstract;
            }

            if (modifiers.HasFlag(RTypeAttributes.Sealed))
            {
                attrs |= TypeAttributes.Sealed;
            }

            if (modifiers.HasFlag(RTypeAttributes.Private) || modifiers.HasFlag(RTypeAttributes.Protected))
            {
                attrs |= TypeAttributes.NotPublic;
            }

            return attrs;
        }

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
    }

    public class Type2Creator : RppNodeVisitor
    {
        public override void VisitEnter(RppClass node)
        {
            node.Type2.CreateNativeType();
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