﻿using System;
using System.Collections.Generic;

namespace CSharpRpp.TypeSystem
{
    public class RppTypeSystem
    {
        public static RType UnitTy = CreatePrimitive("Unit", typeof (void));
        public static RType CharTy = CreatePrimitive("Char", typeof (void));
        public static RType BooleanTy = CreatePrimitive("Boolean", typeof (void));
        public static RType ShortTy = CreatePrimitive("Short", typeof (void));
        public static RType IntTy = CreatePrimitive("Int", typeof (void));
        public static RType LongTy = CreatePrimitive("Long", typeof (void));
        public static RType FloatTy = CreatePrimitive("Float", typeof (void));
        public static RType DoubleTy = CreatePrimitive("Double", typeof (void));
        public static RType NullTy = UnitTy;

        private static RType CreatePrimitive(string name, Type systemType)
        {
            return new RType(name) {TypeInfo = systemType};
        }

        private static readonly Dictionary<string, RType> PrimitiveTypesMap = new Dictionary<string, RType>
        {
            {"Unit", UnitTy},
            {"Char", CharTy},
            {"Boolean", BooleanTy},
            {"Short", ShortTy},
            {"Int", IntTy},
            {"Long", LongTy},
            {"Float", FloatTy},
            {"Double", DoubleTy}
        };

        public static bool IsPrimitive(string name)
        {
            return PrimitiveTypesMap.ContainsKey(name);
        }

        public static RType GetPrimitive(string name)
        {
            return PrimitiveTypesMap[name];
        }

        public RppTypeSystem()
        {
        }
    }
}