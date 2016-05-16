using System;
using CSharpRpp.TypeSystem;

namespace CSharpRpp.Expr
{
    public class ImplicitCast
    {
        public static IRppExpr CastIfNeeded(IRppExpr sourceExpr, RType targetType)
        {
            RType sourceType = sourceExpr.Type.Value;
            if (sourceType.Equals(targetType))
            {
                return sourceExpr;
            }

            if (sourceType.IsPrimitive && targetType == RppTypeSystem.AnyTy)
            {
                return new RppBox(sourceExpr);
            }

            /*
            if (sourceType.IsValueType && targetType == typeof (object))
            {
                return new RppBox(sourceExpr);
            }

            if (IsAssignable(sourceExpr.Type.Runtime, targetType))
            {
                return sourceExpr;
            }

            if (sourceExpr.Type.Runtime.IsSubclassOf(targetType))
            {
                return sourceExpr;
            }
            */

            if (sourceType.IsSubclassOf(targetType))
            {
                return sourceExpr;
            }

            if (targetType.IsClass && sourceType == RppTypeSystem.NullTy)
            {
                return sourceExpr;
            }

            if (sourceType == RppTypeSystem.NothingTy)
            {
                return sourceExpr;
            }

            throw new Exception("Can't cast expression to a specific type");
        }

        public static bool CanCast(RType source, RType dest)
        {
            if (source.IsPrimitive && Equals(dest, RppTypeSystem.AnyTy))
            {
                return true;
            }

            if ((dest.IsClass || dest.IsGenericParameter) && Equals(source, RppTypeSystem.NullTy))
            {
                return true;
            }

            if (dest.IsAssignable(source))
            {
                return true;
            }

            if (Equals(source, RppTypeSystem.NothingTy))
            {
                return true;
            }

            return source.Name == dest.Name;
        }
    }
}