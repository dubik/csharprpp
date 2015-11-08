using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Antlr.Runtime;
using CSharpRpp.Codegen;
using CSharpRpp.Semantics;
using CSharpRpp.Symbols;
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
abstract class Function0[TResult]
{
    def apply : TResult
}

abstract class Function1[T1, TResult]
{
    def apply(arg1: T1) : TResult
}

abstract class Function2[T1, T2, TResult]
{
    def apply(arg1: T1, arg2: T2) : TResult
}

object Bar
{
    def main() : Int = {
        var func: (Int, Int) => Boolean = (x: Int, y: Int) => x < y
        10
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
            SymbolTable runtimeScope = new SymbolTable();
            WireRuntime(runtime.Classes, runtimeScope);
            RppProgram program = Parse(code);
            program.Name = "Sample";
            SymbolTable scope = new SymbolTable(runtimeScope);
            RppTypeSystem.PopulateBuiltinTypes(scope);

            CodeGenerator generator = new CodeGenerator(program);
            try
            {
                Type2Creator typeCreator = new Type2Creator();
                program.Accept(typeCreator);

                program.PreAnalyze(scope);

                InheritanceConfigurator2 configurator = new InheritanceConfigurator2();
                program.Accept(configurator);

                CreateRType createRType = new CreateRType();
                program.Accept(createRType);

                program.Analyze(scope);

                SemanticAnalyzer semantic = new SemanticAnalyzer();
                program.Accept(semantic);

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
            /*
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
            */
            scope.AddType(new RType("Exception", typeof (Exception)));

            //Enumerable.Range(0, 5).Select(CreateFunctionInterfaceType).ForEach(scope.AddType);

            //scope.AddType(new RType("Function0", typeof (Function0<>)));
            //scope.AddType(new RType("Function1", typeof (Function1<,>)));
            //scope.AddType(new RType("Function2", typeof (Function2<,,>)));
            //scope.AddType(new RType("Function3", typeof (Function3<,,,>)));
            //scope.AddType(new RType("Function4", typeof (Function4<,,,,>)));
            //scope.AddType(new RType("Function5", typeof (Function5<,,,,,>)));
        }

        private static RType CreateFunctionInterfaceType(int paramsCount)
        {
            RType functionType = new RType($"Function{paramsCount}", RTypeAttributes.Interface | RTypeAttributes.Public);
            List<string> generics = new List<string>(new[] {"TResult"});
            var paramsList = Enumerable.Range(0, paramsCount).Select(r => $"T{r + 1}");
            generics.AddRange(paramsList);
            RppGenericParameter[] genericParameters = functionType.DefineGenericParameters(generics.ToArray());

            var applyParameters =
                genericParameters.Take(genericParameters.Length - 1).Select(gp => new RppParameterInfo($"arg{gp.Position}", gp.Type)).ToArray();
            functionType.DefineMethod("apply", RMethodAttributes.Abstract | RMethodAttributes.Public, genericParameters.Last().Type, applyParameters);

            return functionType;
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