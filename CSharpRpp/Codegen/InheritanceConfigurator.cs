using System;
using System.Diagnostics;
using System.Reflection.Emit;

namespace CSharpRpp.Codegen
{
    public class InheritanceConfigurator : RppNodeVisitor
    {
        public override void VisitEnter(RppClass node)
        {
            if (node.BaseConstructorCall.BaseClassType2.Value.Name != "Object")
            {
                TypeBuilder builder = node.Type.NativeType as TypeBuilder;
                Debug.Assert(builder != null, "builder != null");

                //builder.SetParent(node.BaseConstructorCall.BaseClassType.Runtime);
            }
        }
    }

    public class InheritanceConfigurator2 : RppNodeVisitor
    {
        public override void VisitEnter(RppClass node)
        {
            if (node.BaseConstructorCall.BaseClassType2.Value.Name != "Object")
            {
                //TypeBuilder builder = node.RuntimeType as TypeBuilder;
                //Debug.Assert(builder != null, "builder != null");

                //builder.SetParent(node.BaseConstructorCall.BaseClassType.Runtime);

                node.Type.BaseType = node.BaseConstructorCall.BaseClassType2.Value;
            }
        }
    }
}