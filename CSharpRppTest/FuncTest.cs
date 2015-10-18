using System.Collections.Generic;
using System.Linq;
using CSharpRpp;
using CSharpRpp.Parser;
using CSharpRpp.TypeSystem;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CSharpRppTest
{
    [TestClass]
    public class FuncTest
    {
        private readonly RppParam _intX = new RppParam("x", RppPrimitiveType.IntTy);
        private readonly RppParam _intY = new RppParam("y", RppPrimitiveType.IntTy);
        private readonly RppParam _floatY = new RppParam("y", RppPrimitiveType.FloatTy);
        private readonly RppParam _varArgIntX = new RppParam("x", RppPrimitiveType.IntTy, true);

        [TestMethod]
        public void QueryOneToOneOverload()
        {
            var args = new List<RppType> {RppPrimitiveType.IntTy};
            var func1 = new RppFunc("create", new List<RppParam> {_intX}, RTypeName.UnitN);
            var results = OverloadQuery.Find(args, new List<IRppFunc> {func1}).ToList();
            Assert.AreEqual(1, results.Count());
            Assert.AreEqual(func1, results.First());

            var func2 = new RppFunc("create", RTypeName.UnitN);
            results = OverloadQuery.Find(Enumerable.Empty<RppType>(), new List<IRppFunc> {func2}).ToList();
            Assert.AreEqual(1, results.Count());
            Assert.AreEqual(func2, results.First());
        }

        [TestMethod]
        public void QueryOneOutOfTwoOverload()
        {
            var args = new List<RppType> {RppPrimitiveType.IntTy, RppPrimitiveType.IntTy};
            var func1 = new RppFunc("create", new List<RppParam> {_intX}, RTypeName.UnitN);
            var func2 = new RppFunc("create", new List<RppParam> {_intX, _intY}, RTypeName.UnitN);
            var func3 = new RppFunc("create", new List<RppParam> {_intX, _floatY}, RTypeName.UnitN);

            var results = OverloadQuery.Find(args, new List<IRppFunc> {func1, func2, func3}).ToList();
            Assert.AreEqual(1, results.Count());
            Assert.AreEqual(func2, results.First());

            args = new List<RppType> {RppPrimitiveType.IntTy, RppPrimitiveType.FloatTy};
            results = OverloadQuery.Find(args, new List<IRppFunc> {func1, func2, func3}).ToList();
            Assert.AreEqual(1, results.Count());
            Assert.AreEqual(func3, results.First());
        }

        [TestMethod]
        public void QueryOnlyVarArg()
        {
            // def create(ids: Int*) : Unit
            var func1 = new RppFunc("create", new List<RppParam> {_varArgIntX}, RTypeName.UnitN);
            var args = new List<RppType> {RppPrimitiveType.IntTy, RppPrimitiveType.IntTy};
            var results = OverloadQuery.Find(args, new List<IRppFunc> {func1}).ToList();
            Assert.AreEqual(1, results.Count());
            Assert.AreEqual(func1, results.First());
        }

        [TestMethod]
        public void QuerySecondVarArg()
        {
            // def create(id: Int, ids: Int*) : Unit
            var func1 = new RppFunc("create", new List<RppParam> {_intX, _varArgIntX}, RTypeName.UnitN);
            var args = new List<RppType> {RppPrimitiveType.IntTy, RppPrimitiveType.IntTy};
            var results = OverloadQuery.Find(args, new List<IRppFunc> {func1}).ToList();
            Assert.AreEqual(1, results.Count());
            Assert.AreEqual(func1, results.First());
        }
    }
}