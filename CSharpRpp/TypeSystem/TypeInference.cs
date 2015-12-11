using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;
using static CSharpRpp.TypeSystem.RppTypeSystem;

namespace CSharpRpp.TypeSystem
{
    public class Types
    {
        public static Type Int = typeof (int);
        public static Type Long = typeof (long);
        public static Type Char = typeof (char);
        public static Type Short = typeof (short);
        public static Type Bool = typeof (bool);
        public static Type Byte = typeof (byte);
        public static Type Float = typeof (float);
        public static Type Double = typeof (double);
    }

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
                var hasUndefinedClosureBinding = closure.Bindings.Any(b => b.Type.IsUndefined());
                if (targetType.IsDefined() && hasUndefinedClosureBinding)
                {
                    RType type = targetType.Value;
                    IReadOnlyCollection<RType> genericArguments = type.GenericArguments;
                    List<IRppParam> newBindings =
                        genericArguments.Zip(closure.Bindings, (varTypeGenArg, binding) => binding.CloneWithNewType(varTypeGenArg)).ToList();
                    return new RppClosure(newBindings, closure.Expr);
                }
            }

            return expr;
        }

        public static IEnumerable<RType> InferTypes(IEnumerable<RType> callList, IEnumerable<RType> targetList)
        {
            Dictionary<int, RType> dict = new Dictionary<int, RType>();
            var targetTypes = targetList as IList<RType> ?? targetList.ToList();
            var stage1 = callList.Zip(targetTypes, (callTy, targetTy) => Infer(callTy, targetTy, dict)).ToList();
            var stage2 = stage1.Zip(targetTypes, (callTy, targetTy) => Infer(callTy, targetTy, dict)).ToList();
            return stage2;
        }

        private static RType Infer(RType source, RType target, IDictionary<int, RType> dict)
        {
            RType finalType;
            if (target.IsGenericParameter && dict.TryGetValue(target.GenericParameterPosition, out finalType))
            {
                return finalType;
            }

            if (IsUndefined(source))
            {
                return target;
            }

            if (target.IsGenericParameter && !dict.ContainsKey(target.GenericParameterPosition) && target.Name != source.Name)
            {
                dict.Add(target.GenericParameterPosition, source);
                return source;
            }

            if (source.IsGenericType)
            {
                var newGenericArguments =
                    source.GenericArguments.Zip(target.GenericArguments, (left, right) => Infer(left, right, dict)).ToArray();

                // Add generic arguments to dictionary in case they were resolved to some real type
                for (int i = 0; i < newGenericArguments.Length; i++)
                {
                    if (!newGenericArguments[i].IsGenericParameter && !dict.ContainsKey(i))
                    {
                        dict.Add(i, newGenericArguments[i]);
                    }
                }

                return source.DefinitionType.MakeGenericType(newGenericArguments);
            }

            return source;
        }

        private static bool IsUndefined(RType type)
        {
            return type.Name.StartsWith("Undefined");
        }
    }
}