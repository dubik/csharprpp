using System;
using System.Collections.Generic;
using System.Linq;
using CSharpRpp.Parser;
using CSharpRpp.Symbols;
using CSharpRpp.TypeSystem;

namespace CSharpRpp
{
    class FunctionResolution
    {
        public class ResolveResults
        {
            public RppMethodInfo Function { get; }

            public ResolveResults(RppMethodInfo resolvedFunc)
            {
                Function = resolvedFunc;
            }

            public virtual IRppExpr RewriteFunctionCall(RType targetType, string functionName, IList<IRppExpr> resolvedArgList, IList<RType> typeArgs)
            {
                var resolvableTypeArgs = typeArgs.Select(t => new ResolvableType(t)).ToList();
                return new RppFuncCall(functionName, resolvedArgList, Function, new ResolvableType(Function.ReturnType), resolvableTypeArgs)
                {
                    TargetType2 = targetType
                };
            }
        }

        public class ClosureResolveResults : ResolveResults
        {
            private readonly RppMember _expr;

            public ClosureResolveResults(RppMember expr, RppMethodInfo resolvedFunc)
                : base(resolvedFunc)
            {
                _expr = expr;
            }

            // For closures we don't specify types explicitely, they are deduced during resolution
            public override IRppExpr RewriteFunctionCall(RType targetType, string functionName, IList<IRppExpr> resolvedArgList, IList<RType> unused)
            {
                return new RppSelector(new RppId(_expr.Name, _expr),
                    new RppFuncCall(Function.Name, resolvedArgList, Function, new ResolvableType(Function.ReturnType), Collections.NoResolvableTypes));
            }
        }

        private readonly RType[] _typeArgs;

        private FunctionResolution(IEnumerable<RType> typeArgs)
        {
            _typeArgs = typeArgs.ToArray();
        }

        public static ResolveResults ResolveFunction(string name, IEnumerable<IRppExpr> args, IEnumerable<RType> typeArgs,
            SymbolTable scope)
        {
            IEnumerable<IRppExpr> argsList = args as IList<IRppExpr> ?? args.ToList();
            FunctionResolution resolution = new FunctionResolution(typeArgs);

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
            return res;
        }

        private ResolveResults SearchInFunctions(string name, IEnumerable<IRppExpr> args, SymbolTable scope)
        {
            IReadOnlyCollection<RppMethodInfo> overloads = scope.LookupFunction(name);

            var candidates = OverloadQuery.Find(args, _typeArgs, overloads, new DefaultTypesComparator(_typeArgs)).ToList();
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

        private ResolveResults SearchInClosures(string name, IEnumerable<IRppExpr> args, SymbolTable scope)
        {
            Symbol symbol = scope.Lookup(name);
            if (symbol is LocalVarSymbol) // () applied to expression, so it could be closure
            {
                List<RppMethodInfo> applyMethods = symbol.Type.Methods.Where(m => m.Name == "apply").ToList();

                List<RType> argTypes = args.Select(a => a.Type.Value).ToList();
                IEnumerable<RppMethodInfo> candidates = OverloadQuery.Find(argTypes, applyMethods).ToList();

                if (candidates.Count() > 1)
                {
                    throw new Exception("Can't figure out which overload to use");
                }

                if (!candidates.Any())
                {
                    return null;
                }

                var member = (RppMember) ((LocalVarSymbol) symbol).Var; // TODO too many casts
                return new ClosureResolveResults(member, candidates.First());
            }

            return null;
        }

        private ResolveResults SearchInCompanionObjects(string name, IEnumerable<IRppExpr> args, Symbols.SymbolTable scope)
        {
            TypeSymbol obj = scope.LookupObject(name);
            var applyFunctions = obj.Type.Methods.Where(func => func.Name == "apply").ToList();
            if (applyFunctions.Count == 0)
            {
                throw new Exception("Companion object doesn't have apply() with required signature");
            }

            return new ResolveResults(applyFunctions[0]);
        }
    }
}