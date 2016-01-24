using System.Collections.Generic;
using System.Linq;
using Antlr.Runtime;
using CSharpRpp.Exceptions;
using CSharpRpp.Expr;
using CSharpRpp.Reporting;
using CSharpRpp.Symbols;
using CSharpRpp.TypeSystem;
using JetBrains.Annotations;
using static CSharpRpp.ListExtensions;

namespace CSharpRpp
{
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

        /*
        v match {
            case 1 => 3
            case 2 => 5
            case _ => 0
        }

            val x = v;
            var y;

            if(x == 1) {
                y = 3;
            } if(x == 2) {
                y = 5;
            } else {
                y = 0;
            }

            if(x == 1) {
                y = 3;
            } else {
                if(x == 2) {
                    y= 5;
                } else {
                    {
                        y = 0;
                    }
                }
            }

            y;
        */

        public override IRppNode Analyze(SymbolTable scope, Diagnostic diagnostic)
        {
            Value = (IRppExpr) Value.Analyze(scope, diagnostic);
            RppVar declIn = new RppVar(MutabilityFlag.MfVal, "<in>", Value.Type, Value);
            declIn.Analyze(scope, diagnostic);

            CaseClauses = NodeUtils.Analyze(scope, CaseClauses, diagnostic);
            CheckCommonType(CaseClauses, Token);
            Type = CaseClauses.First().Type;
            RppVar declOut = new RppVar(MutabilityFlag.MfVar, "<out>", Type, new RppDefaultExpr(Type));

            RppId declInId = new RppId("<in>", declIn);
            RppId declOutId = new RppId("<out>", declOut);

            var ifC = Create(declInId, declOutId, CaseClauses);
            var exrp = new RppBlockExpr(List<IRppNode>(declIn, declOut, ifC, declOutId));

            SymbolTable matchScope = new SymbolTable(scope);
            return exrp.Analyze(matchScope, diagnostic);
        }

        private static void CheckCommonType(IEnumerable<RppCaseClause> caseClauses, IToken token)
        {
            var returnTypes = caseClauses.Select(c => c.Type.Value).Distinct().Count();
            if (returnTypes > 1)
            {
                throw SemanticExceptionFactory.PatternMatchingCaseClausesHaveDifferentExpressionTypes(token);
            }
        }

