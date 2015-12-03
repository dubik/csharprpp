using System.Collections.Generic;
using CommandLine;
using CommandLine.Text;

namespace CSharpRpp
{
    public class RppOptions
    {
        [Value(0, HelpText = "list of input files", Required = true)]
        public IEnumerable<string> InputFiles { get; set; }

        [Option(HelpText = "Specify output file name")]
        public string Out { get; set; }

        [Option(HelpText = "Specify if class library should be created (by default executable)")]
        public bool Library { get; set; }

        [Usage]
        public static IEnumerable<Example> Examples
        {
            get
            {
                yield return new Example("\nCompile one file", new RppOptions {InputFiles = new[] {"First.rpp"}});
                yield return new Example("\nCompile two files", new RppOptions {InputFiles = new[] {"First.rpp", "Second.rpp"}});
                yield return
                    new Example("\nCompile two file and specify output", new RppOptions {InputFiles = new[] {"First.rpp", "Second.rpp"}, Out = "App.exe"});
            }
        }
    }
}