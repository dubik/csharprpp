using System.Collections.Generic;
using System.Linq;
using CSharpRpp;
using CSharpRpp.TypeSystem;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CSharpRppTest
{
    internal class Type2Creator : RppNodeVisitor
    {
        private RType _type;

        public override void VisitEnter(RppClass node)
        {
            RTypeAttributes attrs = GetAttributes(node.Modifiers) | RTypeAttributes.Class;
            _type = new RType(node.Name, attrs);
            node.Type2 = _type;
        }

        private static RTypeAttributes GetAttributes(ICollection<ObjectModifier> modifiers)
        {
            RTypeAttributes attrs = RTypeAttributes.None;
            if (modifiers.Contains(ObjectModifier.OmSealed))
            {
                attrs |= RTypeAttributes.Sealed;
            }
            if (modifiers.Contains(ObjectModifier.OmAbstract))
            {
                attrs |= RTypeAttributes.Abstract;
            }
            if (!modifiers.Contains(ObjectModifier.OmPrivate))
            {
                attrs |= RTypeAttributes.Public;
            }

            return attrs;
        }
    }

    internal class MethodCreator : RppNodeVisitor
    {
        private RType _type;

        public override void VisitEnter(RppClass node)
        {
            _type = node.Type2;
        }

        public override void VisitEnter(RppFunc node)
        {
            RMethodAttributes attrs = GetMethodAttributes(node.Modifiers);
            var funcParams = node.Params.Select(p => new RppParameterInfo(p.Name, p.Type2)).ToArray();
            _type.DefineMethod(node.Name, attrs, node.NewReturnType, funcParams);
        }

        public override void Visit(RppField node)
        {
            _type.DefineField(node.Name, RFieldAttributes.Public, node.Type2);
        }

        private static RMethodAttributes GetMethodAttributes(ICollection<ObjectModifier> modifiers)
        {
            RMethodAttributes attrs = RMethodAttributes.None;
            if (modifiers.Contains(ObjectModifier.OmOverride))
            {
                attrs |= RMethodAttributes.Override;
            }
            if (!modifiers.Contains(ObjectModifier.OmPrivate))
            {
                attrs |= RMethodAttributes.Public;
            }
            if (modifiers.Contains(ObjectModifier.OmAbstract))
            {
                attrs |= RMethodAttributes.Abstract;
            }
            return attrs;
        }
    }

    [TestClass]
    public class TypeSystemTest
    {
        [TestMethod]
        public void PrimitiveTypeEquality()
        {
            RType t = new RType("Int");
            RType t1 = new RType("Int");
            Assert.AreEqual(t, t1);
        }

        [TestMethod]
        public void TestVisitors()
        {
            const string code = @"
class Foo
class Bar extends Foo
";
            RppProgram program = Utils.Parse(code);
            Type2Creator crea = new Type2Creator();
            program.Accept(crea);
            MethodCreator methodCreator = new MethodCreator();
            program.Accept(methodCreator);
        }

        [TestMethod]
        public void TestExtendGenericClass()
        {
            const string code = @"
class Foo[A]

class Bar extends Foo[Int]
";
            RppProgram program = Utils.Parse(code);
            Type2Creator crea = new Type2Creator();
            program.Accept(crea);
            MethodCreator methodCreator = new MethodCreator();
            program.Accept(methodCreator);

            // Analyze
        }
    }
}