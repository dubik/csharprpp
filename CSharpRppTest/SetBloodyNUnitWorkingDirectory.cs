using System.Diagnostics;
using System.IO;
using NUnit.Framework;

namespace CSharpRppTest
{
    [SetUpFixture]
    public class SetBloodyNUnitWorkingDirectory
    {
        [OneTimeSetUp]
        public void RunBeforeAnyTests()
        {
            var dir = Path.GetDirectoryName(typeof(SetBloodyNUnitWorkingDirectory).Assembly.Location);
            Debug.Assert(dir != null, "dir != null");
            Directory.SetCurrentDirectory(dir);
        }
    }
}