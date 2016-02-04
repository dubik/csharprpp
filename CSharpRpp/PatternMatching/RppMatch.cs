using System.Collections.Generic;
using System.Linq;
using Antlr.Runtime;
using CSharpRpp.Exceptions;
using CSharpRpp.Reporting;
using CSharpRpp.Symbols;
using CSharpRpp.TypeSystem;
using JetBrains.Annotations;
using static CSharpRpp.ListExtensions;
using static CSharpRpp.Utils.AstHelper;

namespace CSharpRpp
{
    public class RppMatchingContext
    {
        private int _localVarCounter;
        private int _localOptionsCounter;

        public string CreateLocal()
        {
            return $"localVar{_localVarCounter++}";
        }

        public string CreateLocalOption()
        {
            return $"localOption{_localOptionsCounter++}";
        }
    }

    public class RppMatch : RppNode, IRppExpr
    {
        public ResolvableType Type { get; set; }

        public IRppExpr Value;
        public IEnumerable<RppCaseClause> CaseClauses;

        public RppMatch(IRppExpr value, [NotNull] IEnumerable<RppCaseClause> caseClauses)
        {
            Value = value;
            CaseClauses = caseClauses;
        }

        public override IRppNode Analyze(SymbolTable scope, Diagnostic diagnostic)
        {
            Value = (IRppExpr) Value.Analyze(scope, diagnostic);
            RppVar declIn = new RppVar(MutabilityFlag.MfVal, "<in>", Value.Type, Value);
            declIn.Analyze(scope, diagnostic);

            CaseClauses = NodeUtils.Analyze(scope, CaseClauses, diagnostic);

            Type = CheckCommonType(CaseClauses, Token).AsResolvable();
            RppVar declOut = new RppVar(MutabilityFlag.MfVar, "<out>", Type, new RppDefaultExpr(Type));

            RppId declInId = new RppId("<in>", declIn);
            RppId declOutId = new RppId("<out>", declOut);

            RppMatchingContext ctx = new RppMatchingContext();
            var ifC = Create(declInId, declOutId, CaseClauses, ctx);
            var expr = new RppBlockExpr(List<IRppNode>(declIn, ifC)) {Exitable = true};

            SymbolTable matchScope = new SymbolTable(scope);
            RppBlockExpr matchBlock = new RppBlockExpr(List<IRppNode>(declOut, expr, declOutId));
            return matchBlock.Analyze(matchScope, diagnostic);
        }

        private static RType CheckCommonType(IEnumerable<RppCaseClause> caseClauses, IToken token)
        {
            IEnumerable<RType> types = caseClauses.Select(c => c.Type.Value).Distinct();

            RType commonType = types.Aggregate((left, right) =>
                {
                    if (left != null && left.IsAssignable(right))
                    {
                        // TODO i think this should work the other way around
                        // if right can be assigned to left we need to return left, not right
                        return right;
                    }
                    if (right != null && right.IsAssignable(left))
                    {
                        return left;
                    }
                    return null;
                });

            if (commonType == null)
            {
                throw SemanticExceptionFactory.PatternMatchingCaseClausesHaveDifferentExpressionTypes(token);
            }

            return commonType;
        }

        private static IRppExpr Create(RppMember declInId, RppMember declOutId, IEnumerable<RppCaseClause> caseClauses, RppMatchingContext ctx)
        {
            return Block(caseClauses.Select(c => c.RewriteCaseClause(declInId, declOutId, ctx)).ToList<IRppNode>());
        }
    }
}