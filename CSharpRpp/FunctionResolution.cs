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

            public virtual IRppExpr RewriteFunctionCall(RppObjectType targetType, string functionName, IList<IRppExpr> resolvedArgList,
                IList<RType> typeArgs)
            {
                var resolvableTypeArgs = typeArgs.Select(t => new ResolvableType(t)).ToList();
                return new RppFuncCall(functionName, resolvedArgList, Function, new ResolvableType(Function.ReturnType), resolvableTypeArgs)
                {
                    TargetType = targetType
                };
            }
        }

        public class ClosureResolveResults : ResolveResults
        {
            private readonly RppMember _expr;
            private readonly IEnumerable<Type> _typeArgs;

            public ClosureResolveResults(RppMember expr, RppMethodInfo resolvedFunc, IEnumerable<Type> typeArgs)
                : base(resolvedFunc)
            {
                _expr = expr;
                _typeArgs = typeArgs;
            }

            // For closures we don't specify types explicitely, they are deduced during resolution
            public override IRppExpr RewriteFunctionCall(RppObjectType targetType, string functionName, IList<IRppExpr> resolvedArgList,
                IList<RType> unused)
            {
                /*
                var typeArgs = _typeArgs.Select(type => new RppVariantTypeParam(type));
                return new RppSelector(new RppId(_expr.Name, _expr),
                    new RppFuncCall("apply", resolvedArgList, Function, new ResolvableType(Function.ReturnType), typeArgs.ToList())
                    {
                        TargetType = (RppObjectType) _expr.Type
                    });
                    */
                throw new NotImplementedException();
            }
        }

        private RType[] _typeArgs;

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
                //IRppExpr expr = node as IRppExpr;
                throw new NotImplementedException("Not yet");
                /*
                if (expr.Type.Runtime.IsClass || expr.Type.Runtime.IsAbstract)
                {
                    IRppClass clazz = (expr.Type as RppObjectType).Class;
                    if (expr.Type is RppGenericObjectType)
                    {
                        var objectType = expr.Type as RppGenericObjectType;
                        _typeArgs = objectType.GenericArguments;
                    }

                    // We should have only one function - 'apply'
                    var candidates = clazz.Functions.ToList();
                    if (candidates.Count > 1)
                    {
                        throw new Exception("Can't figure out which overload to use");
                    }

                    if (!candidates.Any())
                    {
                        return null;
                    }

                    return new ClosureResolveResults((RppMember) expr, candidates[0], _typeArgs);
                }
                */
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