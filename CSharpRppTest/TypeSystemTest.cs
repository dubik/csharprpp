using System.Linq;
using CSharpRpp.Codegen;
using CSharpRpp.TypeSystem;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CSharpRppTest
{
    [TestClass]
    public class TypeSystemTest
    {
        [TestMethod]
        public void PrimitiveTypeEquality()
        {
            var t = new RType("Int");
            var t1 = new RType("Int");
            Assert.AreEqual(t, t1);
        }

        [TestMethod]
        public void TestVisitors()
        {
            const string code = @"
class Foo
class Bar extends Foo
object Main
";
            var program = Utils.Parse(code);
            var creator = new Type2Creator();
            program.Accept(creator);
            var classes = program.Classes.ToArray();
            var fooType = classes[0].Type2;
            var barType = classes[1].Type2;
            var mainType = classes[2].Type2;

            Assert.IsTrue(fooType.IsClass);
            Assert.IsFalse(fooType.IsAbstract);
            Assert.IsFalse(fooType.IsArray);
            Assert.IsFalse(fooType.IsGenericType);
            Assert.IsFalse(fooType.IsPrimitive);
            Assert.IsFalse(fooType.IsSealed);
        }

        [TestMethod]
        public void TypeDefinitionForClass()
        {
            var t = new RType("Foo", RTypeAttributes.Class);
            Assert.IsTrue(t.IsClass);
            Assert.IsFalse(t.IsAbstract);
            Assert.IsFalse(t.IsArray);
            Assert.IsFalse(t.IsGenericType);
            Assert.IsFalse(t.IsPrimitive);
            Assert.IsFalse(t.IsSealed);
        }

        [TestMethod]
        public void TypeDefinitionForSealedClass()
        {
            var t = new RType("Foo", RTypeAttributes.Class | RTypeAttributes.Sealed);
            Assert.IsTrue(t.IsClass);
            Assert.IsTrue(t.IsSealed);
        }

        [TestMethod]
        public void TypeDefinitionForAbstractClass()
        {
            var t = new RType("Foo", RTypeAttributes.Class | RTypeAttributes.Abstract);
            Assert.IsTrue(t.IsClass);
            Assert.IsTrue(t.IsAbstract);
        }

        [TestMethod]
        public void TestExtendGenericClass()
        {
            const string code = @"
class Foo[A]

class Bar extends Foo[Int]
";
            var program = Utils.Parse(code);
            var crea = new Type2Creator();
            program.Accept(crea);

            // Analyze
        }
    }
}