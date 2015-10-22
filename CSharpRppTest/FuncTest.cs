// ----------------------------------------------------------------------
// Copyright © 2014 Microsoft Mobile. All rights reserved.
// Contact: Sergiy Dubovik <sergiy.dubovik@microsoft.com>
//  
// This software, including documentation, is protected by copyright controlled by
// Microsoft Mobile. All rights are reserved. Copying, including reproducing, storing,
// adapting or translating, any or all of this material requires the prior written consent of
// Microsoft Mobile. This material also contains confidential information which may not
// be disclosed to others without the prior written consent of Microsoft Mobile.
// ----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using CSharpRpp;
using CSharpRpp.Parser;
using CSharpRpp.TypeSystem;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static CSharpRpp.TypeSystem.RppTypeSystem;

namespace CSharpRppTest
{
    [TestClass]
    public class FuncTest
    {
        private readonly RppParam _intX = new RppParam("x", new ResolvableType(IntTy));
        private readonly RppParam _intY = new RppParam("y", new ResolvableType(IntTy));
        private readonly RppParam _floatY = new RppParam("y", new ResolvableType(FloatTy));
        private readonly RppParam _varArgIntX = new RppParam("x", new ResolvableType(IntTy), true);

        [TestMethod]
        public void QueryOneToOneOverload()
        {
            var args = new List<RType> {IntTy};
            var func1 = new RppMethodInfo("create", null, RMethodAttributes.None, UnitTy, new[] {new RppParameterInfo("x", IntTy)});
            var results = OverloadQuery.Find(args, new List<RppMethodInfo> {func1}).ToList();
            Assert.AreEqual(1, results.Count());
            Assert.AreEqual(func1, results.First());

            var func2 = new RppMethodInfo("create", null, RMethodAttributes.None, UnitTy, new RppParameterInfo[] {});
            results = OverloadQuery.Find(Enumerable.Empty<RType>(), new List<RppMethodInfo> {func2}).ToList();
            Assert.AreEqual(1, results.Count());
            Assert.AreEqual(func2, results.First());
        }
        /*
        [TestMethod]
        public void QueryOneOutOfTwoOverload()
        {
            var args = new List<RppType> {RppPrimitiveType.IntTy, RppPrimitiveType.IntTy};
            var func1 = new RppFunc("create", new List<RppParam> {_intX}, ResolvableType.UnitTy);
            var func2 = new RppFunc("create", new List<RppParam> {_intX, _intY}, ResolvableType.UnitTy);
            var func3 = new RppFunc("create", new List<RppParam> {_intX, _floatY}, ResolvableType.UnitTy);

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
            var func1 = new RppFunc("create", new List<RppParam> {_varArgIntX}, ResolvableType.UnitTy);
            var args = new List<RppType> {RppPrimitiveType.IntTy, RppPrimitiveType.IntTy};
            var results = OverloadQuery.Find(args, new List<IRppFunc> {func1}).ToList();
            Assert.AreEqual(1, results.Count());
            Assert.AreEqual(func1, results.First());
        }

        [TestMethod]
        public void QuerySecondVarArg()
        {
            // def create(id: Int, ids: Int*) : Unit
            var func1 = new RppFunc("create", new List<RppParam> {_intX, _varArgIntX}, ResolvableType.UnitTy);
            var args = new List<RppType> {RppPrimitiveType.IntTy, RppPrimitiveType.IntTy};
            var results = OverloadQuery.Find(args, new List<IRppFunc> {func1}).ToList();
            Assert.AreEqual(1, results.Count());
            Assert.AreEqual(func1, results.First());
        }
        */
    }
}