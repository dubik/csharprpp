using System.Linq;
using CommandLine;
using CSharpRpp.Reporting;

namespace CSharpRpp
{
    public class Program
    {
        public static int Main1(string[] args)
        {
            var result = CommandLine.Parser.Default.ParseArguments<RppOptions>(args);
            return result.MapResult(RunAndReturnExitCode, _ => 1);
        }

        private static int RunAndReturnExitCode(RppOptions options)
        {
            Diagnostic diagnostic = new Diagnostic();
            RppCompiler.CompileAndSave(options, diagnostic);

            diagnostic.Report();

            if (diagnostic.Errors.Any())
            {
                return 1;
            }

            return 0;
        }
    }
}