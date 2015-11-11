using System.Linq;
using Antlr.Runtime;
using CSharpRpp;
using CSharpRpp.TypeSystem;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CSharpRppTest
{
    [TestClass]
    public class ParserTest
    {
        internal static RppProgram Parse(string code)
        {
            RppParser parser = CreateParser(code);
            return parser.CompilationUnit();
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

        [TestMethod]
        public void EmptyObject()
        {
            SimpleTestObjectParsing("object Main", 1);
            SimpleTestObjectParsing("object Main\n", 1);
            SimpleTestObjectParsing("object Main\n\n", 1);
            SimpleTestObjectParsing("object Main{}", 1);
        }

        [TestMethod]
        public void ParseEmptyClass()
        {
            RppClass rppClass = ParseClass("class String");
            Assert.AreEqual("String", rppClass.Name);
        }

        [TestMethod]
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

        [TestMethod]
        public void ParseSimpleType()
        {
            TestType("Array", new RTypeName("Array"));
        }

        [TestMethod]
        [TestCategory("Generics")]
        public void ParseGenericType()
        {
            RTypeName expected = new RTypeName("Array");
            expected.AddGenericArgument(new RTypeName("String"));

            TestType("Array[String]", expected);
        }

        [TestMethod]
        [TestCategory("Generics")]
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
            Assert.IsInstanceOfType(type, typeof (T));
            Assert.AreEqual(expectedType, type);
        }

        [TestMethod]
        public void TestParseClassParam()
        {
            TestClassParam("val foo : Int", new RppField(MutabilityFlag.MF_Val, "foo", null, new ResolvableType(new RTypeName("Int"))));
            TestClassParam("var foo : Int", new RppField(MutabilityFlag.MF_Var, "foo", null, new ResolvableType(new RTypeName("Int"))));
            TestClassParam("foo : Int", new RppField(MutabilityFlag.MF_Val, "foo", null, new ResolvableType(new RTypeName("Int"))));
        }

        private static void TestClassParam(string code, RppField expected)
        {
            RppField field;
            Assert.IsTrue(CreateParser(code).ParseClassParam(out field));
            Assert.AreEqual(expected, field);
        }

        [TestMethod]
        public void TestVarDef()
        {
            TestVarDef("k : Int = 10", new RppVar(MutabilityFlag.MF_Val, "k", new ResolvableType(new RTypeName("Int")), RppEmptyExpr.Instance));
        }

        private static void TestVarDef(string code, RppVar expected)
        {
            var var = CreateParser(code).ParsePatDef(MutabilityFlag.MF_Val);
            Assert.IsNotNull(var);
            Assert.AreEqual(expected, var);
        }

        [TestMethod]
        public void ParamClause()
        {
            Assert.AreEqual(0, CreateParser("").ParseParamClauses().Count());
            Assert.AreEqual(0, CreateParser("()").ParseParamClauses().Count());
            Assert.AreEqual(1, CreateParser("(k : Int)").ParseParamClauses().Count());
            Assert.AreEqual(2, CreateParser("(k : Int, j : String)").ParseParamClauses().Count());
        }
    }
}