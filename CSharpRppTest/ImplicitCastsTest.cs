using CSharpRpp;
using CSharpRpp.Expr;
using CSharpRpp.TypeSystem;
using NUnit.Framework;

namespace CSharpRppTest
{
    [TestFixture]
    public class ImplicitCastsTest
    {
        [Test]
        public void IntegerBoxing()
        {
            RppInteger sourceExpr = new RppInteger(10);
            IRppExpr boxingInt = ImplicitCast.CastIfNeeded(sourceExpr, RppTypeSystem.AnyTy);
            Assert.AreEqual(new RppBox(sourceExpr), boxingInt);
        }
    }
}