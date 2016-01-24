using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CSharpRppTest
{
    [TestClass]
    public class ComplexTests
    {
        [TestMethod]
        [TestCategory("Generics")]
        public void TestOptionMoand()
        {
            const string code = @"
class TOption[A]
class TSome[A](a: A) extends TOption[A]
class TNone[A] extends TOption[A]

object Bar
{
}
";

            Utils.ParseAndCreateType(code, "Bar$");
        }
    }
}