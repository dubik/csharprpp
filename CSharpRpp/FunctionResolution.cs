using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CSharpRpp.Expr;
using CSharpRpp.Native;
using CSharpRpp.Parser;

namespace CSharpRpp
{
    internal class FunctionResolution
    {
        public class ResolveResults
        {
            public IRppFunc Function { get; private set; }

            public ResolveResults(IRppFunc resolvedFunc)
            {
                Function = resolvedFunc;
            }

            public virtual IRppExpr RewriteFunctionCall(string functionName, IList<IRppExpr> resolvedArgList, IList<RppType> typeArgs)
            {
                return new RppFuncCall(functionName, resolvedArgList, Function, Function.ReturnType, typeArgs);
            }
        }

        public class ClosureResolveResults : ResolveResults
        {
            private readonly RppMember _expr;

            public ClosureResolveResults(RppMember expr, IRppFunc resolvedFunc)
                : base(resolvedFunc)
            {
                _expr = expr;
            }

            public override IRppExpr RewriteFunctionCall(string functionName, IList<IRppExpr> resolvedArgList, IList<RppType> typeArgs)
            {
                return new RppSelector(new RppId(_expr.Name, _expr),
                    new RppFuncCall("apply", resolvedArgList, Function, Function.ReturnType, typeArgs));
            }
        }

        public static bool CanCast(IRppExpr source, RppType target)
        {
            return ImplicitCast.CanCast(source.Type, target);
        }

        public static bool TypesComparator(IRppExpr source, RppType target)
        {
            if (source is RppClosure)
            {
                RppClosure closure = (RppClosure) source;
                if (closure.Type == null)
                {
                    Debug.Assert(target.Runtime != null, "Only runtime is supported at this moment");
                    Type[] genericTypes = target.Runtime.GetGenericArguments();
                    RppType[] paramTypes = closure.Bindings.Select(b => b.Type).ToArray();
                    // Differentiate only by the number of arguments
                    return genericTypes.Length == (paramTypes.Length + 1); // '1' is a return type
                }
            }

            return target.Equals(source.Type);
        }

        public static ResolveResults ResolveFunction(string name, IEnumerable<IRppExpr> args, RppScope scope)
        {
            IEnumerable<IRppExpr> exprs = args as IList<IRppExpr> ?? args.ToList();
            ResolveResults res = SearchInFunctions(name, exprs, scope);
            if (res != null)
            {
                return res;
            }

            res = SearchInClosures(name, exprs, scope);
            if (res != null)
            {
                return res;
            }

            res = SearchInCompanionObjects(name, exprs, scope);
            {
                return res;
            }
        }

        private static ResolveResults SearchInFunctions(string name, IEnumerable<IRppExpr> args, RppScope scope)
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

        private static ResolveResults SearchInClosures(string name, IEnumerable<IRppExpr> args, RppScope scope)
        {
            IRppNamedNode node = scope.Lookup(name);
            if (node is IRppExpr) // () applied to expression, so it could be closure
            {
                IRppExpr expr = node as IRppExpr;

                if (expr.Type.Runtime.IsClass || expr.Type.Runtime.IsAbstract)
                {
                    RppNativeClass nativeClass = new RppNativeClass(expr.Type.Runtime);
                    var candidates = OverloadQuery.Find(args.Select(a => a.Type), nativeClass.Functions).ToList();
                    if (candidates.Count > 1)
                    {
                        throw new Exception("Can't figure out which overload to use");
                    }

                    if (candidates.Count == 0)
                    {
                        return null;
                    }

                    return new ClosureResolveResults((RppMember) expr, candidates[0]);
                }
            }

            return null;
        }

        private static ResolveResults SearchInCompanionObjects(string name, IEnumerable<IRppExpr> args, RppScope scope)
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