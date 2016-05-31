using System.Linq;
using CSharpRpp.Expr;
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
        void Visit([NotNull] RppBinOp node);
        void Visit([NotNull] RppInteger node);
        void Visit([NotNull] RppString node);
        void Visit([NotNull] RppFuncCall node);
        void Visit([NotNull] RppBaseConstructorCall node);
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
        void Visit([NotNull] RppBitwiseOp node);
        void Visit([NotNull] RppRelationalBinOp node);
        void Visit([NotNull] RppThrow node);
        void Visit([NotNull] RppNull node);
        void Visit([NotNull] RppClosure node);
        void Visit([NotNull] RppBooleanLiteral node);
        void Visit([NotNull] RppFieldSelector node);
        void Visit([NotNull] RppIf node);
        void Visit([NotNull] RppAsInstanceOf node);
        void Visit([NotNull] RppBreak node);
        void Visit([NotNull] RppPop node);
        void Visit([NotNull] RppThis node);
        void Visit([NotNull] RppUnary node);
    }

    public class RppNodeVisitor : IRppNodeVisitor
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

        public virtual void Visit(RppBinOp node)
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

        public virtual void Visit(RppLogicalBinOp node)
        {
        }

        public virtual void Visit(RppArithmBinOp node)
        {
        }

        public virtual void Visit(RppBitwiseOp node)
        {
        }

        public virtual void Visit(RppRelationalBinOp node)
        {
        }

        public virtual void Visit(RppThrow node)
        {
        }

        public virtual void Visit(RppNull node)
        {
        }

        public virtual void Visit(RppClosure node)
        {
        }

        public virtual void Visit(RppBooleanLiteral node)
        {
        }

        public virtual void Visit(RppFieldSelector node)
        {
        }

        public virtual void Visit(RppIf node)
        {
        }

        public virtual void Visit(RppAsInstanceOf node)
        {
        }

        public virtual void Visit(RppBreak node)
        {
        }

        public virtual void Visit(RppPop node)
        {
        }

        public virtual void Visit(RppThis node)
        {
        }

        public virtual void Visit(RppUnary node)
        {
        }

        public virtual void Visit(RppString node)
        {
        }

        public virtual void Visit(RppFuncCall node)
        {
        }

        public virtual void Visit(RppBaseConstructorCall node)
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

    public class RppAstWalker : RppNodeVisitor
    {
        public override void Visit(RppVar node)
        {
            node.InitExpr.Accept(this);
        }

        public override void Visit(RppField node)
        {
            node.InitExpr.Accept(this);
        }

        public override void Visit(RppBinOp node)
        {
            node.Left.Accept(this);
            node.Right.Accept(this);
        }

        public override void Visit(RppFuncCall node)
        {
            node.Args.ForEach(arg => arg.Accept(this));
        }

        public override void Visit(RppBaseConstructorCall node)
        {
            node.Args.ForEach(arg => arg.Accept(this));
        }

        public override void Visit(RppSelector node)
        {
            node.Target.Accept(this);
            node.Path.Accept(this);
        }

        public override void Visit(RppNew node)
        {
            node.Args.ForEach(arg => arg.Accept(this));
        }

        public override void Visit(RppAssignOp node)
        {
            node.Right.Accept(this);
            node.Left.Accept(this);
        }

        public override void Visit(RppArray node)
        {
            node.Initializers.ForEach(arg => arg.Accept(this));
        }

        public override void Visit(RppBox node)
        {
            node.Expression.Accept(this);
        }

        public override void Visit(RppWhile node)
        {
            node.Condition.Accept(this);
            node.Body.Accept(this);
        }

        public override void Visit(RppLogicalBinOp node)
        {
            node.Left.Accept(this);
            node.Right.Accept(this);
        }

        public override void Visit(RppArithmBinOp node)
        {
            node.Left.Accept(this);
            node.Right.Accept(this);
        }

        public override void Visit(RppBitwiseOp node)
        {
            node.Left.Accept(this);
            node.Right.Accept(this);
        }

        public override void Visit(RppRelationalBinOp node)
        {
            node.Left.Accept(this);
            node.Right.Accept(this);
        }

        public override void Visit(RppThrow node)
        {
            node.Expr.Accept(this);
        }

        public override void Visit(RppClosure node)
        {
            node.Expr.Accept(this);
        }

        public override void Visit(RppIf node)
        {
            node.Condition.Accept(this);
            node.ThenExpr.Accept(this);
            node.ElseExpr.Accept(this);
        }

        public override void Visit(RppAsInstanceOf node)
        {
            node.Value.Accept(this);
        }
    }
}