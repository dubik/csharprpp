namespace CSharpRpp.Codegen
{
    public class InheritanceConfigurator2 : RppNodeVisitor
    {
        public override void VisitEnter(RppClass node)
        {
            if (node.BaseConstructorCall.BaseClassType2.Value.Name != "Object")
            {
                node.Type.BaseType = node.BaseConstructorCall.BaseClassType2.Value;
            }
        }
    }
}