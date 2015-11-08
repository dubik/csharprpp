using System;
using System.Collections.Generic;
using CSharpRpp.TypeSystem;

namespace CSharpRpp.Expr
{
    public class ImplicitCast
    {
        public static IRppExpr CastIfNeeded(IRppExpr sourceExpr, RType targetType)
        {
            RType sourceType = sourceExpr.Type2.Value;
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

            if (IsAssignableFrom(sourceExpr.Type.Runtime, targetType))
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

            throw new Exception("Can't cast expression to a specific type");
        }

        // dest s = (source) s;
        public static bool IsAssignableFrom(Type dest, Type source)
        {
            return IsSubclassOf(source, dest);
        }

        private static bool IsSubclassOf(Type sourceType, Type possibleBaseType)
        {
            if (sourceType.Name == possibleBaseType.Name)
            {
                if (sourceType.IsGenericType != possibleBaseType.IsGenericType)
                {
                    return false;
                }

                var sourceGenericTypes = sourceType.GenericTypeArguments;
                var possibleBaseGenericTypes = possibleBaseType.GenericTypeArguments;
                if (sourceGenericTypes.Length != possibleBaseGenericTypes.Length)
                {
                    return false;
                }

                for (int i = 0; i < sourceGenericTypes.Length; i++)
                {
                    if (sourceGenericTypes[i].Name != possibleBaseGenericTypes[i].Name)
                    {
                        return false;
                    }
                }
            }

            return true;
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
            return Tuple.Create(first, second);
        }

        public static bool CanCast(RType source, RType dest)
        {
            /*
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
            */

            if (source.IsPrimitive && Equals(dest, RppTypeSystem.AnyTy))
            {
                return true;
            }

            return source.Name == dest.Name;
        }
    }
}