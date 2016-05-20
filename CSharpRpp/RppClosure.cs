using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public const string TempClosureTypeName = "<>closure";

        public ResolvableType Type { get; private set; }

        public readonly IEnumerable<IRppParam> Bindings;

        public IRppExpr Expr { get; private set; }
        public ResolvableType ReturnType { get; private set; }

        public RppClosureContext Context { get; }

        public RType ClosureType { get; private set; }
        public RppGenericParameter[] OriginalGenericArguments { get; private set; }

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


        /// Closure captures all available generics and passes them as class generics to the closure itself.
        /// class Foo[A, B] {
        ///    def func[X, Y] = {
        ///       val f = () => ...
        ///    }
        /// 
        /// So [!A,!B,!!X,!!Y] are need to be available for closure because it can declare variables. They are coming from scope.AvailableGenericArguments
        /// However when we construct closure base type, we need to map [!A, !B, !X, !Y] back to original parameters and this is done inside CreateClosureBaseType
        public override IRppNode Analyze(SymbolTable scope, Diagnostic diagnostic)
        {
            OriginalGenericArguments = scope.AvailableGenericArguments.ToArray();
            ClosureType = CreateClosureType(scope);

            SymbolTable closureTypeScope = new SymbolTable(scope, ClosureType, null); // Makes generic types visible
            SymbolTable closureScope = new SymbolTable(closureTypeScope, Context);

            NodeUtils.Analyze(closureScope, Bindings, diagnostic);

            Bindings.ForEach(b => closureScope.AddLocalVar(b.Name, b.Type.Value, b));
            Expr = NodeUtils.AnalyzeNode(closureScope, Expr, diagnostic);

            ProcessCapturedVariables(Context.CapturedVariableReferences);

            ReturnType = Expr.Type;

            Type = CreateClosureBaseType(scope, OriginalGenericArguments.Select(arg => arg.Type).ToArray());

            return this;
        }


        private static RType CreateClosureType(SymbolTable scope)
        {
            RType closureType = new RType(TempClosureTypeName, RTypeAttributes.Sealed | RTypeAttributes.Private | RTypeAttributes.Abstract, null,
                scope.GetEnclosingType());

            var genericArguments = scope.AvailableGenericArguments.ToArray();

            if (genericArguments.NonEmpty())
            {
                string[] genericsNames = genericArguments.Select(ga => ga.Name).ToArray();
                RppGenericParameter[] closureGenericParams = closureType.DefineGenericParameters(genericsNames);
                for (int i = 0; i < closureGenericParams.Length; i++)
                {
                    RppGenericParameter closureGenericParam = closureGenericParams[i];
                    RppGenericParameter genericParam = genericArguments[i];

                    closureGenericParam.Type = new RType(genericParam.Type.Name) {IsGenericParameter = true};
                    closureGenericParam.Constraint = genericParam.Constraint;
                    closureGenericParam.Variance = genericParam.Variance;
                }
            }

            return closureType;
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

        private ResolvableType CreateClosureBaseType([NotNull] SymbolTable scope, RType[] originalGenericTypes)
        {
            RType returnType = ReturnType.Value;

            var functionType = LookupBaseType(scope, returnType, Bindings.Count());

            List<RType> bindingTypes = Bindings.Select(b => b.Type.Value).ToList();
            if (!IsAction(returnType))
            {
                bindingTypes.Add(returnType);
            }

            Func<RType, RType> inflate = type => type.IsGenericParameter ? originalGenericTypes[type.GenericParameterPosition] : type;
            var originalBindingTypes = bindingTypes.Select(t => inflate(t)).ToArray();

            var specializedFunctionType = bindingTypes.Count == 0 ? functionType : functionType.MakeGenericType(originalBindingTypes);
            return new ResolvableType(specializedFunctionType);
        }

        [NotNull]
        private static RType LookupBaseType([NotNull] SymbolTable scope, [NotNull] RType returnType, int bindingsCount)
        {
            var typeName = IsAction(returnType) ? "Action" : "Function";
            var functionTypeName = typeName + bindingsCount;
            TypeSymbol functionTypeSymbol = scope.LookupType(functionTypeName);
            Debug.Assert(functionTypeSymbol != null, "base time should come from runtime");
            return functionTypeSymbol.Type;
        }

        private static bool IsAction([NotNull] RType returnType)
        {
            return returnType.Name == "Unit";
        }

        public override string ToString()
        {
            string bindings = string.Join(", ", Bindings.Select(b => $"{b.Name}: {b.Type}"));
            return $"{bindings} => {Expr}: {Expr.Type}";
        }
    }
}