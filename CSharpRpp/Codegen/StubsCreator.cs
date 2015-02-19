using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using JetBrains.Annotations;

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

            MethodBuilder methodBuilder = builder.DefineMethod(node.Name, attrs, CallingConventions.Standard);

            CodegenParams(node.Params, methodBuilder);
            methodBuilder.SetReturnType(node.RuntimeReturnType);
            node.Builder = methodBuilder;
            //node.Builder.SetReturnType(typeof(int));
            //_funcBuilders.Add(node, methodBuilder);
        }

        private static void CodegenParams([NotNull] IEnumerable<IRppParam> paramList, [NotNull] MethodBuilder methodBuilder)
        {
            Type[] parameterTypes = paramList.Select(param => param.RuntimeType).ToArray();
            methodBuilder.SetParameters(parameterTypes);
        }

    }
}