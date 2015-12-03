using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Antlr.Runtime;
using CSharpRpp.Codegen;
using CSharpRpp.Reporting;
using CSharpRpp.Semantics;
using CSharpRpp.Symbols;
using CSharpRpp.TypeSystem;

namespace CSharpRpp
{
    public sealed class RppCompiler
    {
        public static void CompileAndSave(RppOptions options, Diagnostic diagnostic)
        {
            string outFileName = GetOutputFileName(options);

            CodeGenerator generator = Compile(program => Parse(options.InputFiles, program), outFileName);

            ValidateEntryPoint(generator, options, diagnostic);
            if (!diagnostic.Errors.Any())
            {
                generator.Save(outFileName);
            }
        }

        private static void ValidateEntryPoint(CodeGenerator generator, RppOptions options, Diagnostic diagnostic)
        {
            if (options.Library == false && !generator.HasMain())
            {
                diagnostic.Error(101, "Program doesn't contain a valid entry point");
            }
        }

        private static string GetOutputFileName(RppOptions options)
        {
            return !string.IsNullOrEmpty(options.Out) ? options.Out : GenerateOutputFileName(options);
        }

        private static string GenerateOutputFileName(RppOptions options)
        {
            string ext = options.Library ? ".dll" : ".exe";
            string firstFile = options.InputFiles.First();
            return Path.GetFileNameWithoutExtension(firstFile) + ext;
        }

        public static CodeGenerator Compile(Action<RppProgram> parseFactory, string fileName = "<buffer>")
        {
            RppProgram program = new RppProgram();
            SymbolTable runtimeScope = new SymbolTable();
            WireRuntime(runtimeScope);
            parseFactory(program);

            RppTypeSystem.PopulateBuiltinTypes(runtimeScope);

            CodeGenerator generator = new CodeGenerator(program, fileName);
            try
            {
                Type2Creator typeCreator = new Type2Creator();
                program.Accept(typeCreator);

                program.PreAnalyze(runtimeScope);

                InheritanceConfigurator2 configurator = new InheritanceConfigurator2();
                program.Accept(configurator);

                CreateRType createRType = new CreateRType();
                program.Accept(createRType);

                program.Analyze(runtimeScope);

                SemanticAnalyzer semantic = new SemanticAnalyzer();
                program.Accept(semantic);

                InitializeNativeTypes initializeNativeTypes = new InitializeNativeTypes(generator.Module);
                program.Accept(initializeNativeTypes);
                CreateNativeTypes createNativeTypes = new CreateNativeTypes();
                program.Accept(createNativeTypes);
            }
            catch (TypeMismatchException e)
            {
                const string code = "Get code from token";
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
            return generator;
        }

        private static void WireRuntime(SymbolTable scope)
        {
            Assembly runtimeAssembly = GetRuntimeAssembly();
            Type[] types = runtimeAssembly.GetTypes();

            foreach (Type type in types)
            {
                string name = type.Name;
                if (type.Name.Contains("`"))
                {
                    name = name.Substring(0, name.IndexOf('`'));
                }

                RType rType = RppTypeSystem.CreateType(name, type);
                scope.AddType(rType);
            }

            scope.AddType(RppTypeSystem.CreateType("Exception", typeof (Exception)));
        }

        private static void Parse(IEnumerable<string> fileNames, RppProgram program)
        {
            fileNames.ForEach(fileName => Parse(File.ReadAllText(fileName), program));
        }

        private static void Parse(string code, RppProgram program)
        {
            try
            {
                RppParser parser = CreateParser(code);
                parser.CompilationUnit(program);
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

        private static Assembly GetRuntimeAssembly()
        {
            return Assembly.GetAssembly(typeof (Runtime));
        }
    }
}