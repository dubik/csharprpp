using System;
using System.Collections.Generic;
using System.Linq;
using CSharpRpp.Exceptions;
using CSharpRpp.Reporting;
using CSharpRpp.Symbols;
using CSharpRpp.TypeSystem;
using JetBrains.Annotations;
using static CSharpRpp.ListExtensions;

namespace CSharpRpp
{
    public class RppAssignOp : RppBinOp
    {
        internal static readonly HashSet<string> Ops = new HashSet<string> {"=", "+=", "-=", "*=", "/=", "%=", "<<=", ">>=", "&=", "^=", "|="};

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
                        return updateArray.Analyze(scope, diagnostic);
                    }
                }
                else if (selector.Path is RppFieldSelector) // Rewrite assignment to field as a call to setter of the field
                {
                    RppFieldSelector fieldSelector = (RppFieldSelector) selector.Path;
                    RppSelector callPropertySetter = new RppSelector(selector.Target, new RppFuncCall(fieldSelector.Field.SetterName,
                        List(Right)));
                    return callPropertySetter.Analyze(scope, diagnostic);
                }
            }

            if (!Equals(Left.Type, Right.Type))
            {
                if (!Left.Type.Value.IsAssignable(Right.Type.Value))
                {
                    throw SemanticExceptionFactory.TypeMismatch(Right.Token, Left.Type.Value.Name, Right.Type.Value.Name);
                }
            }

            return this;
        }

        [NotNull]
        public new static RppBinOp Create(string op, [NotNull] IRppExpr left, [NotNull] IRppExpr right)
        {
            if (op == "=")
            {
                return new RppAssignOp(left, right);
            }

            string operatorStr = ExtractOperator(op);
            return Create("=", left, RppBinOp.Create(operatorStr, left, right));
        }

        [NotNull]
        private static string ExtractOperator([NotNull] string assignmentOp)
        {
            if (assignmentOp.Length < 1 && assignmentOp.Length > 3)
                throw new ArgumentException();

            if (assignmentOp.Length == 2)
            {
                return assignmentOp.Substring(0, 1);
            }

            return assignmentOp.Substring(0, 2);
        }
    }
}