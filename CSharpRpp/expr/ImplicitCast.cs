using System;

namespace CSharpRpp.Expr
{
    public class ImplicitCast
    {
        public static IRppExpr CastIfNeeded(IRppExpr sourceExpr, Type targetType)
        {
            Type sourceType = sourceExpr.Type.Runtime;
            if (sourceType == targetType)
            {
                return sourceExpr;
            }

            if (sourceType.IsValueType && targetType == typeof (object))
            {
                return new RppBox(sourceExpr);
            }

            throw new Exception("Can't cast expression to a specific type");
        }
    }
}