using System;
using CSharpRpp.Reporting;
using CSharpRpp.Symbols;
using CSharpRpp.TypeSystem;

namespace CSharpRpp.Expr
{
    public class RppThis : RppMember
    {
        public override ResolvableType Type { get; protected set; }

        public RppThis() : base("this")
        {
        }

        public override IRppNode Analyze(SymbolTable scope, Diagnostic diagnostic)
        {
            RType thisType = scope.GetEnclosingType();
            if (thisType == null)
            {
                throw new Exception("Can't find enclosing type for this");
            }

            Type = new ResolvableType(thisType);
            return this;
        }

        public override void Accept(IRppNodeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}