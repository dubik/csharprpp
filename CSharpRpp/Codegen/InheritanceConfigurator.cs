using System.Diagnostics;
using System.Reflection.Emit;

namespace CSharpRpp.Codegen
{
    class InheritanceConfigurator : RppNodeVisitor
    {
        public override void VisitEnter(RppClass node)
        {
            if (node.BaseConstructorCall.BaseClassName != "Object")
            {
                TypeBuilder builder = node.RuntimeType as TypeBuilder;
                Debug.Assert(builder != null, "builder != null");
                builder.SetParent(node.BaseConstructorCall.BaseClass.RuntimeType);
            }
        }
    }
}