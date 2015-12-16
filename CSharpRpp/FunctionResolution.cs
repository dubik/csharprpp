using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CSharpRpp.Parser;
using CSharpRpp.Symbols;
using CSharpRpp.TypeSystem;
using JetBrains.Annotations;

namespace CSharpRpp
{
    internal class FunctionResolution
    {
        public class ResolveResults
        {
            public RppMethodInfo Method { get; }
            public IEnumerable<RType> TypeArguments { get; }

            public ResolveResults(RppMethodInfo resolvedFunc)
            {
                Method = resolvedFunc;
            }

            public ResolveResults(RppMethodInfo resolvedFunc, IEnumerable<RType> typeArguments)
            {
                Method = resolvedFunc;
                TypeArguments = typeArguments;
            }

            public virtual IRppExpr RewriteFunctionCall(RType targetType, string functionName, IList<IRppExpr> resolvedArgList, IList<RType> typeArgs)
            {
                List<ResolvableType> resolvableTypeArgs = ConvertToResolvableType(GetTypeArguments(typeArgs)).ToList();

                RType returnType = GetReturnType(typeArgs);

                return new RppFuncCall(functionName, resolvedArgList, Method, new ResolvableType(returnType), resolvableTypeArgs)
                {
                    TargetType = targetType
                };
            }

            // TODO This should be replaced with inflated method, we are actually doing something like that
            private RType GetReturnType(IEnumerable<RType> typeArgs)
            {
                RType methodReturnType = Method.ReturnType;
                Debug.Assert(methodReturnType != null, "methodReturnType != null");
                RType[] genericArguments = GetTypeArguments(typeArgs).ToArray();
                RType returnType = (methodReturnType.IsGenericType || methodReturnType.IsGenericParameter) && genericArguments.Length != 0
                    ? methodReturnType.MakeGenericType(genericArguments)
                    : methodReturnType;
                return returnType;
            }

            private IEnumerable<RType> GetTypeArguments(IEnumerable<RType> typeArgs)
            {
                IEnumerable<RType> typeArguments = typeArgs as IList<RType> ?? typeArgs.ToList();
                return !typeArguments.Any() && TypeArguments != null ? TypeArguments : typeArguments;
            }

            private static IEnumerable<ResolvableType> ConvertToResolvableType(IEnumerable<RType> types)
            {
                return types.Select(t => new ResolvableType(t));
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
                    new RppFuncCall(Method.Name, resolvedArgList, Method, new ResolvableType(Method.ReturnType), Collections.NoResolvableTypes));
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

            res = SearchInClosures(name, argsList, scope);
            if (res != null)
            {
                return res;
            }

            res = SearchInCompanionObjects(name, argsList, scope);
            return res;
        }

        private ResolveResults SearchInFunctions(string name, IEnumerable<IRppExpr> args, SymbolTable scope)
        {
            IReadOnlyCollection<RppMethodInfo> overloads = scope.LookupFunction(name);

            DefaultTypesComparator typesComparator = new DefaultTypesComparator(_typeArgs);
            IEnumerable<IRppExpr> argList = args as IList<IRppExpr> ?? args.ToList();
            List<RppMethodInfo> candidates = OverloadQuery.Find(argList, _typeArgs, overloads, typesComparator).ToList();
            if (candidates.Count > 1)
            {
                throw new Exception("Can't figure out which overload to use");
            }

            if (candidates.Count == 0)
            {
                return null;
            }

            RppMethodInfo candidate = candidates[0];

            IEnumerable<RType> inferredTypeArguments = null;
            if (candidate.GenericParameters != null)
            {
                inferredTypeArguments = InferTypes(candidate, argList).ToList();
            }

            return new ResolveResults(candidate, inferredTypeArguments);
        }

        private IEnumerable<RType> InferTypes(RppMethodInfo candidate, IEnumerable<IRppExpr> args)
        {
            var argTypes = args.Select(a => a.Type.Value).ToList();

            Tuple<int, Tuple<string, float>> k = Tuple.Create(13, Tuple.Create("Hello", 2.4f));

            List<RType> targetTypes =
                candidate.GenericParameters.Select(gp => gp.Type)
                    .Concat(candidate.Parameters.Select(p => p.Type))
                    .Concat(candidate.ReturnType).ToList();

            List<RType> sourceTypes = GetGenericArgumentsOrUndefinedTypes(_typeArgs, candidate.GenericParameters.Length)
                .Concat(argTypes).Concat(RppTypeSystem.Undefined).ToList();

            IEnumerable<RType> inferredTypes = TypeInference.InferTypes(sourceTypes, targetTypes).ToList();
            if (inferredTypes.Any(t => RppTypeSystem.Undefined.Equals(t)))
            {
                return null;
            }

            return inferredTypes.Take(candidate.GenericParameters.Length);
        }

        private static IEnumerable<RType> GetGenericArgumentsOrUndefinedTypes(IReadOnlyCollection<RType> typeArgs, int argCount)
        {
            if (typeArgs == null || typeArgs.Count == 0)
            {
                return Enumerable.Range(0, argCount).Select(ta => RppTypeSystem.Undefined);
            }

            return typeArgs;
        }

        private static ResolveResults SearchInClosures(string name, IEnumerable<IRppExpr> args, SymbolTable scope)
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

        [CanBeNull]
        private static ResolveResults SearchInCompanionObjects(string name, IEnumerable<IRppExpr> args, Symbols.SymbolTable scope)
        {
            TypeSymbol obj = scope.LookupObject(name);
            if (obj == null)
            {
                return null;
            }

            var applyFunctions = obj.Type.Methods.Where(func => func.Name == "apply").ToList();
            if (applyFunctions.Count == 0)
            {
                throw new Exception("Companion object doesn't have apply() with required signature");
            }

            return new ResolveResults(applyFunctions[0]);
        }
    }
}