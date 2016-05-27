using System;
using System.Collections.Generic;
using System.Linq;
using CSharpRpp.Exceptions;
using CSharpRpp.Parser;
using CSharpRpp.Reporting;
using CSharpRpp.Symbols;
using CSharpRpp.TypeSystem;
using JetBrains.Annotations;

namespace CSharpRpp
{
    public class RppNew : RppNode, IRppExpr
    {
        public ResolvableType Type { get; set; }

        public IEnumerable<IRppExpr> Args => _arguments.AsEnumerable();

        private IList<IRppExpr> _arguments;

        private IEnumerable<RType> ArgsTypes => Args.Select(arg => arg.Type.Value);

        public RppMethodInfo Constructor { get; private set; }

        public RppNew([NotNull] ResolvableType type, [NotNull] IEnumerable<IRppExpr> arguments)
        {
            Type = type;
            _arguments = arguments.ToList();
        }

        public override void Accept(IRppNodeVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override IRppNode Analyze(SymbolTable scope, Diagnostic diagnostic)
        {
            // TODO should be unified with RppFuncCall and rethink how types for closures are figureout.
            // we should use constraints and type inference only, not this kind of hacks when we check target
            // closure signature and use types from there

            // Skip closures because they may have missing types
            _arguments = NodeUtils.AnalyzeWithPredicate(scope, _arguments, node => !(node is RppClosure), diagnostic);

            Type.Resolve(scope);

            RType targetType = Type.Value;

            if (NeedToInferGenericArguments(targetType))
            {
                RType inferredTargetType;
                Constructor = FindGenericConstructor(targetType, out inferredTargetType);
                Type = new ResolvableType(inferredTargetType);
            }
            else
            {
                Constructor = FindConstructor(targetType);
            }

            _arguments = RppFuncCall.ReplaceUndefinedClosureTypesIfNeeded(_arguments, Constructor.Parameters, new List<RType>());
            NodeUtils.AnalyzeWithPredicate(scope, _arguments, node => node is RppClosure, diagnostic);

            return this;
        }

        private RppMethodInfo FindGenericConstructor(RType targetType, out RType inferredType)
        {
            inferredType = null;
            IReadOnlyList<RppMethodInfo> constructors = targetType.Constructors;

            foreach (RppMethodInfo constructor in constructors)
            {
                if (NeedToInferGenericArguments(targetType))
                {
                    var genericParameters = targetType.GenericParameters;
                    List<RType> argTypes = ArgsTypes.ToList();
                    var inferredTypeArguments = InferGenericArguments(genericParameters, argTypes, constructor.Parameters.Select(p => p.Type));
                    if (inferredTypeArguments.Any(t => t.IsUndefined()))
                    {
                        continue;
                    }

                    RType inflatedType = targetType.MakeGenericType(inferredTypeArguments);
                    var matchingConstructors = FindConstructors(inflatedType);
                    if (matchingConstructors.Count > 1)
                    {
                        throw SemanticExceptionFactory.AmbiguousReferenceToOverloadedDefinition(Token, matchingConstructors, argTypes);
                    }

                    if (matchingConstructors.Count == 1)
                    {
                        inferredType = inflatedType;
                        return matchingConstructors.First();
                    }
                }
            }

            throw SemanticExceptionFactory.SomethingWentWrong(Token);
        }

        private static bool NeedToInferGenericArguments([NotNull] RType type)
        {
            return type.IsGenericTypeDefinition;
        }

        [NotNull]
        private List<RppMethodInfo> FindConstructors([NotNull] RType classType)
        {
            var constructors = classType.Constructors;

            var genericArguments = classType.GenericArguments.ToList();
            DefaultTypesComparator comparator = new DefaultTypesComparator(genericArguments.ToArray());
            List<RppMethodInfo> candidates = OverloadQuery.Find(Args, new RType[0], constructors, comparator).ToList();
            if (candidates.Count != 1)
            {
                throw SemanticExceptionFactory.CreateOverloadFailureException(Token, candidates, Args, constructors);
            }

            return candidates;
        }

        [NotNull]
        private RppMethodInfo FindConstructor([NotNull] RType classType)
        {
            return FindConstructors(classType).First();
        }

        private static RType[] InferGenericArguments(IReadOnlyCollection<RppGenericParameter> genericParameters,
            IEnumerable<RType> argTypes, IEnumerable<RType> constructorParameters)
        {
            IEnumerable<RType> sourceTypes = genericParameters.Select(gp => RppTypeSystem.Undefined).Concat(argTypes);
            IEnumerable<RType> targetTypes = genericParameters.Select(gp => gp.Type).Concat(constructorParameters);
            IEnumerable<RType> inferredTypes = TypeInference.InferTypes(sourceTypes, targetTypes).ToList();
            RType[] inferredTypeArguments = inferredTypes.Take(genericParameters.Count).ToArray();
            return inferredTypeArguments;
        }
    }
}