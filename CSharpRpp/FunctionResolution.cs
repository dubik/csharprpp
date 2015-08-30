using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CSharpRpp.Expr;
using CSharpRpp.Native;
using CSharpRpp.Parser;

namespace CSharpRpp
{
    class FunctionResolution
    {
        public class ResolveResults
        {
            public IRppFunc Function { get; }

            public ResolveResults(IRppFunc resolvedFunc)
            {
                Function = resolvedFunc;
            }

            public virtual IRppExpr RewriteFunctionCall(string functionName, IList<IRppExpr> resolvedArgList, IList<RppVariantTypeParam> typeArgs)
            {
                return new RppFuncCall(functionName, resolvedArgList, Function, Function.ReturnType, typeArgs);
            }
        }

        public class ClosureResolveResults : ResolveResults
        {
            private readonly RppMember _expr;
            private readonly IEnumerable<Type> _typeArgs;

            public ClosureResolveResults(RppMember expr, IRppFunc resolvedFunc, IEnumerable<Type> typeArgs)
                : base(resolvedFunc)
            {
                _expr = expr;
                _typeArgs = typeArgs;
            }

            // For closures we don't specify types explicitely, they are deduced during resolution
            public override IRppExpr RewriteFunctionCall(string functionName, IList<IRppExpr> resolvedArgList, IList<RppVariantTypeParam> unused)
            {
                var typeArgs = _typeArgs.Select(type => new RppVariantTypeParam(type));
                return new RppSelector(new RppId(_expr.Name, _expr),
                    new RppFuncCall("apply", resolvedArgList, Function, Function.ReturnType, typeArgs.ToList()));
            }
        }


        private IEnumerable<Type> _typeArgs;

        private FunctionResolution(IEnumerable<Type> typeArgs)
        {
            _typeArgs = typeArgs;
        }

        public static ResolveResults ResolveFunction(string name, IEnumerable<IRppExpr> args, IEnumerable<RppVariantTypeParam> typeArgs, RppScope scope)
        {
            IEnumerable<IRppExpr> argsList = args as IList<IRppExpr> ?? args.ToList();
            IList<Type> typeArgsList = typeArgs.Select(a => a.Runtime).ToList();
            FunctionResolution resolution = new FunctionResolution(typeArgsList);

            ResolveResults res = resolution.SearchInFunctions(name, argsList, scope);
            if (res != null)
            {
                return res;
            }

            res = resolution.SearchInClosures(name, argsList, scope);
            if (res != null)
            {
                return res;
            }

            res = resolution.SearchInCompanionObjects(name, argsList, scope);
            {
                return res;
            }
        }

        public bool CanCast(IRppExpr source, RppType target)
        {
            return ImplicitCast.CanCast(source.Type, target);
        }

        public bool TypesComparator(IRppExpr source, RppType target)
        {
            RppType targetType = target;

            if (target.IsGenericParameter())
            {
                // TODO should be consistent with RppNew
                targetType = RppNativeType.Create(_typeArgs.ElementAt(target.Runtime.GenericParameterPosition));
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

        private ResolveResults SearchInFunctions(string name, IEnumerable<IRppExpr> args, RppScope scope)
        {
            IReadOnlyCollection<IRppFunc> overloads = scope.LookupFunction(name);
            var candidates = OverloadQuery.Find(args, overloads, TypesComparator, CanCast).ToList();
            if (candidates.Count > 1)
            {
                throw new Exception("Can't figure out which overload to use");
            }

            if (candidates.Count == 0)
            {
                return null;
            }

            return new ResolveResults(candidates[0]);
        }

        private ResolveResults SearchInClosures(string name, IEnumerable<IRppExpr> args, RppScope scope)
        {
            IRppNamedNode node = scope.Lookup(name);
            if (node is IRppExpr) // () applied to expression, so it could be closure
            {
                IRppExpr expr = node as IRppExpr;

                if (expr.Type.Runtime.IsClass || expr.Type.Runtime.IsAbstract)
                {
                    IRppClass clazz = (expr.Type as RppObjectType).Class;
                    if (expr.Type is RppGenericObjectType)
                    {
                        var objectType = expr.Type as RppGenericObjectType;
                        _typeArgs = objectType.GenericArguments;
                    }
                    var candidates = OverloadQuery.Find(args, clazz.Functions, TypesComparator, CanCast).ToList();
                    if (candidates.Count > 1)
                    {
                        throw new Exception("Can't figure out which overload to use");
                    }

                    if (candidates.Count == 0)
                    {
                        return null;
                    }

                    return new ClosureResolveResults((RppMember) expr, candidates[0], _typeArgs);
                }
            }

            return null;
        }

        private ResolveResults SearchInCompanionObjects(string name, IEnumerable<IRppExpr> args, RppScope scope)
        {
            RppClass obj = scope.LookupObject(name);
            var applyFunctions = obj.Functions.Where(func => func.Name == "apply").ToList();
            if (applyFunctions.Count == 0)
            {
                throw new Exception("Companion object doesn't have apply() with required signature");
            }

            return new ResolveResults(applyFunctions[0]);
        }
    }
}