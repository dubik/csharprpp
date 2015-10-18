using System;
using System.Collections.Generic;
using JetBrains.Annotations;

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
            scope.Add(Import(typeof (string)));
        }

        public static RType Import(Type systemType)
        {
            RType type = new RType(systemType.Name, systemType.IsClass ? RTypeAttributes.Class : RTypeAttributes.None) {TypeInfo = systemType};
            return type;
        }
    }
}