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
        public static RType ByteTy = CreatePrimitive("Byte", typeof(byte));
        public static RType LongTy = CreatePrimitive("Long", typeof (long));
        public static RType FloatTy = CreatePrimitive("Float", typeof (float));
        public static RType DoubleTy = CreatePrimitive("Double", typeof (double));
        public static RType NullTy = UnitTy;
        public static RType AnyTy = ImportClass("Any", typeof (object));

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

        public static void PopulateBuiltinTypes([NotNull] Symbols.SymbolTable scope)
        {
            scope.AddType(UnitTy);
            scope.AddType(CharTy);
            scope.AddType(BooleanTy);
            scope.AddType(ShortTy);
            scope.AddType(IntTy);
            scope.AddType(LongTy);
            scope.AddType(FloatTy);
            scope.AddType(DoubleTy);
            scope.AddType(ImportClass(typeof (string)));
            scope.AddType(AnyTy);
        }

        public static RType ImportClass(Type systemType)
        {
            RType type = new RType(systemType.Name, systemType);
            return type;
        }

        public static RType ImportClass(string name, Type systemType)
        {
            RType type = new RType(name, systemType);
            return type;
        }

        [NotNull]
        public static RType ImportPrimitive([NotNull] Type type)
        {
            if (type == typeof (void))
                return UnitTy;
            if (type == typeof (char))
                return CharTy;
            if (type == typeof (bool))
                return BooleanTy;
            if (type == typeof (short))
                return ShortTy;
            if (type == typeof (int))
                return IntTy;
            if (type == typeof (long))
                return LongTy;
            if (type == typeof (float))
                return FloatTy;
            if (type == typeof (double))
                return DoubleTy;

            throw new Exception($"Can't match {type}");
        }
    }
}