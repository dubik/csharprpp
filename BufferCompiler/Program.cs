using System.Diagnostics;
using System.IO;
using System.Reflection;
using CSharpRpp;
using CSharpRpp.Codegen;
using CSharpRpp.Reporting;

namespace BufferCompiler
{
    public class Program
    {
        public static void Main()
        {
            const string code = @"
class Bar
{
    def hasNext() : Boolean = false

    def invoke() : Int = {
        var ret : Int = 13
        while(hasNext())
        {
            ret = 0
        }
        ret
    }

    def invoke1() : Int = {
        var ret : Int = 13
        while(ret > 13)
        {
            ret = 0
        }
        ret
    }

    def invoke2(k : Int) : Int = {
        var ret : Int = 13
        while(ret > 13 && k < 121)
        {
            ret = 0
        }
        ret
    }
}
";


            Diagnostic diagnostic = new Diagnostic();
            CodeGenerator codeGen = RppCompiler.Compile(program => RppCompiler.Parse(code, program), diagnostic, GetStdlibAssembly(), "Sample.dll");
            if (diagnostic.HasError())
            {
                diagnostic.Report();
            }
            else
            {
                Debug.Assert(codeGen != null, "codeGen != null");
                codeGen.Save();
            }
        }

        public static Assembly GetStdlibAssembly()
        {
            var location = Assembly.GetAssembly(typeof (Program)).Location;
            string directory = Path.GetDirectoryName(location);
            return Assembly.LoadFile(directory + @"\RppStdlib.dll");
        }
    }
}