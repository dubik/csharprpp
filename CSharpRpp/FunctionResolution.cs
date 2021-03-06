﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Antlr.Runtime;
using CSharpRpp.Exceptions;
using CSharpRpp.Parser;
using CSharpRpp.Reporting;
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
            private readonly bool _isInsideClosure;

            public ResolveResults(RppMethodInfo resolvedFunc)
            {
                Method = resolvedFunc;
            }

            public ResolveResults(RppMethodInfo resolvedFunc, IEnumerable<RType> typeArguments, bool isInsideClosure)
            {
                Method = resolvedFunc;
                TypeArguments = typeArguments;
                _isInsideClosure = isInsideClosure;
            }

            public virtual IRppExpr RewriteFunctionCall(RType targetType, string functionName, IList<IRppExpr> resolvedArgList, IList<RType> typeArgs)
            {
                List<ResolvableType> resolvableTypeArgs = ConvertToResolvableType(GetTypeArguments(typeArgs)).ToList();

                RType returnType = GetReturnType(typeArgs);

                return new RppFuncCall(functionName, resolvedArgList, Method, new ResolvableType(returnType), resolvableTypeArgs)
                {
                    TargetType = targetType,
                    IsFromClosure = _isInsideClosure
                };
            }

            // TODO This should be replaced with inflated method, we are actually doing something like that
            private RType GetReturnType(IEnumerable<RType> typeArgs)
            {
                RType methodReturnType = Method.ReturnType;
                Debug.Assert(methodReturnType != null, "methodReturnType != null");
                RType[] genericArguments = GetTypeArguments(typeArgs).ToArray();
                RType returnType = (methodReturnType.IsGenericType || methodReturnType.IsGenericParameter) && genericArguments.Length != 0
                    ? MakeReturnType(methodReturnType, genericArguments)
                    : methodReturnType;
                return returnType;
            }

            private RType MakeReturnType(RType methodReturnType, RType[] genericArguments)
            {
                RppInflatedMethodInfo inflatedMethod = (RppInflatedMethodInfo) Method.MakeGenericType(genericArguments);
                return inflatedMethod.ReturnType;
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
                return new RppSelector(_expr,
                    new RppFuncCall(Method.Name, resolvedArgList, Method, new ResolvableType(Method.ReturnType), Collections.NoResolvableTypes));
            }
        }

        private readonly RType[] _typeArgs;
        private readonly IToken _token;

        private FunctionResolution(IToken token, IEnumerable<RType> typeArgs)
        {
            _token = token;
            _typeArgs = typeArgs.ToArray();
        }

        public static ResolveResults ResolveFunction(IToken token, string name, IEnumerable<IRppExpr> args, IEnumerable<RType> typeArgs, SymbolTable scope)
        {
            IEnumerable<IRppExpr> argsList = args as IList<IRppExpr> ?? args.ToList();
            FunctionResolution resolution = new FunctionResolution(token, typeArgs);

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

            if (overloads.IsEmpty())
            {
                return null;
            }

            DefaultTypesComparator typesComparator = new DefaultTypesComparator(_typeArgs);
            IEnumerable<IRppExpr> argList = args as IList<IRppExpr> ?? args.ToList();
            List<RppMethodInfo> candidates = OverloadQuery.Find(argList, _typeArgs, overloads, typesComparator).ToList();

            if (candidates.Count == 0 && overloads.Any())
            {
                throw SemanticExceptionFactory.CreateOverloadFailureException(_token, candidates, argList, overloads);
            }

            RppMethodInfo candidate = candidates[0];

            IEnumerable<RType> inferredTypeArguments = null;
            if (candidate.GenericParameters != null)
            {
                inferredTypeArguments = InferTypes(candidate, argList).ToList();
            }

            return new ResolveResults(candidate, inferredTypeArguments, scope.IsInsideClosure);
        }

        private IEnumerable<RType> InferTypes(RppMethodInfo candidate, IEnumerable<IRppExpr> args)
        {
            var argTypes = args.Select(a => a.Type.Value).ToList();

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
            RppId closure = new RppId(name);
            Diagnostic diagnostic = new Diagnostic();
            try
            {
                RppMember member = (RppMember) closure.Analyze(scope, diagnostic);
                RType closureType = member.Type.Value;
                if(closureType.IsObject)
                    return null;

                List<RppMethodInfo> applyMethods = closureType.Methods.Where(m => m.Name == "apply").ToList();

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
                return new ClosureResolveResults(member, candidates.First());
            }
            catch (Exception)
            {
                return null;
            }
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