using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using CSharpRpp.TypeSystem;
using JetBrains.Annotations;

namespace CSharpRpp.Codegen
{
    /// <summary>
    /// Creates types for all classes
    /// </summary>
    public class Type2Creator : RppNodeVisitor
    {
        public override void VisitEnter(RppClass node)
        {
            RType classType = new RType(node.GetNativeName(), GetTypeAttributes(node), null, null);
            node.Type = classType;

            string[] typeParamsNames = node.TypeParams.Select(tp => tp.Name).ToArray();
            classType.DefineGenericParameters(typeParamsNames);
        }

        private static RTypeAttributes GetTypeAttributes(RppClass node)
        {
            RTypeAttributes attr = node.Kind == ClassKind.Class ? RTypeAttributes.Class : RTypeAttributes.Object;
            return attr | GetAttributes(node.Modifiers);
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
    }

    /// <summary>
    /// Resoles param types in all functions, including constructors
    /// </summary>
    public class ResolveParamTypes : RppNodeVisitor
    {
        private RppClass _currentClass;

        public override void VisitEnter(RppClass node)
        {
            _currentClass = node;
        }

        public override void VisitEnter(RppFunc node)
        {
            node.ResolveTypes(_currentClass.Scope);
        }

        public override void Visit(RppField node)
        {
            node.ResolveType(_currentClass.Scope);
        }
    }

    public class CreateRType : RppNodeVisitor
    {
        private RType _currentType;
        private RppClass _currentClass;

        public override void VisitEnter(RppClass node)
        {
            _currentClass = node;
            _currentType = node.Type;
        }

        public override void VisitExit(RppFunc node)
        {
            string methodName = node.IsConstructor ? "ctor" : node.Name;
            var rMethodAttributes = GetMethodAttributes(node.Modifiers);
            if (node.IsAbstract)
            {
                rMethodAttributes |= RMethodAttributes.Abstract;
            }

            RppMethodInfo method = _currentType.DefineMethod(methodName, rMethodAttributes);
            node.MethodInfo = method;

            if (node.TypeParams.Any())
            {
                string[] genericArgumentsNames = node.TypeParams.Select(tp => tp.Name).ToArray();
                method.DefineGenericParameters(genericArgumentsNames);
            }

            node.ResolveTypes(_currentClass.Scope);

            RppParameterInfo[] parameters = node.Params.Select(p => new RppParameterInfo(p.Name, p.Type.Value, p.IsVariadic)).ToArray();
            node.Params.ForEachWithIndex((index, p) => p.Index = index + 1); // Assign index to each parameter, 1 is for 'this'

            method.Parameters = parameters;
            method.ReturnType = node.ReturnType2.Value;
        }

        public override void Visit(RppField node)
        {
            node.ResolveType(_currentClass.Scope);
            node.FieldInfo = _currentType.DefineField(node.Name, node.Type.Value, GetAttributes(node));
        }

        private static RFieldAttributes GetAttributes(RppField node)
        {
            RFieldAttributes attrs = RFieldAttributes.Public;
            if (node.Name == "_instance")
            {
                attrs |= RFieldAttributes.Static;
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

        private static void UpdateParameters(RppFunc node, RppMethodInfo method)
        {
            IRppParam[] funcParams = node.Params;
            if (funcParams.Length != 0)
            {
                RppParameterInfo[] funcParamsTypes = funcParams.Select(p => new RppParameterInfo(p.Name, ResolveType(p))).ToArray();
                method.Parameters = funcParamsTypes;
            }
        }

        private static RType ResolveType(IRppExpr param)
        {
            return param.Type.Value;
        }

        private void UpdateReturnType(RppFunc node, RppMethodInfo method)
        {
            if (!node.IsConstructor)
            {
                method.ReturnType = node.ReturnType2.Value;
            }
        }
    }

    /// <summary>
    /// Initializing native types, we should just create a type because
    /// it can be used as a parent or parameter in other class. 
    /// </summary>
    public class InitializeNativeTypes : RppNodeVisitor
    {
        [NotNull] private readonly ModuleBuilder _module;

        public InitializeNativeTypes([NotNull] ModuleBuilder module)
        {
            _module = module;
        }

        public override void VisitEnter(RppClass node)
        {
            node.Type.InitializeNativeType(_module);
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

    public class CreateNativeTypes : RppNodeVisitor
    {
        public override void VisitEnter(RppClass node)
        {
            node.Type.CreateNativeType();
        }
    }
}