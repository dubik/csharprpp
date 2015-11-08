using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CSharpRpp.Symbols;
using CSharpRpp.TypeSystem;

namespace CSharpRpp
{
    public class RppClosure : RppNode, IRppExpr
    {
        public RppType Type { get; private set; }
        public ResolvableType Type2 { get; private set; }

        public readonly IEnumerable<IRppParam> Bindings;
        public IRppExpr Expr { get; private set; }
        public RppType ReturnType { get; private set; }
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

            ReturnType = Expr.Type;
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

            /*
            Bindings.Select(b => b.Type2).ForEach(closureType.AddParam);

            RppGenericType closureType = new RppGenericType("Function" + Bindings.Count());
            Bindings.Select(b => b.Type).ForEach(closureType.AddParam);
            closureType.AddParam(Expr.Type);
            var resolvedType = closureType.Resolve(scope);
            Debug.Assert(resolvedType != null, "Can't resolve closure type");
            return resolvedType;
            */
        }
    }
}