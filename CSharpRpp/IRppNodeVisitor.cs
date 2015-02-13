namespace CSharpRpp
{
    public interface IRppNodeVisitor
    {
        void Visit(RppVar node);
        void Visit(RppFunc node);
        void Visit(RppClass node);
        void Visit(BinOp node);
        void Visit(RppInteger node);
        void Visit(RppString node);
        void Visit(RppFuncCall node);
        void Visit(RppBlockExpr node);
        void Visit(RppSelector node);
        void Visit(RppId node);
        void Visit(RppProgram node);
    }

    class RppNodeVisitor : IRppNodeVisitor
    {
        public void Visit(RppVar node)
        {
        }

        public void Visit(RppFunc node)
        {
        }

        public void Visit(RppClass node)
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

        public void Visit(RppBlockExpr node)
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
    }
}