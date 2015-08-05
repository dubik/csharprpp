using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace CSharpRpp
{
    public class RppClosure : RppNode, IRppExpr
    {
        public RppType Type { get; private set; }

        public readonly IEnumerable<IRppParam> Bindings;
        public IRppExpr Expr { get; private set; }
        public RppType ReturnType { get; private set; }

        public RppClosure(IEnumerable<IRppParam> bindings, IRppExpr body)
        {
            Bindings = bindings;
            Expr = body;
        }

        public override IRppNode Analyze(RppScope scope)
        {
            RppScope closureScope = new RppScope(scope);
            Bindings.ForEach(closureScope.Add);
            NodeUtils.Analyze(closureScope, Bindings);
            Expr = NodeUtils.AnalyzeNode(closureScope, Expr);

            ReturnType = Expr.Type;

            Type = CreateClosureType(scope);
            return this;
        }

        public override void Accept(IRppNodeVisitor visitor)
        {
            visitor.Visit(this);
        }

        private ResolvedType CreateClosureType(RppScope scope)
        {
            RppGenericType closureType = new RppGenericType("Function" + Bindings.Count());
            Bindings.Select(b => b.Type).ForEach(closureType.AddParam);
            closureType.AddParam(Expr.Type);
            var resolvedType = closureType.Resolve(scope);
            Debug.Assert(resolvedType != null, "Can't resolve closure type");
            return resolvedType;
        }
    }
}