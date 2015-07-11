using System;
using System.Collections.Generic;

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

            if (sourceExpr.Type.Runtime.IsSubclassOf(targetType))
            {
                return sourceExpr;
            }

            throw new Exception("Can't cast expression to a specific type");
        }

        private static readonly HashSet<Tuple<RppType, RppType>> _implicitConversions = new HashSet<Tuple<RppType, RppType>>()
        {
            Zip(RppPrimitiveType.CharTy, RppPrimitiveType.IntTy),
            Zip(RppPrimitiveType.ShortTy, RppPrimitiveType.IntTy),
            Zip(RppPrimitiveType.IntTy, RppPrimitiveType.IntTy),
            Zip(RppPrimitiveType.IntTy, RppPrimitiveType.FloatTy),
            Zip(RppPrimitiveType.IntTy, RppPrimitiveType.DoubleTy),
            Zip(RppPrimitiveType.FloatTy, RppPrimitiveType.DoubleTy),
        };

        private static Tuple<RppType, RppType> Zip(RppType first, RppType second)
        {
            return Tuple.Create<RppType, RppType>(first, second);
        }

        public static bool CanCast(RppType source, RppType dest)
        {
            if (source is RppPrimitiveType && dest is RppPrimitiveType)
            {
                RppPrimitiveType sourceType = (RppPrimitiveType) source;
                RppPrimitiveType destType = (RppPrimitiveType) dest;
                return _implicitConversions.Contains(Zip(sourceType, destType));
            }

            // TODO fix this somehow, its quite weird to have RppType and RppNativeType handling
            if (source is RppNativeType && dest is RppNativeType)
            {
                return source.Runtime.IsValueType && dest.Runtime == typeof (object);
            }

            return false;
        }
    }
}