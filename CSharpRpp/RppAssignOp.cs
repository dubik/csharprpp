using System;
using System.Collections.Generic;
using System.Linq;
using CSharpRpp.Exceptions;
using CSharpRpp.Reporting;
using CSharpRpp.Symbols;
using CSharpRpp.TypeSystem;
using JetBrains.Annotations;

namespace CSharpRpp
{
    public class RppAssignOp : BinOp
    {
        public RppAssignOp([NotNull] IRppExpr left, [NotNull] IRppExpr right) : base("=", left, right)
        {
            Type = ResolvableType.UnitTy;
        }

        public override void Accept(IRppNodeVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override IRppNode Analyze(SymbolTable scope, Diagnostic diagnostic)
        {
            base.Analyze(scope, diagnostic);

            // Rewrite assignment to function call when assigned to array, e.g. array(index) = value => array.update(index, value)
            if (Left is RppSelector)
            {
                RppSelector selector = (RppSelector) Left;
                if (selector.Path.Name == "apply")
                {
                    RppFuncCall applyFuncCall = selector.Path as RppFuncCall;
                    if (applyFuncCall != null && applyFuncCall.Function.DeclaringType.Name == "Array")
                    {
                        RppSelector updateArray = new RppSelector(selector.Target, new RppFuncCall("update",
                            new List<IRppExpr> {applyFuncCall.Args.First(), Right}));
                        updateArray.Analyze(scope, diagnostic);
                        return updateArray;
                    }
                }
            }

            if (!Equals(Left.Type, Right.Type))
            {
                if (!Right.Type.Value.IsAssignable(Left.Type.Value))
                {
                    throw SemanticExceptionFactory.TypeMismatch(Right.Token, Left.Type.Value.Name, Right.Type.Value.Name);
                }
            }

            return this;
        }
    }
}