using System.Diagnostics;
using System.IO;
using System.Linq;
using CSharpRpp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static CSharpRpp.ListExtensions;

namespace CSharpRppTest
{
    [TestClass]
    public class CompilerTest
    {
        [TestInitialize]
        public void Setup()
        {
            File.Delete("out.dll");
            File.Delete("out.exe");
        }

        [TestCategory("ILVerifier"), TestMethod]
        public void TestThatCompilerAndVerifierCanBeRun()
        {
            string output;
            int csharProcess = SpawnCompiler(new string[0], out output);
            Assert.AreEqual(1, csharProcess);
            csharProcess = SpawnPreverifier(new string[0], out output);
            Assert.AreEqual(0, csharProcess, output);
        }

        [TestCategory("ILVerifier"), TestMethod]
        public void TestClassFoo()
        {
            PeverifyTest("testcase1.rpp");
        }

        [TestCategory("ILVerifier"), TestMethod]
        public void TestMixingGenericArgumentsOfMethodAndClass()
        {
            PeverifyTest("testcase2.rpp");
        }

        [TestCategory("ILVerifier"), TestMethod]
        public void MethodWhichThrowExceptionShouldntUseReturn()
        {
            PeverifyTest("testcase3.rpp");
        }

        [TestCategory("ILVerifier"), TestMethod]
        public void ExpressionsWhichResultsAreNotUsedShouldBePopped()
        {
            PeverifyTest("testcase4.rpp");
        }

        [TestCategory("ILVerifier"), TestMethod]
        public void UsingUnitAsClosureReturn()
        {
            PeverifyTest("testcase5.rpp");
        }

        [TestCategory("ILVerifier"), TestMethod]
        public void TestingTypeComplainsForRefsInClosures()
        {
            PeverifyTest("testcase6.rpp");
        }

        [TestCategory("Runtime"), TestMethod]
        public void PrintHelloToConsole()
        {
            CompileExe("runtimetest1.rpp");
            string output = ExecuteOutExe();
            Assert.AreEqual("Hello\r\n", output);
        }

        [TestCategory("Runtime"), TestMethod]
        public void OverrideToString()
        {
            CompileExe("runtimetest2.rpp");
            string output = ExecuteOutExe();
            Assert.AreEqual("Foo\r\nBar\r\n", output);
        }

        #region Spawn

        private static string ExecuteOutExe()
        {
            string output;
            int returnCode = SpawnProcess("out.exe", new string[] {}, out output);
            Assert.AreEqual(0, returnCode, output);
            return output;
        }

        private static void CompileLibrary(string testcase)
        {
            CompileTestCase(testcase, "--library", "--out", "out.dll");
        }

        private static void CompileExe(string testcase)
        {
            CompileTestCase(testcase, "--out", "out.exe");
        }

        private static void CompileTestCase(string testcase, params string[] args)
        {
            string testcaseFullName = GetTestCaseFullPath(testcase);
            string consoleOutput;
            int compilerExitCode = SpawnCompiler(List(testcaseFullName).Concat(args).ToArray(), out consoleOutput);
            Assert.AreEqual(0, compilerExitCode, consoleOutput);
        }

        private static string GetTestCaseFullPath(string testcase)
        {
            string testcaseFullName = @"tests\" + testcase;
            Assert.IsTrue(File.Exists(testcaseFullName), "Test case file can't be found (forgot to set 'Copy To Output Directory'?)");
            return testcaseFullName;
        }

        private static void PeverifyTest(string testcase)
        {
            CompileLibrary(testcase);
            string output;
            int peverifyExitCode = SpawnPreverifier(new[] {@"out.dll"}, out output);
            Assert.AreEqual(0, peverifyExitCode, output);
        }

        private static int SpawnCompiler(string[] arguments, out string consoleOutput)
        {
            return SpawnProcess("CSharpRpp.exe", arguments, out consoleOutput);
        }

        private static int SpawnPreverifier(string[] arguments, out string output)
        {
            return SpawnProcess(@"tools\peverify.exe", arguments, out output);
        }

        private static int SpawnProcess(string executable, string[] arguments, out string output)
        {
            ProcessStartInfo info = new ProcessStartInfo
            {
                FileName = executable,
                Arguments = string.Join(" ", arguments),
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };

            Process process = Process.Start(info);
            Assert.IsNotNull(process);

            string stdOut = process.StandardOutput.ReadToEnd();
            string stdErr = process.StandardError.ReadToEnd();
            process.WaitForExit();
            output = stdOut;
            if (stdErr.NonEmpty())
            {
                output = stdOut + "\n" + stdErr;
            }
            return process.ExitCode;
        }

        #endregion
    }
}