using System.Collections.Generic;
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
            TypeBuilder classType = _module.DefineType(node.Name);
            _typeBuilders.Add(node, classType);
            node.RuntimeType = classType;
        }
    }
}