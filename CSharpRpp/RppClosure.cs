using System.Collections.Generic;
using System.Linq;
using CSharpRpp.Reporting;
using CSharpRpp.Symbols;
using CSharpRpp.TypeSystem;
using JetBrains.Annotations;

namespace CSharpRpp
{
    public class RppClosure : RppNode, IRppExpr
    {
        public ResolvableType Type { get; private set; }

        public readonly IEnumerable<IRppParam> Bindings;
        public IRppExpr Expr { get; private set; }
        public ResolvableType ReturnType { get; private set; }

        public RppClosure(IEnumerable<IRppParam> bindings, IRppExpr body)
        {
            Bindings = bindings;
            Expr = body;
            Type = ResolvableType.UndefinedTy;
        }

        public override IRppNode Analyze(SymbolTable scope, Diagnostic diagnostic)
        {
            SymbolTable closureScope = new SymbolTable(scope);
            NodeUtils.Analyze(closureScope, Bindings, diagnostic);

            Bindings.ForEach(b => closureScope.AddLocalVar(b.Name, b.Type.Value, b));
            Expr = NodeUtils.AnalyzeNode(closureScope, Expr, diagnostic);

            ReturnType = Expr.Type;

            Type = CreateClosureType(scope);
            return this;
        }

        public override void Accept(IRppNodeVisitor visitor)
        {
            visitor.Visit(this);
        }

        private ResolvableType CreateClosureType([NotNull] SymbolTable scope)
        {
            RType returnType = ReturnType.Value;
            var typeName = IsAction(returnType) ? "Action" : "Function";
            var functionTypeName = typeName + Bindings.Count();
            RType functionType = scope.LookupType(functionTypeName).Type;

            List<RType> bindingTypes = Bindings.Select(b => b.Type.Value).ToList();
            if (!IsAction(returnType))
            {
                bindingTypes.Add(Expr.Type.Value);
            }
            RType specializedFunctionType = functionType.MakeGenericType(bindingTypes.ToArray());
            return new ResolvableType(specializedFunctionType);
        }

        private static bool IsAction(RType returnType)
        {
            return returnType.Name == "Unit";
        }
    }
}