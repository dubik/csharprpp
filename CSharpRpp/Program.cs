using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.NetworkInformation;
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

class String(len: Int)
{
}

object Main
{
    def main(args: Array[String]) : Int = {
        println(""Hello World"")
    }
}
";
            RppProgram runtime = Compile(runtimeCode);
            RppScope runtimeScope = new RppScope(null);
            WireRuntime(runtime.Classes, runtimeScope);
            RppProgram program = Compile(code);
            program.Name = "Sample";
            RppScope scope = new RppScope(null);
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
                }
                else
                {
                    throw new Exception(string.Format("Can't find {0} class from runtime assembly", clazz.Name));
                }
            }
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