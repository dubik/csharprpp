using CSharpRpp.TypeSystem;

namespace CSharpRpp
{
    public class RppAsInstanceOf : RppNode, IRppExpr
    {
        public ResolvableType Type { get; }
        public IRppExpr Value { get; }

        public RppAsInstanceOf(IRppExpr value, ResolvableType type)
        {
            Value = value;
            Type = type;
        }

        public override void Accept(IRppNodeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}