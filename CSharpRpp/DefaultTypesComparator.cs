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
                targetType = _genericArguments[target.GenericParameterPosition];
                // TODO should be consistent with RppNew
                //var genericParameterName = targetType.Runtime.Name;
                //targetType = _scope.LookupGenericType(genericParameterName);
            }

            if (source is RppClosure)
            {
                RppClosure closure = (RppClosure) source;
                if (closure.Type == null)
                {
                    /*
                    Debug.Assert(targetType.Runtime != null, "Only runtime is supported at this moment");
                    Type[] genericTypes = targetType.Runtime.GetGenericArguments();
                    RppType[] paramTypes = closure.Bindings.Select(b => b.Type).ToArray();
                    // Differentiate only by the number of arguments
                    return genericTypes.Length == (paramTypes.Length + 1); // '1' is a return type
                    */
                    throw new NotImplementedException("Not done");
                }
            }

            return targetType.Equals(sourceType);
        }
    }
}