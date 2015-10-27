using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace CSharpRpp
{
    internal class NodeUtils
    {
        public static T AnalyzeNode<T>(Symbols.SymbolTable scope, T node) where T : class, IRppNode
        {
            T analyzedNode = node.Analyze(scope) as T;
            Debug.Assert(analyzedNode != null);
            return analyzedNode;
        }

        public static void PreAnalyze(Symbols.SymbolTable parentScope, IList<RppClass> nodes)
        {
            foreach (var node in nodes)
            {
                node.PreAnalyze(parentScope);
            }
        }

        public static IList<T> Analyze<T>(Symbols.SymbolTable parentScope, IEnumerable<T> nodes) where T : class, IRppNode
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