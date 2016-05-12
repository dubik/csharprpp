using CSharpRpp.Exceptions;
using CSharpRpp.Reporting;

namespace CSharpRpp.Semantics
{
    public class SemanticAnalyzerStage1 : RppNodeVisitor
    {
        private readonly Diagnostic _diagnostic;

        public SemanticAnalyzerStage1(Diagnostic diagnostic)
        {
            _diagnostic = diagnostic;
        }

        public override void Visit(RppField node)
        {
            if (!node.IsClassParam)
                ValidateField(node);
        }

        private static void ValidateField(RppField field)
        {
            if (field.InitExpr == null || field.InitExpr is RppEmptyExpr)
            {
                throw SemanticExceptionFactory.MissingInitializer(field.Token);
            }
        }
    }
}