using System.Collections.Generic;
using System.Linq;
using CSharpRpp.Reporting;
using CSharpRpp.Symbols;
using CSharpRpp.TypeSystem;
using JetBrains.Annotations;

namespace CSharpRpp
{
    public class RppClosureContext
    {
        public bool IsCaptureOuter { get; set; }

        public IEnumerable<RppId> CapturedVariableReferences => _capturedVariableReferences;

        private readonly List<RppId> _capturedVariableReferences = new List<RppId>();
        public bool IsCaptureThis { get; private set; }

        public void CaptureVar(RppId rppId)
        {
            _capturedVariableReferences.Add(rppId);
        }

        public void CaptureThis()
        {
            IsCaptureThis = true;
        }
    }

    public class RppClosure : RppNode, IRppExpr
    {
        public ResolvableType Type { get; private set; }

        public readonly IEnumerable<IRppParam> Bindings;

        public IRppExpr Expr { get; private set; }
        public ResolvableType ReturnType { get; private set; }

        public RppClosureContext Context { get; }

        private List<RppVar> _capturedVars;
        private List<RppParam> _capturedParams;

        public IEnumerable<RppVar> CapturedVars => _capturedVars;
        public IEnumerable<RppParam> CapturedParams => _capturedParams;

        public RppClosure(IEnumerable<IRppParam> bindings, IRppExpr body)
        {
            Bindings = bindings;
            Expr = body;
            Type = ResolvableType.UndefinedTy;
            Context = new RppClosureContext();
        }

        public override IRppNode Analyze(SymbolTable scope, Diagnostic diagnostic)
        {
            SymbolTable closureScope = new SymbolTable(scope, Context);

            NodeUtils.Analyze(closureScope, Bindings, diagnostic);

            Bindings.ForEach(b => closureScope.AddLocalVar(b.Name, b.Type.Value, b));
            Expr = NodeUtils.AnalyzeNode(closureScope, Expr, diagnostic);

            ProcessCapturedVariables(Context.CapturedVariableReferences);

            ReturnType = Expr.Type;

            Type = CreateClosureType(scope);
            return this;
        }

        private void ProcessCapturedVariables(IEnumerable<RppId> capturedVariableReferences)
        {
            var references = capturedVariableReferences as IList<RppId> ?? capturedVariableReferences.ToList();
            _capturedVars = references.Where(v => v.IsVar).Select(v => (RppVar) v.Ref).Distinct().ToList();
            _capturedVars.ForEach(v => v.MakeCaptured());

            _capturedParams = references.Where(v => v.IsParam && !((IRppParam) v.Ref).IsClosureBinding).Select(v => (RppParam) v.Ref).Distinct().ToList();
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
            var specializedFunctionType = bindingTypes.Count == 0 ? functionType : functionType.MakeGenericType(bindingTypes.ToArray());
            return new ResolvableType(specializedFunctionType);
        }

        private static bool IsAction(RType returnType)
        {
            return returnType.Name == "Unit";
        }
    }
}