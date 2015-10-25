using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Antlr.Runtime;
using CSharpRpp.Codegen;
using CSharpRpp.Native;
using CSharpRpp.TypeSystem;
using RppRuntime;

[assembly: CLSCompliant(true)]

namespace CSharpRpp
{
    public class Program
    {
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
class Foo(var k : Int)
{
}

class Bar(var k: Int) extends Foo(k)
{
}
";

            /*
            Create Types (go through classes)
            Create scopes and add types to them
            Create function and resolve parameters
            Create primary constructors and resolve parameters
            Resolve fields
            Set parent class relationship
            Create 
            Analyze function bodies
            Generate code
            */

            RppProgram runtime = Parse(runtimeCode);
            RppScope runtimeScope = new RppScope(null);
            WireRuntime(runtime.Classes, runtimeScope);
            RppProgram program = Parse(code);
            program.Name = "Sample";
            RppScope scope = new RppScope(runtimeScope);
            RppTypeSystem.PopulateBuiltinTypes(scope);

            CodeGenerator generator = new CodeGenerator(program);
            try
            {
                Type2Creator typeCreator = new Type2Creator();
                program.Accept(typeCreator);

                program.PreAnalyze(scope);

                ResolveParamTypes resolver = new ResolveParamTypes();
                program.Accept(resolver);

                InheritanceConfigurator2 configurator = new InheritanceConfigurator2();
                program.Accept(configurator);

                CreateRType createRType = new CreateRType();
                program.Accept(createRType);

                program.Analyze(scope);

                InitializeNativeTypes initializeNativeTypes = new InitializeNativeTypes(generator.Module);
                program.Accept(initializeNativeTypes);
                CreateNativeTypes createNativeTypes = new CreateNativeTypes();
                program.Accept(createNativeTypes);

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
                    throw new Exception($"Can't find {clazz.Name} class from runtime assembly");
                }
            }

            scope.Add(new RppNativeClass(typeof (Exception)));
            scope.Add(new RppNativeClass(typeof (Function0<>)));
            scope.Add(new RppNativeClass(typeof (Function1<,>)));
            scope.Add(new RppNativeClass(typeof (Function2<,,>)));
            scope.Add(new RppNativeClass(typeof (Function3<,,,>)));
            scope.Add(new RppNativeClass(typeof (Function4<,,,,>)));
            scope.Add(new RppNativeClass(typeof (Function5<,,,,,>)));
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