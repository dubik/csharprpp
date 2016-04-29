using System;
using System.Collections.Generic;
using CSharpRpp.Symbols;
using JetBrains.Annotations;

namespace CSharpRpp.TypeSystem
{
    public sealed class RppTypeSystem
    {
        public static readonly RppTypeSystem Instance = new RppTypeSystem();

        public static RType UnitTy = CreateType("Unit", typeof (void));
        public static RType CharTy = CreateType("Char", typeof (char));
        public static RType BooleanTy = CreateType("Boolean", typeof (bool));
        public static RType ShortTy = CreateType("Short", typeof (short));
        public static RType IntTy = CreateType("Int", typeof (int));
        public static RType ByteTy = CreateType("Byte", typeof (byte));
        public static RType LongTy = CreateType("Long", typeof (long));
        public static RType FloatTy = CreateType("Float", typeof (float));
        public static RType DoubleTy = CreateType("Double", typeof (double));
        public static RType NullTy = ImportClass("Null", typeof (object));
        public static RType AnyTy = ImportClass("Any", typeof (object));
        public static RType StringTy = ImportClass("String", typeof (string));
        public static RType NothingTy = ImportClass("Nothing", typeof (object));
        public static RType Undefined = new RType("Undefined");
        public static RType ArrayTy = CreateArrayType();

        private readonly Dictionary<string, RType> _allTypes = new Dictionary<string, RType>();

        public static RType CreateType(string name)
        {
            return Instance.GetOrCreate(name, () => new RType(name));
        }

        public static RType CreateType(string name, Type systemType)
        {
            return Instance.GetOrCreate(name, () => new RType(name, systemType));
        }

        public static RType CreateType(string name, RTypeAttributes attributes, RType parent, RType declaringType)
        {
            return Instance.GetOrCreate(name, () => new RType(name, attributes, parent, declaringType));
        }

        private RType GetOrCreate(string name, Func<RType> typeFactory)
        {
            RType type;
            if (_allTypes.TryGetValue(name, out type))
            {
                return type;
            }

            type = typeFactory();
            _allTypes.Add(name, type);
            return type;
        }

        public static void PopulateBuiltinTypes([NotNull] SymbolTable scope)
        {
            Instance._allTypes.Clear();
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
            scope.AddType(NothingTy);
            scope.AddType(ArrayTy);

            Instance._allTypes.Add(typeof (void).Name, UnitTy);
            Instance._allTypes.Add(typeof (char).Name, CharTy);
            Instance._allTypes.Add(typeof (bool).Name, BooleanTy);
            Instance._allTypes.Add(typeof (short).Name, ShortTy);
            Instance._allTypes.Add(typeof (int).Name, IntTy);
            Instance._allTypes.Add(typeof (byte).Name, ByteTy);
            Instance._allTypes.Add(typeof (long).Name, LongTy);
            Instance._allTypes.Add(typeof (float).Name, FloatTy);
            Instance._allTypes.Add(typeof (object).Name, AnyTy);
        }

        public static RType ImportClass(Type systemType)
        {
            RType type = CreateType(systemType.Name, systemType);
            return type;
        }

        public static RType ImportClass(string name, Type systemType)
        {
            RType type = CreateType(name, systemType);
            return type;
        }

        private static RType CreateArrayType()
        {
            RType arrayType = new RType("Array") {IsArray = true};
            RppGenericParameter genericParameter = arrayType.DefineGenericParameters(new[] {"A"})[0];
            arrayType.DefineConstructor(RMethodAttributes.Public, new[] {new RppParameterInfo("size", IntTy)});
            arrayType.DefineMethod("length", RMethodAttributes.Public, IntTy, new RppParameterInfo[0]);
            arrayType.DefineMethod("apply", RMethodAttributes.Public, genericParameter.Type, new[] {new RppParameterInfo("index", IntTy)},
                new RppGenericParameter[0]);
            arrayType.DefineMethod("update", RMethodAttributes.Public, UnitTy,
                new[] {new RppParameterInfo("index", IntTy), new RppParameterInfo("value", genericParameter.Type)}, new RppGenericParameter[0]);
            return arrayType;
        }

        [NotNull]
        public static RType ImportPrimitive([NotNull] Type type)
        {
            if (type == typeof (void))
            {
                return UnitTy;
            }

            if (type == typeof (char))
            {
                return CharTy;
            }

            if (type == typeof (bool))
            {
                return BooleanTy;
            }

            if (type == typeof (short))
            {
                return ShortTy;
            }

            if (type == typeof (int))
            {
                return IntTy;
            }

            if (type == typeof (long))
            {
                return LongTy;
            }

            if (type == typeof (float))
            {
                return FloatTy;
            }

            if (type == typeof (double))
            {
                return DoubleTy;
            }

            if (type == typeof (string))
            {
                return StringTy;
            }

            throw new Exception($"Can't match {type}");
        }
    }
}