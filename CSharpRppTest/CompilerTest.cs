using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CSharpRppTest
{
    [TestClass]
    public class CompilerTest
    {
        [TestCategory("ILVerifier"), TestMethod]
        public void SpawnVerifier()
        {
            ProcessStartInfo info = new ProcessStartInfo
            {
                FileName = "CSharpRpp.exe",
                CreateNoWindow = false,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            Process csharProcess = Process.Start(info);
            Assert.IsNotNull(csharProcess);

            csharProcess.WaitForExit();
            Assert.AreEqual(1, csharProcess.ExitCode);

            ProcessStartInfo peverifyInfo = new ProcessStartInfo
            {
                FileName = @"tools\peverify.exe",
                CreateNoWindow = false,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            Process proc = Process.Start(peverifyInfo);
            Assert.IsNotNull(proc);
            proc.WaitForExit();
            Assert.AreEqual(1, csharProcess.ExitCode);
        }
    }
}