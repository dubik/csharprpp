using System.Collections.Generic;
using CSharpRpp;
using CSharpRpp.Expr;

namespace CSharpRppTest
{
    internal class AstNodeMatcher<T> : IRppNodeVisitor where T : class, IRppNamedNode
    {
        public IList<T> Matches = new List<T>();
        private readonly string _name;

        public AstNodeMatcher(string name)
        {
            _name = name;
        }

        private void AddIfMatch<K>(K node) where K : class, IRppNamedNode
        {
            if (node is T && node.Name == _name)
            {
                Matches.Add(node as T);
            }
        }

        public void Visit(RppVar node)
        {
            AddIfMatch(node);
        }

        public void Visit(RppField node)
        {
            AddIfMatch(node);
        }

        public void VisitEnter(RppFunc node)
        {
            AddIfMatch(node);
        }

        public void VisitExit(RppFunc node)
        {
            AddIfMatch(node);
        }

        public void VisitEnter(RppClass node)
        {
            AddIfMatch(node);
        }

        public void VisitExit(RppClass node)
        {
            AddIfMatch(node);
        }

        public void Visit(BinOp node)
        {
        }

        public void Visit(RppInteger node)
        {
        }

        public void Visit(RppString node)
        {
        }

        public void Visit(RppFuncCall node)
        {
            AddIfMatch(node);
        }

        public void Visit(RppBaseConstructorCall node)
        {
            AddIfMatch(node);
        }

        public void VisitEnter(RppBlockExpr node)
        {
        }

        public void VisitExit(RppBlockExpr node)
        {
        }

        public void Visit(RppSelector node)
        {
        }

        public void Visit(RppId node)
        {
            AddIfMatch(node);
        }

        public void Visit(RppProgram node)
        {
        }

        public void Visit(RppParam node)
        {
            AddIfMatch(node);
        }

        public void Visit(RppNew node)
        {
        }

        public void Visit(RppAssignOp node)
        {
        }

        public void Visit(RppArray node)
        {
        }

        public void Visit(RppBox node)
        {
        }

        public void Visit(RppFloat node)
        {
        }

        public void Visit(RppWhile node)
        {
        }

        public void Visit(RppLogicalBinOp node)
        {
        }

        public void Visit(RppArithmBinOp node)
        {
        }

        public void Visit(RppRelationalBinOp node)
        {
        }

        public void Visit(RppThrow node)
        {
        }

        public void Visit(RppNull node)
        {
        }

        public void Visit(RppClosure node)
        {
        }

        public void Visit(RppBooleanLiteral node)
        {
        }

        public void Visit(RppFieldSelector node)
        {
        }

        public void Visit(RppIf node)
        {
        }

        public void Visit(RppAsInstanceOf node)
        {
        }
    }
}