using System;
using CSharpRpp.Expr;
using CSharpRpp.Parser;
using CSharpRpp.TypeSystem;

namespace CSharpRpp
{
    internal class DefaultTypesComparator : ITypesComparator<IRppExpr>
    {
        private readonly RType[] _genericArguments;

        public DefaultTypesComparator(RType[] genericArguments)
        {
            _genericArguments = genericArguments;
        }

        public bool Compare(IRppExpr source, RType target)
        {
            return TypesComparator(source, target);
        }

        public bool CanCast(IRppExpr source, RType target)
        {
            return ImplicitCast.CanCast(source.Type.Value, target);
        }

        private bool TypesComparator(IRppExpr source, RType target)
        {
            if (source.Type == ResolvableType.UndefinedTy)
            {
                return true;
            }

            RType sourceType = source.Type.Value;
            RType targetType = target;

            if (target.IsGenericParameter)
            {
                // Skip the check, generic arguments will be inferred
                if (_genericArguments == null || _genericArguments.Length == 0)
                {
                    return true;
                }

                targetType = _genericArguments[target.GenericParameterPosition];
            }

            if (source is RppClosure)
            {
                RppClosure closure = (RppClosure) source;
                if (closure.Type == null)
                {
                    throw new NotImplementedException("Not done");
                }
            }

            return targetType.Equals(sourceType);
        }
    }
}