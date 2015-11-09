using System.Collections.Generic;
using System.Linq;
using CSharpRpp.Symbols;
using CSharpRpp.TypeSystem;

namespace CSharpRpp
{
    public class RppClosure : RppNode, IRppExpr
    {
        public ResolvableType Type2 { get; private set; }

        public readonly IEnumerable<IRppParam> Bindings;
        public IRppExpr Expr { get; private set; }
        public ResolvableType ReturnType2 { get; private set; }

        public RppClosure(IEnumerable<IRppParam> bindings, IRppExpr body)
        {
            Bindings = bindings;
            Expr = body;
            Type2 = ResolvableType.UndefinedTy;
        }

        public override IRppNode Analyze(SymbolTable scope)
        {
            SymbolTable closureScope = new SymbolTable(scope);
            NodeUtils.Analyze(closureScope, Bindings);

            Bindings.ForEach(b => closureScope.AddLocalVar(b.Name, b.Type2.Value, b));
            Expr = NodeUtils.AnalyzeNode(closureScope, Expr);

            ReturnType2 = Expr.Type2;

            Type2 = CreateClosureType(scope);
            return this;
        }

        public override void Accept(IRppNodeVisitor visitor)
        {
            visitor.Visit(this);
        }

        private ResolvableType CreateClosureType(SymbolTable scope)
        {
            var functionTypeName = "Function" + Bindings.Count();
            RType functionType = scope.LookupType(functionTypeName).Type;

            List<RType> bindingTypes = Bindings.Select(b => b.Type2.Value).ToList();
            bindingTypes.Add(Expr.Type2.Value);
            RType specializedFunctionType = functionType.MakeGenericType(bindingTypes.ToArray());
            return new ResolvableType(specializedFunctionType);
        }
    }
}