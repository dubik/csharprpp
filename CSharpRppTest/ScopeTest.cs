﻿using System.Collections.Generic;
using System.Linq;
using CSharpRpp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CSharpRppTest
{
    [TestClass]
    public class ScopeTest
    {
        private readonly RppParam _intX = new RppParam("x", RppPrimitiveType.IntTy);
        private readonly RppParam _intY = new RppParam("y", RppPrimitiveType.IntTy);
        private readonly RppParam _floatY = new RppParam("y", RppPrimitiveType.FloatTy);
        private readonly RppParam _varArgIntX = new RppParam("x", RppPrimitiveType.IntTy, true);

        [TestMethod]
        public void OneLevelLookup()
        {
            RppScope scope = new RppScope(null);
            RppClass clazz = new RppClass(ClassKind.Class, "Array");
            scope.Add(clazz);

            Assert.AreEqual(clazz, scope.Lookup("Array"));
            Assert.IsNull(scope.Lookup("somethingmissing"));
        }

        [TestMethod]
        public void TwoLevelLookup()
        {
            RppScope parent = new RppScope(null);
            RppScope scope = new RppScope(parent);

            RppClass clazz = new RppClass(ClassKind.Class, "Array");
            parent.Add(clazz);

            Assert.AreEqual(clazz, scope.Lookup("Array"));
            Assert.IsNull(scope.Lookup("somethingmissing"));
        }

        [TestMethod]
        public void BaseClassLookup()
        {
            RppClassScope parent = new RppClassScope(null);
            RppFunc func = new RppFunc("create", RppPrimitiveType.UnitTy);
            parent.Add(func);
            RppClassScope scope = new RppClassScope(null) {BaseClassScope = parent};
            var res = scope.LookupFunction("create").ToList();
            Assert.AreEqual(1, res.Count);
            Assert.AreSame(func, res[0]);
        }

        [TestMethod]
        public void TwoFuncsMatchInCurrentAndBase()
        {
            RppClassScope parent = new RppClassScope(null);
            RppFunc func = new RppFunc("create", RppPrimitiveType.UnitTy);
            parent.Add(func);
            RppClassScope scope = new RppClassScope(null) {BaseClassScope = parent};

            RppFunc func1 = new RppFunc("create", new List<IRppParam> {_intX}, RppPrimitiveType.UnitTy);
            scope.Add(func1);
            var res = scope.LookupFunction("create").ToList();
            Assert.AreEqual(2, res.Count);
            Assert.AreSame(func1, res[0]); // Order is important
            Assert.AreSame(func, res[1]);
        }
    }
}