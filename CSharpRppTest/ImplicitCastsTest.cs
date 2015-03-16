using CSharpRpp;
using CSharpRpp.Expr;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CSharpRppTest
{
    [TestClass]
    public class ImplicitCastsTest
    {
        [TestMethod]
        public void IntegerBoxing()
        {
            RppInteger sourceExpr = new RppInteger(10);
            IRppExpr boxingInt = ImplicitCast.CastIfNeeded(sourceExpr, typeof (object));
            Assert.AreEqual(new RppBox(sourceExpr), boxingInt);
        }
    }
}