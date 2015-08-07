using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CSharpRppTest
{
    [TestClass]
    public class ComplexTests
    {
        [TestMethod]
        public void TestOptionMoand()
        {
            const string code = @"
class Option[A]
class Some[A](a: A) extends Options[A]
class None[A] extends Options[A]

object Bar
{
}
";

            var barTy = Utils.ParseAndCreateType(code, "Bar$");
        }
    }
}
