using CSharpRpp.Exceptions;
using CSharpRpp.Reporting;

namespace CSharpRpp.Semantics
{
    public class SemanticAnalyzer : RppNodeVisitor
    {
        private readonly Diagnostic _diagnostic;

        public SemanticAnalyzer(Diagnostic diagnostic)
        {
            _diagnostic = diagnostic;
        }

        public override void VisitEnter(RppClass node)
        {
            CheckForNonDefinedAbstractMethods(node);
        }

        public override void VisitEnter(RppFunc node)
        {
            TypeShouldBeDeclaredAbstractOrMethodShouldBeImplemented(node);
        }

        private static void TypeShouldBeDeclaredAbstractOrMethodShouldBeImplemented(RppFunc node)
        {
            /*
            if (!node.Class.Modifiers.Contains(ObjectModifier.OmAbstract) && node.IsAbstract && !node.Modifiers.Contains(ObjectModifier.OmAbstract))
            {
                throw new SemanticException($"DeclaringType {node.Class.Name} needs to be abstract, since method {node.ToString()} is not defined");
            }
            */
        }

        private void CheckForNonDefinedAbstractMethods(RppClass node)
        {
            /*
            if (node.Modifiers.Contains(ObjectModifier.OmAbstract))
            {
            }
            */
            // TODO not implemented yet
        }
    }
}