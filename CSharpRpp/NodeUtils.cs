using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CSharpRpp.Symbols;
using System;

namespace CSharpRpp
{
    internal class NodeUtils
    {
        public static T AnalyzeNode<T>(SymbolTable scope, T node) where T : class, IRppNode
        {
            T analyzedNode = node.Analyze(scope) as T;
            Debug.Assert(analyzedNode != null);
            return analyzedNode;
        }

        public static void PreAnalyze(SymbolTable parentScope, IList<RppClass> nodes)
        {
            foreach (var node in nodes)
            {
                node.PreAnalyze(parentScope);
            }
        }

        public static IList<T> Analyze<T>(SymbolTable parentScope, IEnumerable<T> nodes) where T : class, IRppNode
        {
            return nodes.Select(node =>
            {
                T analyzedNode = node.Analyze(parentScope) as T;
                Debug.Assert(analyzedNode != null);
                return analyzedNode;
            }).ToList();
        }

        public static IList<T> AnalyzeWithPredicate<T>(SymbolTable parentScope, IEnumerable<T> nodes, Func<T, bool> pred) where T : class, IRppNode
        {
            return nodes.Select(node =>
            {
                if (pred(node))
                {
                    T analyzedNode = node.Analyze(parentScope) as T;
                    Debug.Assert(analyzedNode != null);
                    return analyzedNode;
                }

                return node;
            }).ToList();
        }
    }
}