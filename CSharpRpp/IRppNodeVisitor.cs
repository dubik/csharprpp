﻿using JetBrains.Annotations;

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
        void Visit([NotNull] RppNew node);
        void Visit([NotNull] RppAssignOp node);
    }

    class RppNodeVisitor : IRppNodeVisitor
    {
        public virtual void Visit(RppVar node)
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

        public virtual void Visit(RppString node)
        {
        }

        public virtual void Visit(RppFuncCall node)
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
    }
}