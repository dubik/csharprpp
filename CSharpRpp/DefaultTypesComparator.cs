using System;
using System.Diagnostics;
using System.Linq;
using CSharpRpp.Expr;
using CSharpRpp.Parser;
using CSharpRpp.TypeSystem;

namespace CSharpRpp
{
    class DefaultTypesComparator : ITypesComparator<IRppExpr>
    {
        private readonly RppScope _scope;

        public DefaultTypesComparator(RppScope scope)
        {
            _scope = scope;
        }

        public bool Compare(IRppExpr source, RType target)
        {
            return TypesComparator(source, target);
        }

        public bool CanCast(IRppExpr source, RType target)
        {
            return ImplicitCast.CanCast(source.Type2.Value, target);
        }


        private bool TypesComparator(IRppExpr source, RType target)
        {
            RType targetType = target;

            /*
            if (target.IsGenericParameter())
            {
                // TODO should be consistent with RppNew
                var genericParameterName = targetType.Runtime.Name;
                targetType = _scope.LookupGenericType(genericParameterName);
            }
            */

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

            return targetType.Equals(source.Type);
        }
    }
}