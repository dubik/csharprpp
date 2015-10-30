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
class Foo(val id: Int)
{
}

object Foo
{
    def apply(id: Int) : Foo = new Foo(id)
}

object Bar
{
    def create() : Int = {
        val foo : Foo = Foo(10)
        foo.id
    }
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
            Symbols.SymbolTable runtimeScope = new Symbols.SymbolTable();
            WireRuntime(runtime.Classes, runtimeScope);
            RppProgram program = Parse(code);
            program.Name = "Sample";
            Symbols.SymbolTable scope = new Symbols.SymbolTable(runtimeScope);
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

        private static void WireRuntime(IEnumerable<RppClass> classes, Symbols.SymbolTable scope)
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
//                    scope.Add(runtimeClass);
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

            scope.AddType(new RType("Exception", typeof(Exception)));
            scope.AddType(new RType("Function0", typeof(Function0<>)));
            scope.AddType(new RType("Function1", typeof(Function1<,>)));
            scope.AddType(new RType("Function2", typeof(Function2<,,>)));
            scope.AddType(new RType("Function3", typeof(Function3<,,,>)));
            scope.AddType(new RType("Function4", typeof(Function4<,,,,>)));
            scope.AddType(new RType("Function5", typeof(Function5<,,,,,>)));
        }

        private static void AddFunctionsToScope(IEnumerable<IRppFunc> funcs, Symbols.SymbolTable scope)
        {
//            funcs.ForEach(scope.Add);
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