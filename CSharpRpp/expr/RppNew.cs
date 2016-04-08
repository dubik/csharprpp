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

            RType classType = Type.Value;

            var constructor = FindConstructor(classType);

            _arguments = RppFuncCall.ReplaceUndefinedClosureTypesIfNeeded(_arguments, constructor.Parameters, new List<RType>());
            NodeUtils.AnalyzeWithPredicate(scope, _arguments, node => node is RppClosure, diagnostic);

            IReadOnlyCollection<RppGenericParameter> genericParameters = classType.GenericParameters;
            if (genericParameters.Count > 0)
            {
                List<RType> argTypes = Args.Select(a => a.Type.Value).ToList();
                var inferredTypeArguments = InferGenericArguments(genericParameters, argTypes, constructor.Parameters.Select(p => p.Type));
                RType inferredType = Type.Value.MakeGenericType(inferredTypeArguments);
                Type = new ResolvableType(inferredType);
                Constructor = inferredType.Constructors.First(c => c.GenericMethodDefinition == constructor);
            }
            else
            {
                Constructor = constructor;
            }

            return this;
        }

        [NotNull]
        private RppMethodInfo FindConstructor([NotNull] RType classType)
        {
            var constructors = classType.Constructors;

            var genericArguments = classType.GenericArguments.ToList();
            DefaultTypesComparator comparator = new DefaultTypesComparator(genericArguments.ToArray());
            List<RppMethodInfo> candidates = OverloadQuery.Find(Args, new RType[0], constructors, comparator).ToList();
            if (candidates.Count != 1)
            {
                throw SemanticExceptionFactory.CreateOverloadFailureException(Token, candidates, Args, constructors);
            }

            return candidates.First();
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