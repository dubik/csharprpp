using System.Collections.Generic;
using System.Diagnostics;
using CSharpRpp.Reporting;
using CSharpRpp.Symbols;
using CSharpRpp.TypeSystem;
using JetBrains.Annotations;

namespace CSharpRpp
{
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
            RType inputType = GetInputType(localScope);
            IEnumerable<IRppExpr> locals = Pattern.DeclareVariables(inputType);
            NodeUtils.Analyze(localScope, locals, diagnostic);

            Expr = (IRppExpr) Expr.Analyze(localScope, diagnostic);
            return this;
        }

        public override string ToString()
        {
            return Pattern + " => " + Expr;
        }

        public IRppExpr RewriteCaseClause(RppMember inVar, RppMember outOut, RppMatchingContext ctx)
        {
            return Pattern.RewriteCaseClause(inVar, outOut, Expr, ctx);
        }

        [NotNull]
        protected static RType GetInputType([NotNull] SymbolTable scope)
        {
            LocalVarSymbol entry = (LocalVarSymbol) scope.Lookup("<in>");
            Debug.Assert(entry != null, "input variable must exists");
            return entry.Type;
        }
    }
}