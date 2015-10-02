using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;

namespace CSharpRpp
{
    public class TypeInference
    {
        private static readonly Dictionary<Type, Type> ftChar = new Dictionary<Type, Type>
        {
            {Types.Char, Types.Char},
            {Types.Byte, Types.Int},
            {Types.Short, Types.Int},
            {Types.Int, Types.Int},
            {Types.Long, Types.Long},
            {Types.Float, Types.Float},
            {Types.Double, Types.Double},
        };

        private static readonly Dictionary<Type, Type> ftByte = new Dictionary<Type, Type>
        {
            {Types.Char, Types.Int},
            {Types.Byte, Types.Byte},
            {Types.Short, Types.Short},
            {Types.Int, Types.Int},
            {Types.Long, Types.Long},
            {Types.Float, Types.Float},
            {Types.Double, Types.Double},
        };

        private static readonly Dictionary<Type, Type> ftShort = new Dictionary<Type, Type>
        {
            {Types.Char, Types.Int},
            {Types.Byte, Types.Short},
            {Types.Short, Types.Short},
            {Types.Int, Types.Int},
            {Types.Long, Types.Long},
            {Types.Float, Types.Float},
            {Types.Double, Types.Double},
        };

        private static readonly Dictionary<Type, Type> ftInt = new Dictionary<Type, Type>
        {
            {Types.Char, Types.Int},
            {Types.Byte, Types.Int},
            {Types.Short, Types.Int},
            {Types.Int, Types.Int},
            {Types.Long, Types.Long},
            {Types.Float, Types.Float},
            {Types.Double, Types.Double},
        };

        private static readonly Dictionary<Type, Type> ftLong = new Dictionary<Type, Type>
        {
            {Types.Char, Types.Long},
            {Types.Byte, Types.Long},
            {Types.Short, Types.Long},
            {Types.Int, Types.Long},
            {Types.Long, Types.Long},
            {Types.Float, Types.Float},
            {Types.Double, Types.Double},
        };

        private static readonly Dictionary<Type, Type> ftFloat = new Dictionary<Type, Type>
        {
            {Types.Char, Types.Float},
            {Types.Byte, Types.Float},
            {Types.Short, Types.Float},
            {Types.Int, Types.Float},
            {Types.Long, Types.Float},
            {Types.Float, Types.Float},
            {Types.Double, Types.Double},
        };

        private static readonly Dictionary<Type, Type> ftDouble = new Dictionary<Type, Type>
        {
            {Types.Char, Types.Double},
            {Types.Byte, Types.Double},
            {Types.Short, Types.Double},
            {Types.Int, Types.Double},
            {Types.Long, Types.Double},
            {Types.Float, Types.Double},
            {Types.Double, Types.Double},
        };

        private static Dictionary<Type, Dictionary<Type, Type>> convTable = new Dictionary<Type, Dictionary<Type, Type>>()
        {
            {Types.Char, ftChar},
            {Types.Byte, ftByte},
            {Types.Short, ftShort},
            {Types.Int, ftInt},
            {Types.Long, ftLong},
            {Types.Float, ftFloat},
            {Types.Double, ftDouble},
        };

        public static Type ResolveCommonType(Type left, Type right)
        {
            if (left.IsNumeric() && right.IsNumeric())
            {
                Type commonType = convTable[left][right];
                return commonType;
            }

            Debug.Fail("Not done yet");

            return null;
        }

        [NotNull]
        public static IRppExpr ReplaceUndefinedClosureTypesIfNeeded([NotNull] IRppExpr expr, RppType targetType)
        {
            if (expr is RppClosure)
            {
                RppClosure closure = (RppClosure) expr;
                var hasUndefinedClosureBinding = closure.Bindings.Any(b => b.Type.IsUndefined());
                if (targetType.IsDefined() && hasUndefinedClosureBinding)
                {
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
                        var newBindings = varType.Params.Zip(closure.Bindings, (varTypeGenArg, binding) => binding.CloneWithNewType(varTypeGenArg)).ToList();
                        return new RppClosure(newBindings, closure.Expr);
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

                    throw new NotSupportedException("Only RppGenericType and RppGenericObjectType is supported at the moment");
                }
            }

            return expr;
        }
    }
}