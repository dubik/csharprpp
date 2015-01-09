using System;

namespace CSharpRpp
{
    public abstract class RppExpr : IRppNode
    {
        public abstract void PreAnalyze(RppScope scope);
        public abstract IRppNode Analyze(RppScope scope);
    }

    public class BinOp : RppExpr
    {
        private string _op;
        private RppExpr _left;
        private RppExpr _right;

        public BinOp(string op, RppExpr left, RppExpr right)
        {
            _op = op;
            _left = left;
            _right = right;
        }

        public override void PreAnalyze(RppScope scope)
        {
            throw new NotImplementedException();
        }

        public override IRppNode Analyze(RppScope scope)
        {
            throw new NotImplementedException();
        }
    }

    public class RppInteger : RppExpr
    {
        private string _valueStr;

        public RppInteger(string valueStr)
        {
            _valueStr = valueStr;
        }

        public override void PreAnalyze(RppScope scope)
        {
            throw new NotImplementedException();
        }

        public override IRppNode Analyze(RppScope scope)
        {
            throw new NotImplementedException();
        }
    }
}