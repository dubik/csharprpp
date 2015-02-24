using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Antlr.Runtime;
using CSharpRpp.Codegen;
using CSharpRpp.Native;
using RppRuntime;

[assembly: CLSCompliant(true)]

namespace CSharpRpp
{
    public class Program
    {
        private static void Main(string[] args)
        {
            const string runtimeCode = @"
object Runtime
{
    def println(line: String) : Unit = { }
}
";
            /*
            const string code = @"
object Foo
{
    def calculate : Int = {
        var k : Int = 10
        k
    }
}
";*/
            const string code = @"
class Foo(k : Int)
{
    def calculate(x : Int) : Int = {
        k + x
    }
}

object Bar
{
    def main(argc: Array[String]) : Int = {
        val p : Foo = new Foo(10)
        p.calculate(13)
        val msg : String = ""Hello""
        println(msg)
    }
}
";
            /*
             class Array(k: Int)
            {
               def length: Int = 10
            }

            class Foo
            {
                def calculate : Int = 10

                def mult(x : Int) : Int = x * 2
            }

            class String(len: Int)
            {
            }

            object Main
            {
                def calculate(x : Int, y : Int) : Int = x + y

                def main(args: Array[String]) : Int = {
                    val k : Foo = new Foo
                    k.mult(10)
                    println(""Hello World!!! Moika mo''ika!!!"")
                    calculate(10, 5)
                }
            }
             */

            RppProgram runtime = Parse(runtimeCode);
            RppScope runtimeScope = new RppScope(null);
            WireRuntime(runtime.Classes, runtimeScope);
            RppProgram program = Parse(code);
            program.Name = "Sample";
            RppScope scope = new RppScope(runtimeScope);

            CodeGenerator generator = new CodeGenerator(program);
            program.PreAnalyze(scope);
            generator.PreGenerate();
            program.Analyze(scope);
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
            RppParser parser = CreateParser(code);
            return parser.CompilationUnit();
        }

        private static RppParser CreateParser(string code)
        {
            ANTLRStringStream input = new ANTLRStringStream(code);
            RppLexer lexer = new RppLexer(input);
            CommonTokenStream tokenStream = new CommonTokenStream(lexer);
            var k = tokenStream.GetTokens();
            RppParser parser = new RppParser(tokenStream);
            return parser;
        }
    }
}