using System.Diagnostics;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
            int csharProcess = SpawnCompiler(new string[0]);
            Assert.AreEqual(1, csharProcess);
            string output;
            csharProcess = SpawnPreverifier(new string[0], out output);
            Assert.AreEqual(0, csharProcess, output);
        }

        [TestCategory("ILVerifier"), TestMethod]
        public void TestClassFoo()
        {
            PeverifyTest("testcase1.rpp");
        }

        #region Spawn

        private static void PeverifyTest(string testcase)
        {
            int compilerExitCode = SpawnCompiler(new[] {@"tests\" + testcase, "--library","--out", "out.dll"});
            Assert.AreEqual(0, compilerExitCode);
            string output;
            int peverifyExitCode = SpawnPreverifier(new[] {@"out.dll"}, out output);
            Assert.AreEqual(0, peverifyExitCode, output);
        }

        private static int SpawnCompiler(string[] arguments)
        {
            string output;
            return SpawnProcess("CSharpRpp.exe", arguments, out output);
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
                UseShellExecute = false,
            };

            Process process = Process.Start(info);
            Assert.IsNotNull(process);

            output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return process.ExitCode;
        }

        #endregion
    }
}