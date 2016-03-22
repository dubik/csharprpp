using System;
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
abstract class XIterator[+A] {
  def hasNext: Boolean

  def next(): A

  def copy(): XIterator[A]

  protected def foreach(f: A => Unit): Unit =
    while (hasNext())
      f(next())

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