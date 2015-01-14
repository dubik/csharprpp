using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace CSharpRpp
{
    class NodeUtils
    {
        public static T Analyze<T>(RppScope scope, T node) where T : class, IRppNode
        {
            T analyzedNode = node.Analyze(scope) as T;
            Debug.Assert(analyzedNode != null);
            return analyzedNode;
        }

        public static void PreAnalyze<T>(RppScope parentScope, IList<T> nodes) where T : class, IRppNode
        {
            foreach (var node in nodes)
            {
                RppScope scope = new RppScope(parentScope);
                node.PreAnalyze(scope);
            }
        }

        public static IList<T> Analyze<T>(RppScope parentScope, IList<T> nodes) where T : class, IRppNode
        {
            return nodes.Select(rppClass =>
            {
                RppScope scope = new RppScope(parentScope);
                T analyzedNode = rppClass.Analyze(scope) as T;
                Debug.Assert(analyzedNode != null);
                return analyzedNode;
            }).ToList();
        }
    }
}
