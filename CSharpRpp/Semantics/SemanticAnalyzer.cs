using CSharpRpp.Exceptions;

namespace CSharpRpp.Semantics
{
    public class SemanticAnalyzer : RppNodeVisitor
    {
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
            if (!node.Class.Modifiers.Contains(ObjectModifier.OmAbstract) && node.IsAbstract && !node.Modifiers.Contains(ObjectModifier.OmAbstract))
            {
                throw new SemanticException(string.Format("Class {0} needs to be abstract, since method {1} is not defined", node.Class.Name, node.ToString()));
            }
        }

        private void CheckForNonDefinedAbstractMethods(RppClass node)
        {
            if (node.Modifiers.Contains(ObjectModifier.OmAbstract))
            {
            }

            // TODO not implemented yet
        }
    }
}