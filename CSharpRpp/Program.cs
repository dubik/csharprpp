using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Antlr.Runtime;
using Antlr.Runtime.Tree;
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
            const string code = @"
class Array(k: Int)
{
   def apply(index: Int, value: Int) : Unit = 10 + 3

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
        println(""Hello World!!! Moika mo''ika!!!"")
        calculate(10, 5)
    }
}
";
            RppProgram runtime = Compile(runtimeCode);
            RppScope runtimeScope = new RppScope(null);
            WireRuntime(runtime.Classes, runtimeScope);
            RppProgram program = Compile(code);
            program.Name = "Sample";
            RppScope scope = new RppScope(runtimeScope);
            CodegenContext codegenContext = new CodegenContext();
            program.PreAnalyze(scope);
            program.CodegenType(scope);
            program.CodegenMethodStubs(scope, codegenContext);
            program.Analyze(scope);
            program.Codegen(codegenContext);
            program.Save();

            /*
             * Array[String]
             * Array[Array[String]]
             * Pair[String, Int]
             * Pair[String, Array[String]]
            RppProgram  p = new RppProgram();
            RppClass c = new RppClass("Array");
            RppFunc f = new RppFunc();
            c.AddFunc();
            p.Add();
             */
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

        private static RppProgram Compile(string code)
        {
            ANTLRStringStream input = new ANTLRStringStream(code);
            JRppLexer lexer = new JRppLexer(input);
            CommonTokenStream tokens = new CommonTokenStream(lexer);
            JRppParser parser = new JRppParser(tokens);
            var result = parser.compilationUnit();
            var s = ((CommonTree) result.Tree).ToStringTree();
            CommonTreeNodeStream treeNodeStream = new CommonTreeNodeStream(result.Tree);
            JRppTreeGrammar walker = new JRppTreeGrammar(treeNodeStream);
            return walker.walk();
        }
    }
}