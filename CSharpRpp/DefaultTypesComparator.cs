using System;
using System.Diagnostics;
using System.Linq;
using CSharpRpp.Expr;
using CSharpRpp.Parser;

namespace CSharpRpp
{
    class DefaultTypesComparator : ITypesComparator<IRppExpr>
    {
        private readonly RppScope _scope;

        public DefaultTypesComparator(RppScope scope)
        {
            _scope = scope;
        }

        public bool Compare(IRppExpr source, RppType target)
        {
            return TypesComparator(source, target);
        }

        public bool CanCast(IRppExpr source, RppType target)
        {
            return ImplicitCast.CanCast(source.Type, target);
        }


        private bool TypesComparator(IRppExpr source, RppType target)
        {
            RppType targetType = target;

            if (target.IsGenericParameter())
            {
                // TODO should be consistent with RppNew
                //targetType = RppNativeType.Create(_typeArgs.ElementAt(target.Runtime.GenericParameterPosition));
                var genericParameterName = targetType.Runtime.Name;
                targetType = _scope.LookupGenericType(genericParameterName);
            }

            if (source is RppClosure)
            {
                RppClosure closure = (RppClosure) source;
                if (closure.Type == null)
                {
                    Debug.Assert(targetType.Runtime != null, "Only runtime is supported at this moment");
                    Type[] genericTypes = targetType.Runtime.GetGenericArguments();
                    RppType[] paramTypes = closure.Bindings.Select(b => b.Type).ToArray();
                    // Differentiate only by the number of arguments
                    return genericTypes.Length == (paramTypes.Length + 1); // '1' is a return type
                }
            }

            return targetType.Equals(source.Type);
        }
    }
}