using NUnit.Framework;

namespace CSharpRppTest
{
    [TestFixture]
    public class ComplexTests
    {
        [Test]
        [Category("Generics")]
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