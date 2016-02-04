using JetBrains.Annotations;
using static CSharpRpp.Utils.AstHelper;

namespace CSharpRpp
{
    public class RppLiteralPattern : RppMatchPattern
    {
        public IRppExpr Literal { get; set; }

        public RppLiteralPattern([NotNull] IRppExpr literal)
        {
            Literal = literal;
        }

        public override string ToString()
        {
            return Literal.ToString();
        }

        public override IRppExpr RewriteCaseClause(RppMember inVar, RppMember outOut, IRppExpr thenExpr, RppMatchingContext ctx)
        {
            return If(BinOp("==", inVar, Literal), Block(Assign(outOut, thenExpr), Break));
        }
    }
}