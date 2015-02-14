using JetBrains.Annotations;

namespace CSharpRpp
{
    public interface IRppNodeVisitor
    {
        void Visit([NotNull] RppVar node);
        void VisitEnter([NotNull] RppFunc node);
        void VisitExit([NotNull] RppFunc node);
        void VisitEnter([NotNull] RppClass node);
        void VisitExit([NotNull] RppClass node);
        void Visit([NotNull] BinOp node);
        void Visit([NotNull] RppInteger node);
        void Visit([NotNull] RppString node);
        void Visit([NotNull] RppFuncCall node);
        void VisitEnter([NotNull] RppBlockExpr node);
        void VisitExit([NotNull] RppBlockExpr node);
        void Visit([NotNull] RppSelector node);
        void Visit([NotNull] RppId node);
        void Visit([NotNull] RppProgram node);
        void Visit([NotNull] RppParam node);
    }

    class RppNodeVisitor : IRppNodeVisitor
    {
        public void Visit(RppVar node)
        {
        }

        public void VisitEnter(RppFunc node)
        {
        }

        public void VisitExit(RppFunc node)
        {
        }

        public void VisitEnter(RppClass node)
        {
        }

        public void VisitExit(RppClass node)
        {
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
        }

        public void Visit(RppProgram node)
        {
        }

        public void Visit(RppParam node)
        {
        }
    }
}