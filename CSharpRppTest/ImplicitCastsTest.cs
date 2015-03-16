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
            /*
            RppVar variable = new RppVar(MutabilityFlag.MF_Val, "k", RppNativeType.Create(typeof (object)), new RppInteger("10"));
            RppScope scope = new RppScope(null);
            variable.PreAnalyze(scope);
            RppVar analyzedVar = variable.Analyze(scope) as RppVar;
            Assert.IsNotNull(analyzedVar);
            */

            RppInteger sourceExpr = new RppInteger(10);
            IRppExpr boxingInt = ImplicitCast.CastIfNeeded(sourceExpr, typeof (object));
            Assert.AreEqual(new RppBox(sourceExpr), boxingInt);
        }
    }
}