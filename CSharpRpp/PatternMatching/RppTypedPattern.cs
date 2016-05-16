using System.Collections.Generic;
using Antlr.Runtime;
using CSharpRpp.Expr;
using CSharpRpp.Reporting;
using CSharpRpp.Symbols;
using CSharpRpp.TypeSystem;
using static CSharpRpp.ListExtensions;
using static CSharpRpp.Utils.AstHelper;

namespace CSharpRpp
{
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

        public override IEnumerable<IRppExpr> DeclareVariables(RType inputType)
        {
            RppVar variable = new RppVar(MutabilityFlag.MfVal, Name, _resolvableType, new RppDefaultExpr(_resolvableType)) {Token = Token};
            return List(variable);
        }

        public override IRppExpr RewriteCaseClause(RppMember inVar, RppMember outOut, IRppExpr thenExpr, RppMatchingContext ctx)
        {
            RppVar variable = new RppVar(MutabilityFlag.MfVal, Name, _resolvableType, new RppAsInstanceOf(inVar, _resolvableType)) {Token = Token};
            RppIf ifCond = If(BinOp("!=", Id(Name), NullTy), Block(Assign(outOut, thenExpr), Break), EmptyExpr);
            return Block(variable, ifCond);
        }

        public override string ToString()
        {
            return $"{Name}:{_resolvableType}";
        }
    }
}