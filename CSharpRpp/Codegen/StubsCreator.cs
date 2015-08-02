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
        private readonly Dictionary<RppFunc, MethodBuilder> _funcBuilders;

        public StubsCreator(Dictionary<RppFunc, MethodBuilder> funcBuilders)
        {
            _funcBuilders = funcBuilders;
        }

        public override void VisitEnter(RppFunc node)
        {
            MethodBuilder method = node.Builder;

            DefineReturnType(node, method);
            DefineParams(method, node.Params, node.IsStatic);

            DefineAttributes(node, method);

            _funcBuilders.Add(node, method);
        }

        private static void DefineAttributes(RppFunc node, MethodBuilder method)
        {
            if (node.IsVariadic)
            {
                ConstructorInfo constructorInfo = typeof (ParamArrayAttribute).GetConstructor(Type.EmptyTypes);
                Debug.Assert(constructorInfo != null, "constructorInfo != null");
                method.SetCustomAttribute(constructorInfo, new byte[] {1, 0, 0, 0});
            }
        }

        private static void DefineReturnType(RppFunc node, MethodBuilder method)
        {
            method.SetReturnType(node.ReturnType.Runtime);
        }

        private static Type[] ParamTypes([NotNull] IEnumerable<IRppParam> paramList)
        {
            return paramList.Select(param => param.Type.Runtime).ToArray();
        }

        private static void DefineParams(MethodBuilder method, IRppParam[] rppParams, bool isStatic)
        {
            Type[] paramTypes = ParamTypes(rppParams);
            method.SetParameters(paramTypes);

            int index = 1;
            foreach (var param in rppParams)
            {
                param.Index = isStatic ? index - 1 : index; // In static args should start from 1
                method.DefineParameter(index, ParameterAttributes.None, param.Name);
                index++;
            }
        }
    }
}