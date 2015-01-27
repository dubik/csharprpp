using CSharpRpp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CSharpRppTest
{
    [TestClass]
    public class ScopeTest
    {
        [TestMethod]
        public void OneLevelLookup()
        {
            RppScope scope = new RppScope(null);
            RppClass clazz = new RppClass("Array", ClassKind.Class);
            scope.Add(clazz);

            Assert.AreEqual(clazz, scope.Lookup("Array"));
            Assert.IsNull(scope.Lookup("somethingmissing"));
        }

        [TestMethod]
        public void TwoLevelLookup()
        {
            RppScope parent = new RppScope(null);
            RppScope scope = new RppScope(parent);

            RppClass clazz = new RppClass("Array", ClassKind.Class);
            parent.Add(clazz);

            Assert.AreEqual(clazz, scope.Lookup("Array"));
            Assert.IsNull(scope.Lookup("somethingmissing"));
        }
    }
}