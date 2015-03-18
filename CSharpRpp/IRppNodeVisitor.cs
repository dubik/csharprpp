using JetBrains.Annotations;

namespace CSharpRpp
{
    public interface IRppNodeVisitor
    {
        void Visit([NotNull] RppVar node);
        void Visit([NotNull] RppField node);
        void VisitEnter([NotNull] RppFunc node);
        void VisitExit([NotNull] RppFunc node);
        void VisitEnter([NotNull] RppClass node);
        void VisitExit([NotNull] RppClass node);
        void Visit([NotNull] BinOp node);
        void Visit([NotNull] RppInteger node);
        void Visit([NotNull] RppString node);
        void Visit([NotNull] RppFuncCall node);
        void Visit([NotNull] RppMessage node);
        void VisitEnter([NotNull] RppBlockExpr node);
        void VisitExit([NotNull] RppBlockExpr node);
        void Visit([NotNull] RppSelector node);
        void Visit([NotNull] RppId node);
        void Visit([NotNull] RppProgram node);
        void Visit([NotNull] RppParam node);
        void Visit([NotNull] RppNew node);
        void Visit([NotNull] RppAssignOp node);
        void Visit([NotNull] RppArray node);
        void Visit([NotNull] RppBox node);
        void Visit([NotNull] RppFloat node);
        void Visit([NotNull] RppWhile node);
        void Visit([NotNull] RppLogicalBinOp node);
        void Visit([NotNull] RppArithmBinOp node);
    }

    class RppNodeVisitor : IRppNodeVisitor
    {
        public virtual void Visit(RppVar node)
        {
        }

        public virtual void Visit(RppField node)
        {
        }

        public virtual void VisitEnter(RppFunc node)
        {
        }

        public virtual void VisitExit(RppFunc node)
        {
        }

        public virtual void VisitEnter(RppClass node)
        {
        }

        public virtual void VisitExit(RppClass node)
        {
        }

        public virtual void Visit(BinOp node)
        {
        }

        public virtual void Visit(RppInteger node)
        {
        }

        public virtual void Visit(RppFloat node)
        {
        }

        public virtual void Visit(RppWhile node)
        {
        }

        public void Visit(RppLogicalBinOp node)
        {
        }

        public void Visit(RppArithmBinOp node)
        {
        }

        public virtual void Visit(RppString node)
        {
        }

        public virtual void Visit(RppFuncCall node)
        {
        }

        public virtual void Visit(RppMessage node)
        {
        }

        public virtual void VisitEnter(RppBlockExpr node)
        {
        }

        public virtual void VisitExit(RppBlockExpr node)
        {
        }

        public virtual void Visit(RppSelector node)
        {
        }

        public virtual void Visit(RppId node)
        {
        }

        public virtual void Visit(RppProgram node)
        {
        }

        public virtual void Visit(RppParam node)
        {
        }

        public virtual void Visit(RppNew node)
        {
        }

        public virtual void Visit(RppAssignOp node)
        {
        }

        public virtual void Visit(RppArray node)
        {
        }

        public virtual void Visit(RppBox node)
        {
        }
    }
}