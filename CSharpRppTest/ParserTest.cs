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
        public void OneFunObject()
        {
            const string code = @"object Main {
    def main(args: Array[String]) : Unit = {
    }
}";
            RppProgram program = Parse(code);
            Assert.IsNotNull(program);
            Assert.AreEqual(1, program.Classes.First().Functions.Count());
            Assert.AreEqual("main", program.Classes.First().Functions.First().Name);
        }

        [TestMethod]
        public void ParseEmptyClass()
        {
            RppClass expected = new RppClass(ClassKind.Class, Collections.NoModifiers, "String", Collections.NoFields, Collections.NoNodes,
                Collections.NoVariantTypeParams,
                RppBaseConstructorCall.Object);
            Assert.AreEqual(ParseClass("class String"), expected);
        }

        [TestMethod]
        public void ParseEmptyClassWithOneField()
        {
            RppField field = new RppField(MutabilityFlag.MF_Val, "length", null, new RppTypeName("Int"));
            RppClass expected = new RppClass(ClassKind.Class, Collections.NoModifiers, "String", new[] {field}, Collections.NoNodes,
                Collections.NoVariantTypeParams,
                RppBaseConstructorCall.Object);
            Assert.AreEqual(ParseClass("class String(length: Int)"), expected);
        }

        private RppClass ParseClass(string code)
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
            TestType("Array", new RppTypeName("Array"));
        }

        [TestMethod]
        public void ParseGenericType()
        {
            RppGenericType expected = new RppGenericType("Array");
            expected.AddParam(new RppTypeName("String"));

            TestType("Array[String]", expected);
        }

        [TestMethod]
        public void ParseMultiGenericType()
        {
            RppGenericType expected = new RppGenericType("Array");
            expected.AddParam(new RppTypeName("String"));
            expected.AddParam(new RppTypeName("Int"));

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
            TestClassParam("val foo : Int", new RppField(MutabilityFlag.MF_Val, "foo", null, new RppTypeName("Int")));
            TestClassParam("var foo : Int", new RppField(MutabilityFlag.MF_Var, "foo", null, new RppTypeName("Int")));
            TestClassParam("foo : Int", new RppField(MutabilityFlag.MF_Val, "foo", null, new RppTypeName("Int")));
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
            TestVarDef("k : Int = 10", new RppVar(MutabilityFlag.MF_Val, "k", new RppTypeName("Int"), RppEmptyExpr.Instance));
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