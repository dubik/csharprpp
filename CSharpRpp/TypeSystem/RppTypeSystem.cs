using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace CSharpRpp.TypeSystem
{
    public class RppTypeSystem
    {
        public static RType UnitTy = CreatePrimitive("Unit", typeof (void));
        public static RType CharTy = CreatePrimitive("Char", typeof (char));
        public static RType BooleanTy = CreatePrimitive("Boolean", typeof (bool));
        public static RType ShortTy = CreatePrimitive("Short", typeof (short));
        public static RType IntTy = CreatePrimitive("Int", typeof (int));
        public static RType LongTy = CreatePrimitive("Long", typeof (long));
        public static RType FloatTy = CreatePrimitive("Float", typeof (float));
        public static RType DoubleTy = CreatePrimitive("Double", typeof (double));
        public static RType NullTy = UnitTy;

        private static RType CreatePrimitive(string name, Type systemType)
        { 
            return new RType(name, systemType);
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

        public static void PopulateBuiltinTypes([NotNull] RppScope scope)
        {
            scope.Add(UnitTy);
            scope.Add(CharTy);
            scope.Add(BooleanTy);
            scope.Add(ShortTy);
            scope.Add(IntTy);
            scope.Add(LongTy);
            scope.Add(FloatTy);
            scope.Add(DoubleTy);
            scope.Add(ImportClass(typeof (string)));
        }

        public static RType ImportClass(Type systemType)
        {
            RType type = new RType(systemType.Name, systemType);
            return type;
        }
    }
}