        private static IRppExpr Create(RppId declInId, RppId declOutId, IEnumerable<RppCaseClause> caseClauses)
        {
            IEnumerable<RppCaseClause> clauses = caseClauses as IList<RppCaseClause> ?? caseClauses.ToList();
            if (clauses.IsEmpty())
            {
                return null;
            }

            RppCaseClause head = clauses.First();
            IEnumerable<RppCaseClause> tail = clauses.Skip(1);

            IRppExpr ifC = head.RewriteCaseClause(declInId, declOutId, Create(declInId, declOutId, tail));
            return ifC;
        }
    }

    public class RppCaseClause : RppNode, IRppExpr
    {
        public ResolvableType Type => Expr.Type;

        public RppMatchPattern Pattern { get; set; }

        public IRppExpr Expr { get; set; }

        public RppCaseClause([NotNull] RppMatchPattern pattern, [NotNull] IRppExpr expr)
        {
            Pattern = pattern;
            Expr = expr;
        }

        /// <summary>
        /// So we have pattern like this:
        /// case [Pattern] => [Expr]
        /// we need to figure out type of [Expr] but it can depend on variables spawned in
        /// [Pattern], so we need to get thise variables (see RppMatchPattern.DeclareVariables())
        /// and add them to the scope and then anaylize [Expr]
        /// </summary>
        public override IRppNode Analyze(SymbolTable scope, Diagnostic diagnostic)
        {
            Pattern = (RppMatchPattern) Pattern.Analyze(scope, diagnostic);

            SymbolTable localScope = new SymbolTable(scope);
            IEnumerable<IRppExpr> locals = Pattern.DeclareVariables();
            NodeUtils.Analyze(localScope, locals, diagnostic);

            Expr = (IRppExpr) Expr.Analyze(localScope, diagnostic);
            return this;
        }

        public override string ToString()
        {
            return Pattern + " => " + Expr;
        }

        public IRppExpr RewriteCaseClause(RppMember inVar, RppMember outOut, IRppExpr create)
        {
            return Pattern.RewriteCaseClause(inVar, outOut, Expr, create);
        }
    }

    public abstract class RppMatchPattern : RppNode
    {
        /// <summary>
        /// Creates RppVar(s) with new variables (if any), needed for semantic analysis
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerable<IRppExpr> DeclareVariables()
        {
            return Collections.NoExprs;
        }

        public abstract IRppExpr RewriteCaseClause(RppMember inVar, RppMember outOut, IRppExpr thenExpr, IRppExpr elseExpr);
    }

    public class RppLiteralPattern : RppMatchPattern
    {
        public IRppExpr Literal { get; set; }

        public RppLiteralPattern([NotNull] IRppExpr literal)
        {
            Literal = literal;
        }

        public override string ToString()
        {
            return Literal.ToString();
        }

        public override IRppExpr RewriteCaseClause(RppMember inVar, RppMember outOut, IRppExpr thenExpr, IRppExpr elseExpr)
        {
            return new RppIf(RppBinOp.Create("==", inVar, Literal), new RppAssignOp(outOut, thenExpr), elseExpr);
        }
    }

    public class RppVariablePattern : RppMatchPattern
    {
        public string Name { get; set; }

        public RppVariablePattern()
        {
            Name = "_";
        }

        public RppVariablePattern(string name)
        {
            Name = name;
        }

        public override string ToString()
        {
            return Name;
        }

        public override IRppExpr RewriteCaseClause(RppMember inVar, RppMember outOut, IRppExpr thenExpr, IRppExpr elseExpr)
        {
            if (Name == "_")
            {
                return new RppAssignOp(outOut, thenExpr);
            }

            throw new System.NotImplementedException();
        }
    }

    public class RppConstructorPattern : RppMatchPattern
    {
        public RppConstructorPattern(IRppExpr expr, IEnumerable<RppMatchPattern> patterns)
        {
        }

        public override IRppExpr RewriteCaseClause(RppMember inVar, RppMember outOut, IRppExpr thenExpr, IRppExpr elseExpr)
        {
            throw new System.NotImplementedException();
        }
    }

    public class RppTypedPattern : RppMatchPattern
    {
        public string Name { get; }
        private readonly ResolvableType _resolvableType;

        public RppTypedPattern(IToken varid, RTypeName typeName)
        {
            Token = varid;
            Name = varid.Text;
            _resolvableType = new ResolvableType(typeName);
        }

        public override IRppNode Analyze(SymbolTable scope, Diagnostic diagnostic)
        {
            _resolvableType.Resolve(scope);
            return this;
        }

        public override IEnumerable<IRppExpr> DeclareVariables()
        {
            RppVar variable = new RppVar(MutabilityFlag.MfVal, Name, _resolvableType, new RppDefaultExpr(_resolvableType)) {Token = Token};
            return List(variable);
        }

        public override IRppExpr RewriteCaseClause(RppMember inVar, RppMember outOut, IRppExpr thenExpr, IRppExpr elseExpr)
        {
            RppVar variable = new RppVar(MutabilityFlag.MfVal, Name, _resolvableType, new RppAsInstanceOf(inVar, _resolvableType)) {Token = Token};
            RppId variableRef = new RppId(Name, variable);
            RppIf ifCond = new RppIf(RppBinOp.Create("!=", variableRef, RppNull.Instance), new RppAssignOp(outOut, thenExpr), elseExpr);

            return new RppBlockExpr(List<IRppNode>(variable, ifCond));
        }

        public override string ToString()
        {
            return $"{Name}:{_resolvableType}";
        }
    }

    public class RppBinderPattern : RppMatchPattern
    {
        public RppBinderPattern(IToken varid, RppMatchPattern pattern)
        {
        }

        public override IRppExpr RewriteCaseClause(RppMember inVar, RppMember outOut, IRppExpr thenExpr, IRppExpr elseExpr)
        {
            throw new System.NotImplementedException();
        }
    }
}