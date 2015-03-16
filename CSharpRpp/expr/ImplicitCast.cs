using System;

namespace CSharpRpp.Expr
{
    public class ImplicitCast
    {
        public static IRppExpr CastIfNeeded(IRppExpr sourceExpr, Type targetType)
        {
            if (sourceExpr.Type.Runtime == targetType)
            {
                return sourceExpr;
            }

            return null;
        }
    }
}