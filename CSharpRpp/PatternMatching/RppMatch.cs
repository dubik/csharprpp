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
        private readonly Dictionary<string, int> _counts = new Dictionary<string, int>();

        public string CreateLocal()
        {
            return CreateLocal("Var");
        }

        public string CreateLocalOption()
        {
            return CreateLocal("Option");
        }

        public string CreateLocal(string name)
        {
            int count;
            if (_counts.TryGetValue(name, out count))
            {
                _counts[name] = count + 1;
            }
            else
            {
                _counts.Add(name, 1);
            }

            return $"local{name}{count}";
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
            declInId.Analyze(scope, diagnostic);
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
            RType commonType = TypeInference.ResolveCommonType(types).FirstOrDefault();

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