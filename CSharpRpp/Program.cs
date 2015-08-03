using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Antlr.Runtime;
using CSharpRpp.Codegen;
using CSharpRpp.Native;
using CSharpRpp.Semantics;
using RppRuntime;

[assembly: CLSCompliant(true)]

namespace CSharpRpp
{
    public class Program
    {
        public static void func(object k)
        {
        }

        public static void func(int k)
        {
        }

        private static void Main()
        {
            const string runtimeCode = @"
object Runtime
{
    def println(line: String) : Unit = { }
    def printFormat(format: String, args: Any*) : Unit = { }
} 
";
            const string code = @"
class Foo(var k: Int)
{
    def this() = this(27)
}

class Bar extends Foo

object Main
{
    def get() : Int = {
        val inst : Foo = new Bar
        inst.k
    }
}
";
            RppProgram runtime = Parse(runtimeCode);
            RppScope runtimeScope = new RppScope(null);
            WireRuntime(runtime.Classes, runtimeScope);
            RppProgram program = Parse(code);
            program.Name = "Sample";
            RppScope scope = new RppScope(runtimeScope);

            CodeGenerator generator = new CodeGenerator(program);
            try
            {
                program.PreAnalyze(scope);
                generator.PreGenerate();
                program.Analyze(scope);

                SemanticAnalyzer semantic = new SemanticAnalyzer();
                program.Accept(semantic);
            }
            catch (TypeMismatchException e)
            {
                var lines = GetLines(code);
                var line = lines[e.Token.Line - 1];
                Console.WriteLine("<buffer>:{0} error: type mismatch", e.Token.Line);
                Console.WriteLine(" found   : {0}", e.Found);
                Console.WriteLine(" required: {0}", e.Required);
                Console.WriteLine(line);
                Console.WriteLine("{0}^", Ident(e.Token.CharPositionInLine));
                Environment.Exit(-1);
            }

            generator.Generate();
            generator.Save();
        }

        private static void WireRuntime(IEnumerable<RppClass> classes, RppScope scope)
        {
            Assembly runtimeAssembly = GetRuntimeAssembly();
            Type[] types = runtimeAssembly.GetTypes();
            var typesMap = types.ToDictionary(t => t.Name);
            foreach (RppClass clazz in classes)
            {
                Type matchingType;
                if (typesMap.TryGetValue(clazz.Name, out matchingType))
                {
                    IRppClass runtimeClass = new RppNativeClass(matchingType);
                    scope.Add(runtimeClass);
                    if (runtimeClass.Name == "Runtime")
                    {
                        AddFunctionsToScope(runtimeClass.Functions, scope);
                    }
                }
                else
                {
                    throw new Exception(string.Format("Can't find {0} class from runtime assembly", clazz.Name));
                }
            }

            scope.Add(new RppNativeClass(typeof (Exception)));
        }

        private static void AddFunctionsToScope(IEnumerable<IRppFunc> funcs, RppScope scope)
        {
            funcs.ForEach(scope.Add);
        }

        private static Assembly GetRuntimeAssembly()
        {
            return Assembly.GetAssembly(typeof (Runtime));
        }

        private static RppProgram Parse(string code)
        {
            try
            {
                RppParser parser = CreateParser(code);
                return parser.CompilationUnit();
            }
            catch (UnexpectedTokenException e)
            {
                var lines = GetLines(code);
                var line = lines[e.Actual.Line - 1];
                Console.WriteLine("Systax error at line: {0}, unexpected token '{1}'", e.Actual.Line, e.Actual.Text);
                Console.WriteLine(line);
                Console.WriteLine("{0}^ Found '{1}', but expected '{2}'", Ident(e.Actual.CharPositionInLine), e.Actual.Text, e.Expected);
                Environment.Exit(-1);
            }

            return null;
        }

        private static RppParser CreateParser(string code)
        {
            ANTLRStringStream input = new ANTLRStringStream(code);
            RppLexer lexer = new RppLexer(input);
            CommonTokenStream tokenStream = new CommonTokenStream(lexer);
            RppParser parser = new RppParser(tokenStream);
            return parser;
        }

        private static IList<string> GetLines(string text)
        {
            List<string> lines = new List<string>();
            using (var reader = new StringReader(text))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    lines.Add(line);
                }
            }

            return lines;
        }

        private static string Ident(int ident)
        {
            StringBuilder res = new StringBuilder();
            while (ident-- > 0)
            {
                res.Append(" ");
            }

            return res.ToString();
        }
    }
}