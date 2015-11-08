using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;
using static CSharpRpp.TypeSystem.RppTypeSystem;

namespace CSharpRpp.TypeSystem
{
    public class TypeInference
    {
        private static readonly Dictionary<Type, RType> FtChar = new Dictionary<Type, RType>
        {
            {Types.Char, CharTy},
            {Types.Byte, IntTy},
            {Types.Short, IntTy},
            {Types.Int, IntTy},
            {Types.Long, LongTy},
            {Types.Float, FloatTy},
            {Types.Double, DoubleTy},
        };

        private static readonly Dictionary<Type, RType> FtByte = new Dictionary<Type, RType>
        {
            {Types.Char, IntTy},
            {Types.Byte, ByteTy},
            {Types.Short, ShortTy},
            {Types.Int, IntTy},
            {Types.Long, LongTy},
            {Types.Float, FloatTy},
            {Types.Double, DoubleTy},
        };

        private static readonly Dictionary<Type, RType> FtShort = new Dictionary<Type, RType>
        {
            {Types.Char, IntTy},
            {Types.Byte, ShortTy},
            {Types.Short, ShortTy},
            {Types.Int, IntTy},
            {Types.Long, LongTy},
            {Types.Float, FloatTy},
            {Types.Double, DoubleTy},
        };

        private static readonly Dictionary<Type, RType> FtInt = new Dictionary<Type, RType>
        {
            {Types.Char, IntTy},
            {Types.Byte, IntTy},
            {Types.Short, IntTy},
            {Types.Int, IntTy},
            {Types.Long, LongTy},
            {Types.Float, FloatTy},
            {Types.Double, DoubleTy},
        };

        private static readonly Dictionary<Type, RType> FtLong = new Dictionary<Type, RType>
        {
            {Types.Char, LongTy},
            {Types.Byte, LongTy},
            {Types.Short, LongTy},
            {Types.Int, LongTy},
            {Types.Long, LongTy},
            {Types.Float, FloatTy},
            {Types.Double, DoubleTy},
        };

        private static readonly Dictionary<Type, RType> FtFloat = new Dictionary<Type, RType>
        {
            {Types.Char, FloatTy},
            {Types.Byte, FloatTy},
            {Types.Short, FloatTy},
            {Types.Int, FloatTy},
            {Types.Long, FloatTy},
            {Types.Float, FloatTy},
            {Types.Double, DoubleTy},
        };

        private static readonly Dictionary<Type, RType> FtDouble = new Dictionary<Type, RType>
        {
            {Types.Char, DoubleTy},
            {Types.Byte, DoubleTy},
            {Types.Short, DoubleTy},
            {Types.Int, DoubleTy},
            {Types.Long, DoubleTy},
            {Types.Float, DoubleTy},
            {Types.Double, DoubleTy},
        };

        private static readonly Dictionary<Type, Dictionary<Type, RType>> ConvTable = new Dictionary<Type, Dictionary<Type, RType>>()
        {
            {Types.Char, FtChar},
            {Types.Byte, FtByte},
            {Types.Short, FtShort},
            {Types.Int, FtInt},
            {Types.Long, FtLong},
            {Types.Float, FtFloat},
            {Types.Double, FtDouble},
        };

        public static RType ResolveCommonType(RType left, RType right)
        {
            if (left.IsNumeric() && right.IsNumeric())
            {
                Type leftNativeType = left.NativeType;
                Type rightNativeType = right.NativeType;
                RType commonType = ConvTable[leftNativeType][rightNativeType];
                return commonType;
            }

            Debug.Fail("Not done yet");

            return null;
        }

        [NotNull]
        public static IRppExpr ReplaceUndefinedClosureTypesIfNeeded([NotNull] IRppExpr expr, ResolvableType targetType)
        {
            if (expr is RppClosure)
            {
                RppClosure closure = (RppClosure) expr;
                var hasUndefinedClosureBinding = closure.Bindings.Any(b => b.Type2.IsUndefined());
                if (targetType.IsDefined() && hasUndefinedClosureBinding)
                {
                    /*
                    if (targetType is RppGenericObjectType)
                    {
                        RppGenericObjectType varType = (RppGenericObjectType) targetType;
                        var newBindgings =
                            varType.GenericArguments.Zip(closure.Bindings,
                                (varTypeGenArg, binding) => binding.CloneWithNewType(RppNativeType.Create(varTypeGenArg))).ToList();
                        return new RppClosure(newBindgings, closure.Expr);
                    }

                    if (targetType is RppGenericType)
                    {
                        RppGenericType varType = (RppGenericType) targetType;
                        //var newBindings = varType.Params.Zip(closure.Bindings, (varTypeGenArg, binding) => binding.CloneWithNewType(varTypeGenArg)).ToList();
                        //return new RppClosure(newBindings, closure.Expr);
                    }

                    if (targetType is RppNativeType)
                    {
                        Type[] genericTypes = targetType.Runtime.GetGenericArguments();
                        var newBindings =
                            genericTypes.Take(genericTypes.Length - 1) // -1 is for return type
                                .Zip(closure.Bindings, (genArg, binding) => binding.CloneWithNewType(RppNativeType.Create(genArg)))
                                .ToList();
                        return new RppClosure(newBindings, closure.Expr);
                    }
                    */
                    throw new NotSupportedException("Only RppGenericType and RppGenericObjectType is supported at the moment");
                }
            }

            return expr;
        }
    }
}