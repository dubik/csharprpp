using System;
using Antlr.Runtime;

namespace CSharpRpp
{
    public class RppBinderPattern : RppMatchPattern
    {
        public RppBinderPattern(IToken varid, RppMatchPattern pattern)
        {
        }

        public override IRppExpr RewriteCaseClause(RppMember inVar, RppMember outOut, IRppExpr thenExpr, RppMatchingContext ctx)
        {
            throw new NotImplementedException();
        }
    }
}