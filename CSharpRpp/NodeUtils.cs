using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace CSharpRpp
{
    internal class NodeUtils
    {
        public static T AnalyzeNode<T>(RppScope scope, T node) where T : class, IRppNode
        {
            T analyzedNode = node.Analyze(scope) as T;
            Debug.Assert(analyzedNode != null);
            return analyzedNode;
        }

        public static void PreAnalyze(RppScope parentScope, IList<RppClass> nodes)
        {
            foreach (var node in nodes)
            {
                node.PreAnalyze(parentScope);
            }
        }

        public static IList<T> Analyze<T>(RppScope parentScope, IEnumerable<T> nodes) where T : class, IRppNode
        {
            return nodes.Select(rppClass =>
            {
                T analyzedNode = rppClass.Analyze(parentScope) as T;
                Debug.Assert(analyzedNode != null);
                return analyzedNode;
            }).ToList();
        }
    }
}