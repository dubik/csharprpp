using System.Diagnostics;

namespace CSharpRpp
{
    public abstract class RppExpr : RppNode
    {
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
            _left.PreAnalyze(scope);
            _right.PreAnalyze(scope);
        }

        public override IRppNode Analyze(RppScope scope)
        {
            _left = _left.Analyze(scope) as RppExpr;
            Debug.Assert(_left != null);
            _right = _right.Analyze(scope) as RppExpr;
            Debug.Assert(_right != null);

            return this;
        }
    }

    public class RppInteger : RppExpr
    {
        private int _value;

        public RppInteger(string valueStr)
        {
            _value = int.Parse(valueStr);
        }
    }
}