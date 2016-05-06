using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Antlr.Runtime;
using CSharpRpp.Codegen;
using CSharpRpp.Exceptions;
using CSharpRpp.Reporting;
using CSharpRpp.Semantics;
using CSharpRpp.Symbols;
using CSharpRpp.TypeSystem;
using JetBrains.Annotations;

namespace CSharpRpp
{
    public sealed class RppCompiler
    {
        public static void CompileAndSave(RppOptions options, Diagnostic diagnostic)
        {
            string outFileName = GetOutputFileName(options);

            Assembly stdlib = null;
            if (!options.Nostdlib)
            {
                stdlib = FindStdlib();
            }

            CodeGenerator generator = Compile(program => Parse(options.InputFiles, program), diagnostic, stdlib, outFileName);
            if (generator != null)
            {
                ValidateEntryPoint(generator, options, diagnostic);
                if (!diagnostic.Errors.Any())
                {
                    generator.Save(options.Library ? ApplicationType.Library : ApplicationType.Application);
                }
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

        public static Assembly FindStdlib()
        {
            var location = Assembly.GetAssembly(typeof (RppCompiler)).Location;
            string directory = Path.GetDirectoryName(location);
            return Assembly.LoadFile(directory + @"\RppStdlib.dll");
        }

        [CanBeNull]
        public static CodeGenerator Compile(Action<RppProgram> parseFactory, Diagnostic diagnostic, [CanBeNull] Assembly stdlibAssembly,
            string fileName = "<buffer>")
        {
            RppProgram program = new RppProgram();
            SymbolTable runtimeScope = new SymbolTable();

            RppTypeSystem.PopulateBuiltinTypes(runtimeScope);

            WireRuntime(runtimeScope);

            if (stdlibAssembly != null)
            {
                WireAssembly(runtimeScope, stdlibAssembly);
            }

            parseFactory(program);

            CodeGenerator generator = new CodeGenerator(program, fileName);
            try
            {
                Type2Creator typeCreator = new Type2Creator();
                program.Accept(typeCreator);

                program.PreAnalyze(runtimeScope);

                InheritanceConfigurator2 configurator = new InheritanceConfigurator2();
                program.Accept(configurator);

                CreateRType createRType = new CreateRType(diagnostic);
                program.Accept(createRType);

                program.Analyze(runtimeScope, null);

                SemanticAnalyzer semantic = new SemanticAnalyzer(diagnostic);
                program.Accept(semantic);

                InitializeNativeTypes initializeNativeTypes = new InitializeNativeTypes(generator.Module);
                program.Accept(initializeNativeTypes);
                CreateNativeTypes createNativeTypes = new CreateNativeTypes();
                program.Accept(createNativeTypes);
            }
            catch (SemanticException e)
            {
                diagnostic.Error(e.Code, e.Message);
                return null;
            }

            generator.Generate();
            return generator;
        }

        private static void WireRuntime(SymbolTable scope)
        {
            Assembly runtimeAssembly = GetRuntimeAssembly();
            WireAssembly(scope, runtimeAssembly);

            scope.AddType(RppTypeSystem.CreateType("Exception", typeof (Exception)));
        }

        private static void WireAssembly(SymbolTable scope, Assembly assembly)
        {
            Type[] types = assembly.GetTypes();

            foreach (Type type in types)
            {
                string name = type.Name;
                if (type.Name.Contains("`"))
                {
                    name = name.Substring(0, name.IndexOf('`'));
                }

                if (type.GetField("_instance", BindingFlags.Public | BindingFlags.Static) != null && !name.EndsWith("$"))
                {
                    name = name + "$";
                }


                RType rType = RppTypeSystem.CreateType(name, type);
                scope.AddType(rType);
            }
        }

        private static void Parse(IEnumerable<string> fileNames, RppProgram program)
        {
            fileNames.ForEach(fileName => Parse(File.ReadAllText(fileName), program));
        }

        public static void Parse(string code, RppProgram program)
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