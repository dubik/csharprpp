using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Mono.Collections.Generic;

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

            Type[] paramTypes = ParamTypes(node.Params);
            node.Builder = builder.DefineMethod(node.Name, attrs, CallingConventions.Standard, node.RuntimeReturnType, paramTypes);
            DefineParams(node.Builder, node.Params, node.IsStatic);
        }

        private static Type[] ParamTypes([NotNull] IEnumerable<IRppParam> paramList)
        {
            return paramList.Select(param => param.RuntimeType).ToArray();
        }

        private static void DefineParams(MethodBuilder methodBuilder, IEnumerable<IRppParam> rppParams, bool isStatic)
        {
            int index = isStatic ? 0 : 1;
            foreach (var param in rppParams)
            {
                param.Index = index;
                methodBuilder.DefineParameter(index, ParameterAttributes.None, param.Name);
                index++;
            }
        }
    }
}