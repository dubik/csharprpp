using System.Collections.Generic;
using System.Linq;
using Antlr.Runtime;
using CSharpRpp;
using CSharpRpp.TypeSystem;
using NUnit.Framework;

namespace CSharpRppTest
{
    [TestFixture]
    public class ParserTest
    {
        internal static RppProgram Parse(string code)
        {
            RppParser parser = CreateParser(code);
            RppProgram program = new RppProgram();
            parser.CompilationUnit(program);
            return program;
        }

        internal static RppParser CreateParser(string code)
        {
            ANTLRStringStream input = new ANTLRStringStream(code);
            RppLexer lexer = new RppLexer(input);
            CommonTokenStream tokenStream = new CommonTokenStream(lexer);
            var k = tokenStream.GetTokens();
            RppParser parser = new RppParser(tokenStream);
            return parser;
        }

        [Test]
        public void EmptyObject()
        {
            SimpleTestObjectParsing("object Main", 1);
            SimpleTestObjectParsing("object Main\n", 1);
            SimpleTestObjectParsing("object Main\n\n", 1);
            SimpleTestObjectParsing("object Main{}", 1);
        }

        [Test]
        public void ParseEmptyClass()
        {
            RppClass rppClass = ParseClass("class String");
            Assert.AreEqual("String", rppClass.Name);
        }

        [Test]
        public void ParseEmptyClassWithOneField()
        {
            RppClass rppClass = ParseClass("class String(val length: Int)");
            Assert.AreEqual(1, rppClass.Fields.Count());
            Assert.AreEqual("length", rppClass.Fields.First().Name);
            Assert.AreEqual("String", rppClass.Name);
        }

        private static RppClass ParseClass(string code)
        {
            RppProgram program = Parse(code);
            Assert.IsNotNull(program);
            Assert.AreEqual(1, program.Classes.Count());
            return program.Classes.First();
        }

        private static void SimpleTestObjectParsing(string code, int expectedClassesCount)
        {
            RppProgram program = Parse(code);
            Assert.IsNotNull(program);
            Assert.AreEqual(expectedClassesCount, program.Classes.Count());
        }

        [Test]
        public void ParseSimpleType()
        {
            TestType("Array", new RTypeName("Array"));
        }

        [Test]
        [Category("Generics")]
        public void ParseGenericType()
        {
            RTypeName expected = new RTypeName("Array");
            expected.AddGenericArgument(new RTypeName("String"));

            TestType("Array[String]", expected);
        }

        [Test]
        [Category("Generics")]
        public void ParseMultiGenericType()
        {
            RTypeName expected = new RTypeName("Array");
            expected.AddGenericArgument(new RTypeName("String"));
            expected.AddGenericArgument(new RTypeName("Int"));

            TestType("Array[String, Int]", expected);
        }

        private static void TestType<T>(string code, T expectedType)
        {
            RTypeName type;
            Assert.IsTrue(CreateParser(code).ParseType(out type));
            Assert.IsInstanceOf<T>(type);
            Assert.AreEqual(expectedType, type);
        }

        [Test]
        public void TestParseClassParam()
        {
            TestFixtureParam("val foo : Int", new RppField(MutabilityFlag.MfVal, "foo", Collections.NoModifiers, new ResolvableType(new RTypeName("Int"))));
            TestFixtureParam("var foo : Int", new RppField(MutabilityFlag.MfVar, "foo", Collections.NoModifiers, new ResolvableType(new RTypeName("Int"))));
            TestFixtureParam("foo : Int", new RppField(MutabilityFlag.MfVal, "foo", Collections.NoModifiers, new ResolvableType(new RTypeName("Int"))));
        }

        private static void TestFixtureParam(string code, RppField expected)
        {
            RppField field;
            Assert.IsTrue(CreateParser(code).ParseClassParam(out field));
            Assert.AreEqual(expected, field);
        }

        [Test]
        public void TestFieldDef()
        {
            TestVarDef("k : Int = 10", new RppField(MutabilityFlag.MfVal, "k", Collections.NoModifiers, new ResolvableType(new RTypeName("Int"))));
        }

        private static void TestVarDef(string code, RppVar expected)
        {
            var var = CreateParser(code).ParsePatDef(MutabilityFlag.MfVal, Collections.NoModifiers);
            Assert.IsNotNull(var);
            Assert.AreEqual(expected, var);
        }

        [Test]
        public void ParamClause()
        {
            Assert.AreEqual(0, CreateParser("").ParseParamClauses().Count());
            Assert.AreEqual(0, CreateParser("()").ParseParamClauses().Count());
            Assert.AreEqual(1, CreateParser("(k : Int)").ParseParamClauses().Count());
            Assert.AreEqual(2, CreateParser("(k : Int, j : String)").ParseParamClauses().Count());
        }
    }
}