using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using CSharpRpp.Reporting;
using CSharpRpp.TypeSystem;
using JetBrains.Annotations;

namespace CSharpRpp.Codegen
{
    /// <summary>
    /// Creates types for all classes
    /// </summary>
    public class Type2Creator : RppNodeVisitor
    {
        private readonly Stack<RType> _outterTypes = new Stack<RType>();

        public override void VisitEnter(RppClass node)
        {
            string typeName = node.GetNativeName();
            RTypeAttributes typeAttributes = GetTypeAttributes(node);

            RType classType;

            if (_outterTypes.Any())
            {
                RType outterType = _outterTypes.Peek();
                classType = outterType.DefineNestedType(typeName, typeAttributes, null);
            }
            else
            {
                classType = RppTypeSystem.GetOrCreateType(typeName, typeAttributes, null, null);
            }

            node.Type = classType;

            _outterTypes.Push(classType);

            string[] typeParamsNames = CombineGenericParameters(node.TypeParams.Select(tp => tp.Name));
            classType.DefineGenericParameters(typeParamsNames);
        }

        /// <summary>
        /// Going up in the hierarchy and combine all generic parameters.
        /// </summary>
        /// <param name="classGenericParameters"></param>
        /// <returns>combined array</returns>
        private string[] CombineGenericParameters(IEnumerable<string> classGenericParameters)
        {
            IEnumerable<string> allGenerics = _outterTypes.Aggregate((IEnumerable<string>) new List<string>(),
                (list, type) => list.Concat(type.GenericParameters.Select(gp => gp.Name)));
            string[] typeParamsNames = allGenerics.Concat(classGenericParameters).ToArray();
            return typeParamsNames;
        }

        public override void VisitExit(RppClass node)
        {
            _outterTypes.Pop();
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
        private readonly Diagnostic _diagnostic;
        private RppClass _currentClass;

        public ResolveParamTypes(Diagnostic diagnostic)
        {
            _diagnostic = diagnostic;
        }

        public override void VisitEnter(RppClass node)
        {
            _currentClass = node;
            node.ResolveGenericTypeConstraints(_currentClass.Scope, _diagnostic);
        }

        public override void VisitEnter(RppFunc node)
        {
            node.ResolveTypes(_currentClass.Scope, _diagnostic);
        }

        public override void Visit(RppField node)
        {
            node.ResolveType(_currentClass.Scope);
        }
    }

    public class CreateRType : RppNodeVisitor
    {
        private readonly Diagnostic _diagnostic;
        private RType _currentType;
        private RppClass _currentClass;

        public CreateRType(Diagnostic diagnostic)
        {
            _diagnostic = diagnostic;
        }

        public override void VisitEnter(RppClass node)
        {
            _currentClass = node;
            _currentType = node.Type;

            ProcessTypeConstraints(node);
        }

        // class Bag[A : Item] -> resolves 'Item' and sets constraint to generic parameter
        private void ProcessTypeConstraints(RppClass node)
        {
            if (_currentType.IsGenericType)
            {
                _currentClass.ResolveGenericTypeConstraints(_currentClass.Scope, _diagnostic);
                ProcessGenerics(node.TypeParams, _currentType.GenericParameters);
            }
        }

        private static void ProcessGenerics(IEnumerable<RppVariantTypeParam> typeParams, IEnumerable<RppGenericParameter> genericParameters)
        {
            genericParameters.EachPair(typeParams, (genericParam, typeParam) =>
                {
                    genericParam.Constraint = typeParam.ConstraintType;
                    if (typeParam.ConstraintType != null)
                    {
                        if (typeParam.ConstraintType.IsClass || typeParam.ConstraintType.IsGenericParameter)
                        {
                            genericParam.Type.BaseType = typeParam.ConstraintType;
                        }
                        else
                        {
                            throw new Exception("Interfaces and value types are not supported yet");
                        }
                    }

                    genericParam.Variance = GetVariance(typeParam.Variant);
                });
        }

        public override void VisitExit(RppFunc node)
        {
            string methodName = node.IsConstructor ? "ctor" : node.Name;
            var rMethodAttributes = GetMethodAttributes(node.Modifiers);
            if (node.IsAbstract)
            {
                rMethodAttributes |= RMethodAttributes.Abstract;
            }

            if (node.IsPropertyAccessor)
            {
                rMethodAttributes |= RMethodAttributes.Final;
            }

            if (node.IsSynthesized)
            {
                rMethodAttributes |= RMethodAttributes.Synthesized;
            }

            RppMethodInfo method = _currentType.DefineMethod(methodName, rMethodAttributes);
            node.MethodInfo = method;

            if (node.TypeParams.Any())
            {
                string[] genericArgumentsNames = node.TypeParams.Select(tp => tp.Name).ToArray();
                var genericParameters = method.DefineGenericParameters(genericArgumentsNames);

                node.ResolveGenericTypeConstraints(_currentClass.Scope, _diagnostic);
                ProcessGenerics(node.TypeParams, genericParameters);
            }

            node.ResolveTypes(_currentClass.Scope, _diagnostic);

            RppParameterInfo[] parameters = node.Params.Select(p => new RppParameterInfo(p.Name, p.Type.Value, p.IsVariadic)).ToArray();
            node.Params.ForEachWithIndex((index, p) => p.Index = index + 1); // Assign index to each parameter, 1 is for 'this'

            method.Parameters = parameters;
            method.ReturnType = node.ReturnType.Value;
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
            if (modifiers.Contains(ObjectModifier.OmPrivate))
            {
                attrs |= RMethodAttributes.Private;
            }
            else if (modifiers.Contains(ObjectModifier.OmProtected))
            {
                attrs |= RMethodAttributes.Protected;
            }
            else
            {
                attrs |= RMethodAttributes.Public;
            }

            if (modifiers.Contains(ObjectModifier.OmAbstract))
            {
                attrs |= RMethodAttributes.Abstract;
            }
            return attrs;
        }

        private static RppGenericParameterVariance GetVariance(TypeVariant variant)
        {
            switch (variant)
            {
                case TypeVariant.Invariant:
                    return RppGenericParameterVariance.Invariant;
                case TypeVariant.Covariant:
                    return RppGenericParameterVariance.Covariant;
                case TypeVariant.Contravariant:
                    return RppGenericParameterVariance.Contravariant;
                default:
                    throw new ArgumentOutOfRangeException(nameof(variant), variant, null);
            }
        }
    }

    public class StubCreator : RppNodeVisitor
    {
        public override void VisitEnter(RppClass node)
        {
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

        private static void UpdateReturnType(RppFunc node, RppMethodInfo method)
        {
            if (!node.IsConstructor)
            {
                method.ReturnType = node.ReturnType.Value;
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
    }

    public class CreateNativeTypes : RppNodeVisitor
    {
        public override void VisitEnter(RppClass node)
        {
            node.Type.CreateNativeType();
        }
    }
}