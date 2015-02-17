using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

namespace CSharpRpp.Codegen
{
    class StubsCreator : RppNodeVisitor
    {
        private RppClass _class;
        private readonly Dictionary<RppFunc, MethodBuilder> _funcBuilders;

        public StubsCreator(Dictionary<RppFunc, MethodBuilder> funcBuilders)
        {
            _funcBuilders = funcBuilders;
        }

        public override void VisitEnter(RppClass node)
        {
            _class = node;
        }

        public override void VisitExit(RppClass node)
        {
            _class = null;
        }

        public override void VisitEnter(RppFunc node)
        {
            TypeBuilder builder = _class.RuntimeType as TypeBuilder;
            Debug.Assert(builder != null, "builder != null");

            MethodAttributes attrs = MethodAttributes.Private;

            if (node.IsPublic)
            {
                attrs = MethodAttributes.Public;
            }

            if (node.IsStatic)
            {
                attrs |= MethodAttributes.Static;
            }

            MethodBuilder methodBuilder = builder.DefineMethod(node.Name, attrs);
            node.Builder = methodBuilder;
            _funcBuilders.Add(node, methodBuilder);
        }
    }
}